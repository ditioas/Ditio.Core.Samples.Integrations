"""The tabular model (DataModelSchema) of the Ditio Power BI template."""

import json
import uuid
from pathlib import Path

from spec import COLUMN_TYPES, DATE_KEY_COLUMN, DATE_KEY_LABEL, REFRESH_DAYS, RELATIONSHIPS, STORE_MONTHS, is_hidden
from queries import (
    DATE_COLUMNS, FUNCTIONS, MEASURE_HOST_QUERY, DATA_STATUS_QUERY, PARAMETERS,
    output_columns, render_date_query, render_parameter, render_static_table,
)

TRANSLATIONS_DIR = Path(__file__).resolve().parent / "translations"
LANGUAGE = "nb"

NOT_SUMMED_SUFFIXES = ("year", "version", "latitude", "longitude", "price", "gradient", "speedkmh", "rate", "perhour")
DATETIME_FORMAT = "dd.MM.yyyy HH:mm"
DATE_FORMAT = "dd.MM.yyyy"

MEASURE_TABLE = "Målinger"
DATE_TABLE = "Dato"
SOURCES_TABLE = "Datakilder"
GLOSSARY_TABLE = "Feltordliste"
STATUS_TABLE = "Datagrunnlag"


def lineage(*parts):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, "ditio-powerbi/" + "/".join(parts)))


class Labels:
    """Resolves the label of every column; 'hdr:<Key>' reads Ditio's Excel export headers."""

    def __init__(self, language=LANGUAGE):
        path = TRANSLATIONS_DIR / f"export-headers.{language}.json"
        self.headers = json.loads(path.read_text(encoding="utf-8"))

    def resolve(self, raw):
        if raw.startswith("hdr:"):
            key = raw[4:]
            if key not in self.headers:
                raise SystemExit(f"Label key {key!r} is not in {LANGUAGE} export headers")
            return self.headers[key]
        return raw

    def columns(self, table):
        """[(api, kind, label, excel_label_or_None)] for every column the query returns."""
        result = [(api, kind, self.resolve(raw), self.resolve(raw) if raw.startswith("hdr:") else None)
                  for api, kind, raw in table["columns"]]
        result += [(new_field, "text", label, None) for new_field, _, _, label in table.get("mapped", [])]
        if "per_person" in table:
            result.append((table["per_person"]["count_field"], "int", table["per_person"]["count_label"], None))
        if "date_key" in table:
            result.append((DATE_KEY_COLUMN, "date", DATE_KEY_LABEL, None))
        assert [c[0] for c in result] == [c[0] for c in output_columns(table)], table["name"]
        return result

    def label_of(self, table, api):
        for column_api, _, label, _ in self.columns(table):
            if column_api == api:
                return label
        raise SystemExit(f"{table['name']} has no column {api!r}")


def _description(table, api, excel_label):
    if api == DATE_KEY_COLUMN:
        return f"Dato uten klokkeslett, fra {table['date_key']}. Kobler tabellen til Dato-tabellen."
    if "per_person" in table and api == table["per_person"]["count_field"]:
        return "Antall firmaprofiler personen har. Raden viser profilen i arbeidsgiverfirmaet."
    for new_field, source, _, _ in table.get("mapped", []):
        if api == new_field:
            return f"Norsk visning av API-feltet {source}."
    text = f"API-felt: {api}."
    if excel_label:
        text += f" Samme som Excel-kolonnen «{excel_label}» i Ditio."
    return text


def _model_column(table, api, kind, label, excel_label):
    data_type = COLUMN_TYPES[kind][1]
    column = {
        "name": label,
        "dataType": data_type,
        "sourceColumn": api,
        "lineageTag": lineage(table["name"], api),
        "description": _description(table, api, excel_label),
        "summarizeBy": "sum" if data_type in ("double", "int64") and not api.lower().endswith(NOT_SUMMED_SUFFIXES) else "none",
        "annotations": [{"name": "SummarizationSetBy", "value": "Automatic"}],
    }
    if kind == "datetime":
        column["formatString"] = DATETIME_FORMAT
    if kind == "date":
        column["formatString"] = DATE_FORMAT
    if is_hidden(table, api):
        column["isHidden"] = True
    return column


def _partition(name, m, group):
    return [{"name": f"{name}-partition", "mode": "import", "queryGroup": group,
             "source": {"type": "m", "expression": m.split("\n")}}]


def _table(name, description, columns, m, group, **extra):
    return {
        "name": name,
        "description": description,
        "lineageTag": lineage(name),
        "columns": columns,
        "partitions": _partition(name, m, group),
        "annotations": [
            {"name": "PBI_NavigationStepName", "value": "Navigation"},
            {"name": "PBI_ResultType", "value": "Table"},
        ],
        **extra,
    }


def _plain_column(table_name, name, kind, hidden=False, description=None, **extra):
    data_type = COLUMN_TYPES[kind][1]
    column = {
        "name": name, "dataType": data_type, "sourceColumn": name,
        "lineageTag": lineage(table_name, name), "summarizeBy": "none",
        "annotations": [{"name": "SummarizationSetBy", "value": "Automatic"}],
        **extra,
    }
    if kind == "date":
        column["formatString"] = DATE_FORMAT
    if kind == "datetime":
        column["formatString"] = DATETIME_FORMAT
    if hidden:
        column["isHidden"] = True
    if description:
        column["description"] = description
    return column


def _expression(name, m, group, result_type, description):
    return {
        "name": name, "kind": "m", "expression": m.split("\n"), "queryGroup": group,
        "description": description, "lineageTag": lineage("expression", name),
        "annotations": [
            {"name": "PBI_NavigationStepName", "value": "Navigation"},
            {"name": "PBI_ResultType", "value": result_type},
        ],
    }


def glossary_rows(data_tables, labels):
    rows = []
    for table in data_tables:
        for api, _, label, excel_label in labels.columns(table):
            if is_hidden(table, api):
                continue
            # The API field and Excel column have their own glossary columns; only explanations go here.
            description = _description(table, api, excel_label)
            if description.startswith("API-felt:"):
                description = ""
            rows.append((table["name"], label, api, excel_label or "", description))
    return rows


def static_queries(data_tables, labels):
    """(name, description, columns, m, group, extra) for the tables that don't call the API."""
    sources = render_static_table(
        [("Tabell", "text"), ("Endepunkt", "text"), ("Innhold", "text")],
        [(t["name"], t["path"], t["description"]) for t in data_tables],
    )
    glossary = render_static_table(
        [("Tabell", "text"), ("Felt i rapporten", "text"), ("API-felt", "text"),
         ("Excel-kolonne", "text"), ("Beskrivelse", "text")],
        glossary_rows(data_tables, labels),
    )
    sort_keys = {"Måned": "Månedsnr", "Ukedag": "Ukedagnr"}
    hidden_date = {"Månedsnr", "Ukedagnr"}
    date_columns = []
    for name, _, kind in DATE_COLUMNS:
        extra = {}
        if name in sort_keys:
            extra["sortByColumn"] = sort_keys[name]
        if name == "Dato":
            extra["isKey"] = True
        date_columns.append(_plain_column(DATE_TABLE, name, kind, hidden=name in hidden_date, **extra))
    return [
        (DATE_TABLE, "Én rad per dag i perioden som er lastet. Uker etter ISO 8601 (mandag først).",
         date_columns, render_date_query(), "Hjelpetabeller", {"dataCategory": "Time"}),
        (MEASURE_TABLE, "Målinger brukt i rapporten. Dra disse inn i egne visualiseringer.",
         [_plain_column(MEASURE_TABLE, "Målinger", "text", hidden=True)], MEASURE_HOST_QUERY, "Hjelpetabeller", {}),
        (SOURCES_TABLE, "Tabellene i rapporten og Ditio-endepunktet hver av dem lastes fra.",
         [_plain_column(SOURCES_TABLE, c, "text") for c in ("Tabell", "Endepunkt", "Innhold")],
         sources, "Hjelpetabeller", {}),
        (GLOSSARY_TABLE, "Hva hvert felt heter i rapporten, i API-et og i Ditios Excel-eksport.",
         [_plain_column(GLOSSARY_TABLE, c, "text")
          for c in ("Tabell", "Felt i rapporten", "API-felt", "Excel-kolonne", "Beskrivelse")],
         glossary, "Hjelpetabeller", {}),
        (STATUS_TABLE, "Når dataene sist ble hentet.",
         [_plain_column(STATUS_TABLE, "Oppdatert (UTC)", "datetime")],
         DATA_STATUS_QUERY, "Hjelpetabeller", {}),
    ]


def build_model(data_queries, measures, labels):
    """data_queries: [(spec table, M)]; measures: [(name, folder, dax, format, description)]."""
    tables = []
    for table, m in data_queries:
        columns = [_model_column(table, api, kind, label, excel) for api, kind, label, excel in labels.columns(table)]
        extra = {}
        if "date_key" in table:
            # Incremental refresh: the service splits the table into date partitions and calls the
            # query once per partition with that partition's RangeStart/RangeEnd.
            extra["refreshPolicy"] = {
                "policyType": "basic",
                "rollingWindowGranularity": "month", "rollingWindowPeriods": STORE_MONTHS,
                "incrementalGranularity": "day", "incrementalPeriods": REFRESH_DAYS,
                "sourceExpression": m.split("\n"),
            }
        tables.append(_table(table["name"], table["description"], columns, m, "Data", **extra))

    data_tables = [t for t, _ in data_queries]
    for name, description, columns, m, group, extra in static_queries(data_tables, labels):
        tables.append(_table(name, description, columns, m, group, **extra))

    measure_table = next(t for t in tables if t["name"] == MEASURE_TABLE)
    measure_table["measures"] = [{
        "name": name, "expression": dax.split("\n"), "formatString": fmt, "displayFolder": folder,
        "description": description, "lineageTag": lineage("measure", name),
    } for name, folder, dax, fmt, description in measures]

    by_name = {t["name"]: t for t in data_tables}
    relationships = []
    for from_table, from_api, to_table, to_api in RELATIONSHIPS:
        relationships.append((from_table, labels.label_of(by_name[from_table], from_api),
                              to_table, labels.label_of(by_name[to_table], to_api)))
    for table in data_tables:
        if "date_key" in table:
            relationships.append((table["name"], DATE_KEY_LABEL, DATE_TABLE, "Dato"))

    expressions = [_expression(p["name"], render_parameter(p), "Parametere", p["result_type"], p["description"])
                   for p in PARAMETERS]
    expressions += [_expression(f["name"], f["m"], "Funksjoner", "Function", f["description"]) for f in FUNCTIONS]
    query_order = [p["name"] for p in PARAMETERS] + [f["name"] for f in FUNCTIONS] + [t["name"] for t in tables]
    return {
        "name": lineage("model"),
        "compatibilityLevel": 1550,
        "model": {
            "culture": "nb-NO",
            "dataAccessOptions": {"legacyRedirects": True, "returnErrorValuesAsNull": True},
            "defaultPowerBIDataSourceVersion": "powerBI_V3",
            "sourceQueryCulture": "en-US",
            "tables": tables,
            "relationships": [{
                "name": lineage("relationship", f, fc, t, tc),
                "fromTable": f, "fromColumn": fc, "toTable": t, "toColumn": tc,
            } for f, fc, t, tc in relationships],
            "expressions": expressions,
            "queryGroups": [
                {"folder": "Parametere", "annotations": [{"name": "PBI_QueryGroupOrder", "value": "0"}]},
                {"folder": "Funksjoner", "annotations": [{"name": "PBI_QueryGroupOrder", "value": "1"}]},
                {"folder": "Data", "annotations": [{"name": "PBI_QueryGroupOrder", "value": "2"}]},
                {"folder": "Hjelpetabeller", "annotations": [{"name": "PBI_QueryGroupOrder", "value": "3"}]},
            ],
            "annotations": [
                {"name": "__PBI_TimeIntelligenceEnabled", "value": "0"},
                {"name": "PBI_QueryOrder", "value": json.dumps(query_order, ensure_ascii=False)},
            ],
        },
    }
