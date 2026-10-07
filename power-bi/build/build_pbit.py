#!/usr/bin/env python3
"""Build the Ditio Power BI template (.pbit), its readable Power Query sources and the
field glossary.

  spec.py       tables, columns, labels, relationships
  queries.py    parameters, shared functions, M per table
  measures.py   DAX measures
  model.py      the tabular model
  pages.py      report pages and theme
  checks.py     checks run on every build

Running this script writes

  power-bi/queries/*.pq            one file per query, for review and copy-paste
  power-bi/feltordliste.md         field glossary: report label, API field, Excel column
  power-bi/Ditio-PowerBI-mal.pbit

Usage:  python3 power-bi/build/build_pbit.py [--refresh-translations <translations folder>]
Needs only the Python 3 standard library.
"""

import json
import sys
import zipfile
from pathlib import Path

import checks
from measures import build_measures
from model import Labels, build_model, glossary_rows, TRANSLATIONS_DIR
from pages import THEME, THEME_FILE, build_layout, glossary_page, start_page
from queries import FUNCTIONS, PARAMETERS, render_item_query, render_parameter, render_table_query
from spec import ITEM_TABLE, TABLES

VERSION = "2.2.0"

HERE = Path(__file__).resolve().parent
POWER_BI_DIR = HERE.parent
STATIC_DIR = HERE / "static"
QUERIES_DIR = POWER_BI_DIR / "queries"
GLOSSARY_PATH = POWER_BI_DIR / "feltordliste.md"
# Unversioned so download links stay stable; the version is on the Start page and in Metadata.
PBIT_PATH = POWER_BI_DIR / "Ditio-PowerBI-mal.pbit"

CONTENT_TYPES = (
    '﻿<?xml version="1.0" encoding="utf-8"?>'
    '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
    '<Default Extension="json" ContentType="" />'
    '<Override PartName="/Version" ContentType="" />'
    '<Override PartName="/DataModelSchema" ContentType="" />'
    '<Override PartName="/DiagramLayout" ContentType="" />'
    '<Override PartName="/Report/Layout" ContentType="" />'
    '<Override PartName="/Settings" ContentType="application/json" />'
    '<Override PartName="/Metadata" ContentType="application/json" />'
    '</Types>'
)


def refresh_translations(translations_folder):
    """Copy the Excel-export header labels from Ditio's web translations (<folder>/<language>/translation.json)."""
    for language in ("nb", "en"):
        source = Path(translations_folder) / language / "translation.json"
        headers = json.loads(source.read_text(encoding="utf-8-sig"))["Reporting"]["FileExport"]["Headers"]
        target = TRANSLATIONS_DIR / f"export-headers.{language}.json"
        target.write_text(json.dumps(dict(sorted(headers.items())), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(f"Updated {target.name} ({len(headers)} labels)")


def utf16(value):
    text = value if isinstance(value, str) else json.dumps(value, ensure_ascii=False)
    return text.encode("utf-16-le")


def build_diagram(table_names):
    return {"version": "1.1.0", "diagrams": [{
        "ordinal": 0, "scrollPosition": {"x": 0, "y": 0}, "name": "Alle tabeller",
        "nodes": [{"location": {"x": 40 + (i % 6) * 280, "y": 40 + (i // 6) * 420},
                   "nodeIndex": name, "size": {"height": 360, "width": 234}, "zIndex": i}
                  for i, name in enumerate(table_names)],
    }]}


def render_glossary(rows, measures):
    lines = [
        "# Feltordliste",
        "",
        "Hva hvert felt heter i Power BI-malen, i Data Extraction API-et og i Ditios Excel-eksport,",
        "og hva hver måling i tabellen «Målinger» betyr.",
        "Generert av `build/build_pbit.py` – ikke rediger for hånd.",
        "",
        "## Målinger",
        "",
        "| Måling | Mappe | Betydning |",
        "|---|---|---|",
    ]
    lines += [f"| {name} | {folder} | {description} |" for name, folder, _, _, description in measures]
    current = None
    for table, label, api, excel, description in rows:
        if table != current:
            lines += ["", f"## {table}", "", "| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |",
                      "|---|---|---|---|"]
            current = table
        lines.append(f"| {label} | `{api}` | {excel} | {description} |")
    return "\n".join(lines) + "\n"


def main():
    if len(sys.argv) == 3 and sys.argv[1] == "--refresh-translations":
        refresh_translations(sys.argv[2])

    labels = Labels()
    data_tables = TABLES + [ITEM_TABLE]
    checks.check_labels(data_tables, labels)

    data_queries = [(t, render_table_query(t)) for t in TABLES] + [(ITEM_TABLE, render_item_query(ITEM_TABLE))]
    measures = build_measures(data_tables, labels.label_of)
    model = build_model(data_queries, measures, labels)
    columns, measure_names = checks.check_model(model)

    layout = build_layout([start_page(VERSION), glossary_page()])
    checks.check_layout(layout, columns, measure_names)

    query_files = {}
    for p in PARAMETERS:
        query_files[p["name"]] = f"// {p['description']}\n{render_parameter(p)}\n"
    for f in FUNCTIONS:
        query_files[f["name"]] = f"// {f['description']}\n{f['m']}\n"
    for table in model["model"]["tables"]:
        m = "\n".join(table["partitions"][0]["source"]["expression"])
        query_files[table["name"]] = f"// {table['description']}\n{m}\n"
    glossary = render_glossary(glossary_rows(data_tables, labels), measures)

    metadata = {
        "Version": 5, "AutoCreatedRelationships": [], "CreatedFrom": "Cloud", "CreatedFromRelease": "2024.04",
        "FileDescription": (
            f"Ditio Power BI-mal v{VERSION}. Henter prosjekter, arbeidsordrer, ressurser, brukere, timer, "
            "maskinregistreringer, fravær, varsler, sjekklister, massetransport og varer fra Ditio Data Extraction "
            "API. Fyll ut ClientId/ClientSecret (eller AccessToken) og periode."
        ),
    }
    parts = {
        "DataModelSchema": json.dumps(model, indent=2, ensure_ascii=False),
        "DiagramLayout": json.dumps(build_diagram([t["name"] for t in model["model"]["tables"]]), ensure_ascii=False),
        "Report/Layout": json.dumps(layout, ensure_ascii=False),
        "Metadata": json.dumps(metadata, ensure_ascii=False),
        f"Report/StaticResources/RegisteredResources/{THEME_FILE}": json.dumps(THEME, indent=2),
    }
    checks.check_publication_safety(
        list(parts.items()) + [(f"queries/{k}.pq", v) for k, v in query_files.items()] + [("feltordliste.md", glossary)]
        + [(doc, (POWER_BI_DIR / doc).read_text(encoding="utf-8")) for doc in ("README.md", "BRUKERVEILEDNING.md")])

    QUERIES_DIR.mkdir(exist_ok=True)
    for stale in QUERIES_DIR.glob("*.pq"):
        stale.unlink()
    for name, text in query_files.items():
        (QUERIES_DIR / f"{name}.pq").write_text(text, encoding="utf-8")
    GLOSSARY_PATH.write_text(glossary, encoding="utf-8")
    for stale in POWER_BI_DIR.glob("*.pbit"):
        if stale != PBIT_PATH:
            stale.unlink()

    with zipfile.ZipFile(PBIT_PATH, "w", zipfile.ZIP_DEFLATED) as archive:
        def write(name, data):
            # A fixed timestamp keeps the .pbit byte-identical across rebuilds.
            archive.writestr(zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0)), data, zipfile.ZIP_DEFLATED)

        write("Version", (STATIC_DIR / "Version").read_bytes())
        write("[Content_Types].xml", CONTENT_TYPES.encode("utf-8"))
        write("DataModelSchema", utf16(parts["DataModelSchema"]))
        write("DiagramLayout", utf16(parts["DiagramLayout"]))
        write("Report/Layout", utf16(parts["Report/Layout"]))
        write("Settings", (STATIC_DIR / "Settings").read_bytes())
        write("Metadata", utf16(parts["Metadata"]))
        base_theme = "Report/StaticResources/SharedResources/BaseThemes/CY19SU06.json"
        write(base_theme, (STATIC_DIR / base_theme).read_bytes())
        write(f"Report/StaticResources/RegisteredResources/{THEME_FILE}", parts[f"Report/StaticResources/RegisteredResources/{THEME_FILE}"].encode("utf-8"))

    print(f"Wrote {PBIT_PATH.name} v{VERSION}, {len(query_files)} queries, {GLOSSARY_PATH.name}; "
          f"{len(model['model']['tables'])} tables, {len(measure_names)} measures, "
          f"{len(model['model']['relationships'])} relationships, {len(layout['sections'])} pages")


if __name__ == "__main__":
    main()
