"""Report pages (Report/Layout) of the Ditio Power BI template.

Visual configs follow the shape Power BI Desktop writes for the same visual types.
Fields are ("column", table, column) or ("measure", name); measures live in Målinger.
"""

import json
import uuid

from model import GLOSSARY_TABLE, MEASURE_TABLE, SOURCES_TABLE
from spec import REFRESH_MONTHS, STORE_MONTHS

PAGE_WIDTH, PAGE_HEIGHT = 1280, 720
DOCS_URL = "https://docs.ditio.app/guides/powerbi/"

THEME_FILE = "DitioTheme.json"
THEME = {
    "name": "Ditio",
    "dataColors": ["#F3671B", "#017EFF", "#1B1B18", "#99CBFF", "#C25216",
                   "#014C99", "#868682", "#FAC2A4", "#67B2FF", "#61290B"],
    "background": "#FFFFFF",
    "foreground": "#1B1B18",
    "tableAccent": "#F3671B",
}

REPORT_VERSION = "5.53"


def _name(*parts):
    return uuid.uuid5(uuid.NAMESPACE_URL, "ditio-powerbi/visual/" + "/".join(parts)).hex[:20]


def _literal(value):
    return {"expr": {"Literal": {"Value": value}}}


def _field_expr(field, source):
    kind = field[0]
    if kind == "measure":
        return {"Measure": {"Expression": {"SourceRef": {"Source": source}}, "Property": field[1]}}
    return {"Column": {"Expression": {"SourceRef": {"Source": source}}, "Property": field[2]}}


def _entity(field):
    return MEASURE_TABLE if field[0] == "measure" else field[1]


def _query_ref(field):
    return f"{_entity(field)}.{field[1] if field[0] == 'measure' else field[2]}"


def _prototype(fields):
    aliases = {}
    for field in fields:
        aliases.setdefault(_entity(field), f"t{len(aliases)}")
    return {
        "Version": 2,
        "From": [{"Name": alias, "Entity": entity, "Type": 0} for entity, alias in aliases.items()],
        "Select": [{**_field_expr(f, aliases[_entity(f)]), "Name": _query_ref(f)} for f in fields],
    }


def _title(text):
    return {"title": [{"properties": {"text": _literal(f"'{text}'"), "show": _literal("true")}}]}


def _container(page, key, x, y, width, height, z, single_visual):
    config = {
        "name": _name(page, key),
        "layouts": [{"id": 0, "position": {"x": x, "y": y, "z": z, "width": width, "height": height}}],
        "singleVisual": single_visual,
    }
    return {"x": x, "y": y, "z": z, "width": width, "height": height,
            "config": json.dumps(config, ensure_ascii=False), "filters": "[]"}


def textbox(page, key, x, y, width, height, paragraphs, z=0):
    """paragraphs: list of lists of (text, style dict, url or None)."""
    def run(text, style, url):
        r = {"value": text}
        if style:
            r["textStyle"] = style
        if url:
            r["url"] = url
        return r
    return _container(page, key, x, y, width, height, z, {
        "visualType": "textbox",
        "drillFilterOtherVisuals": True,
        "objects": {"general": [{"properties": {"paragraphs": [
            {"textRuns": [run(*r) for r in p]} for p in paragraphs
        ]}}]},
    })


def card(page, key, x, y, width, height, measure, title, z=0):
    field = ("measure", measure)
    return _container(page, key, x, y, width, height, z, {
        "visualType": "card",
        "projections": {"Values": [{"queryRef": _query_ref(field)}]},
        "prototypeQuery": _prototype([field]),
        "drillFilterOtherVisuals": True,
        "objects": {"categoryLabels": [{"properties": {"show": _literal("false")}}]},
        "vcObjects": _title(title),
    })


def table(page, key, x, y, width, height, fields, title=None, z=0):
    single = {
        "visualType": "tableEx",
        "projections": {"Values": [{"queryRef": _query_ref(f)} for f in fields]},
        "prototypeQuery": _prototype(fields),
        "drillFilterOtherVisuals": True,
    }
    if title:
        single["vcObjects"] = _title(title)
    return _container(page, key, x, y, width, height, z, single)


def slicer(page, key, x, y, width, height, table_name, column, title, z=0):
    field = ("column", table_name, column)
    return _container(page, key, x, y, width, height, z, {
        "visualType": "slicer",
        "projections": {"Values": [{"queryRef": _query_ref(field), "active": True}]},
        "prototypeQuery": _prototype([field]),
        "drillFilterOtherVisuals": True,
        "objects": {"data": [{"properties": {"mode": _literal("'Dropdown'")}}]},
        "vcObjects": _title(title),
    })


BOLD = {"fontWeight": "bold"}
HEADING = {"fontWeight": "bold", "fontSize": "20pt"}
SUBTLE = {"fontSize": "10pt", "color": "#706F6C"}


def start_page(version):
    page = "start"
    visuals = [
        textbox(page, "title", 40, 20, 1200, 64, [
            [("Ditio – rapport fra Ditio Data Extraction", HEADING, None)],
            [(f"Malversjon {version}. Data hentes fra Ditio med API-klienten din.", SUBTLE, None)],
        ]),
        card(page, "from", 40, 100, 170, 90, "Data fra", "Data fra"),
        card(page, "to", 225, 100, 170, 90, "Data til", "Data til"),
        card(page, "refreshed", 410, 100, 170, 90, "Sist oppdatert (UTC)", "Sist oppdatert (UTC)"),
        textbox(page, "guide", 40, 210, 540, 480, [
            [("Slik bruker du rapporten", BOLD, None)],
            [("1. I Power BI Desktop lastes perioden fra RangeStart til RangeEnd (Transformer data > Rediger "
              f"parametere). Publisert til Power BI-tjenesten holder rapporten de siste {STORE_MONTHS} månedene og "
              f"henter inneværende måned og de {REFRESH_MONTHS - 1} foregående på nytt ved hver oppdatering. Endringer i "
              "registreringer med eldre dato (for eksempel sene godkjenninger) kommer ikke med før rapporten "
              "publiseres på nytt. "
              "Prosjekter, arbeidsordrer, ressurser og brukere lastes alltid i sin helhet.", None, None)],
            [("2. Tabellen til høyre viser hvor mange rader hver tabell har og når dataene sist ble endret i Ditio. "
              "En tom tabell betyr som regel at firmaet ikke bruker den delen av Ditio, eller at perioden er feil.", None, None)],
            [("3. Feltene heter det samme som kolonnene i Ditios Excel-eksport. Siden «Feltordliste» viser hva hvert "
              "felt heter i rapporten, i API-et og i Excel.", None, None)],
            [("4. Bruk målingene i tabellen «Målinger» i egne visualiseringer, ikke summer av kolonner. "
              "Målingene tar hensyn til ting som blandede enheter (tonn og m³).", None, None)],
            [("Planlagt refresh i Power BI-tjenesten: velg Anonym pålogging og personvernnivå Organisasjon for "
              "begge Ditio-adressene, og huk av for «Hopp over testtilkobling».", None, None)],
            [("Mer hjelp: ", None, None), (DOCS_URL, None, DOCS_URL)],
        ]),
        table(page, "sources", 600, 100, 640, 590, [
            ("column", SOURCES_TABLE, "Tabell"),
            ("column", SOURCES_TABLE, "Endepunkt"),
            ("measure", "Rader"),
            ("measure", "Sist endret i data"),
        ], title="Datakilder"),
    ]
    return ("Start", visuals)


def glossary_page():
    page = "glossary"
    visuals = [
        slicer(page, "table", 40, 20, 320, 70, GLOSSARY_TABLE, "Tabell", "Velg tabell"),
        textbox(page, "intro", 380, 20, 860, 70, [
            [("Feltordliste", BOLD, None)],
            [("Hva hvert felt heter i rapporten, i API-et og i Ditios Excel-eksport. "
              "Skjulte nøkkelfelt (id-er) er ikke med.", SUBTLE, None)],
        ]),
        table(page, "fields", 40, 100, 1200, 600, [
            ("column", GLOSSARY_TABLE, c)
            for c in ("Tabell", "Felt i rapporten", "API-felt", "Excel-kolonne", "Beskrivelse")
        ]),
    ]
    return ("Feltordliste", visuals)


def build_layout(pages):
    sections = []
    for index, (display_name, visuals) in enumerate(pages):
        for z, visual in enumerate(visuals):
            visual["z"] = z * 1000
            config = json.loads(visual["config"])
            config["layouts"][0]["position"]["z"] = z * 1000
            visual["config"] = json.dumps(config, ensure_ascii=False)
        sections.append({
            "id": index, "name": f"ReportSection{index}", "displayName": display_name, "filters": "[]",
            "ordinal": index, "config": "{}", "displayOption": 1, "width": PAGE_WIDTH, "height": PAGE_HEIGHT,
            "visualContainers": visuals,
        })
    report_config = {
        "version": REPORT_VERSION,
        "themeCollection": {
            "baseTheme": {"name": "CY19SU06", "version": "5.3", "type": 2},
            "customTheme": {"name": THEME_FILE, "version": REPORT_VERSION, "type": 1},
        },
        "activeSectionIndex": 0,
        "defaultDrillFilterOtherVisuals": True,
        "settings": {"useStylableVisualContainerHeader": True, "exportDataMode": 1,
                     "useNewFilterPaneExperience": True, "allowChangeFilterTypes": True},
    }
    return {
        "id": 0,
        "theme": THEME_FILE,
        "resourcePackages": [
            {"resourcePackage": {"name": "SharedResources", "type": 2, "disabled": False,
                                 "items": [{"type": 202, "path": "BaseThemes/CY19SU06.json", "name": "CY19SU06"}]}},
            {"resourcePackage": {"name": "RegisteredResources", "type": 1, "disabled": False,
                                 "items": [{"type": 100, "path": THEME_FILE, "name": THEME_FILE}]}},
        ],
        "sections": sections,
        "config": json.dumps(report_config),
        "layoutOptimization": 0,
    }
