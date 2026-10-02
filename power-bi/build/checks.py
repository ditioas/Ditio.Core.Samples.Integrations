"""Checks run on every build. Power BI Desktop can't run here, so these catch what they can:
labels, names, DAX and visual references, and anything that must never ship in a public file."""

import hashlib
import json
import re

# Ditio company ids are six digits starting with 0. Hex colours are excluded.
COMPANY_ID = re.compile(r"(?<![#\w])0\d{5}(?!\w)")
SECRET_PATTERNS = {
    "JWT / access token": re.compile(r"eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}"),
    "client secret value": re.compile(r"client_secret\s*[=:]\s*[\"']?(?!ClientSecret\b)[A-Za-z0-9+/_-]{12,}", re.I),
    "e-mail address": re.compile(r"[\w.+-]+@[\w-]+(?:\.[\w-]+)+"),
    "company id": COMPANY_ID,
    "internal host": re.compile(r"azurewebsites\.net", re.I),
}
ALLOWED_EMAILS = {"support@ditio.no"}
# Customer names and internal names must never ship. This repository is public, so the list
# itself is stored as truncated SHA-256 hashes of lower-case words and word pairs. To add a
# term: hashlib.sha256("term".encode()).hexdigest()[:16]
DENIED_TERM_HASHES = {
    "076560c8becc6055",
    "0bbbbc0caec1a100",
    "0cedd06ccd12fefe",
    "1149abe4008b9393",
    "13a520640eb8f379",
    "174fdeba4b82e24b",
    "2c94c18e8404a246",
    "3a70f377cf443e9c",
    "3dbc6109cafe8e69",
    "455d023b28a75a77",
    "47c7e2bb224d8cd2",
    "55a929e331745cfe",
    "575bc2120c00bb46",
    "68f866e016bb70da",
    "a94173aeddb3d704",
    "b1763271fb0f4804",
    "c813fe5851087346",
    "fb7cf248dde72f45",
}


def _denied_terms(text):
    words = re.findall(r"[\wæøå-]+", text.lower())
    candidates = set(words) | {f"{a} {b}" for a, b in zip(words, words[1:])}
    return sorted(c for c in candidates if hashlib.sha256(c.encode()).hexdigest()[:16] in DENIED_TERM_HASHES)


def fail(errors):
    if errors:
        raise SystemExit("Build check failed:\n  - " + "\n  - ".join(errors))


def check_labels(data_tables, labels):
    errors = []
    for table in data_tables:
        seen = {}
        for api, _, label, _ in labels.columns(table):
            if not label:
                errors.append(f"{table['name']}.{api}: no label")
            if label.lower() in seen:
                errors.append(f"{table['name']}: label {label!r} used by both {seen[label.lower()]} and {api}")
            seen[label.lower()] = api
    fail(errors)


def check_model(model):
    """Measure names, DAX references, relationships and sort columns."""
    errors = []
    tables = {t["name"]: t for t in model["model"]["tables"]}
    columns = {name: {c["name"] for c in t["columns"]} for name, t in tables.items()}
    measures = {m["name"] for t in tables.values() for m in t.get("measures", [])}
    all_column_names = {c.lower() for cols in columns.values() for c in cols}

    for name in measures:
        if name.lower() in all_column_names:
            errors.append(f"measure {name!r} has the same name as a column")

    for table in tables.values():
        for measure in table.get("measures", []):
            dax = "\n".join(measure["expression"])
            for ref_table in re.findall(r"'([^']+)'", dax):
                if ref_table not in columns:
                    errors.append(f"measure {measure['name']!r}: unknown table {ref_table!r}")
            for ref_table, ref_column in re.findall(r"'([^']+)'\[([^\]]+)\]", dax):
                if ref_table not in columns:
                    errors.append(f"measure {measure['name']!r}: unknown table {ref_table!r}")
                elif ref_column not in columns[ref_table]:
                    errors.append(f"measure {measure['name']!r}: unknown column '{ref_table}'[{ref_column}]")
            for ref in re.findall(r"(?<!')\[([^\]]+)\]", dax):
                # [Value] is the column of a DAX table constructor, e.g. MINX({a, b}, [Value]).
                if ref not in measures and ref != "Value":
                    errors.append(f"measure {measure['name']!r}: unknown measure [{ref}]")
            if dax.count("(") != dax.count(")"):
                errors.append(f"measure {measure['name']!r}: unbalanced parentheses")
        policy = table.get("refreshPolicy")
        if policy:
            source = "\n".join(policy["sourceExpression"])
            if "RangeStart" not in source or "RangeEnd" not in source:
                errors.append(f"{table['name']}: incremental refresh policy but the query doesn't filter on RangeStart/RangeEnd")
        for column in table["columns"]:
            sort_by = column.get("sortByColumn")
            if sort_by and sort_by not in columns[table["name"]]:
                errors.append(f"{table['name']}.{column['name']}: sortByColumn {sort_by!r} missing")

    for rel in model["model"]["relationships"]:
        for side in ("from", "to"):
            t, c = rel[f"{side}Table"], rel[f"{side}Column"]
            if t not in columns or c not in columns[t]:
                errors.append(f"relationship {rel['fromTable']}->{rel['toTable']}: missing {t}[{c}]")
    fail(errors)
    return columns, measures


def check_layout(layout, columns, measures):
    """Every field a visual uses must exist in the model."""
    errors = []
    for section in layout["sections"]:
        for visual in section["visualContainers"]:
            single = json.loads(visual["config"]).get("singleVisual", {})
            query = single.get("prototypeQuery")
            if not query:
                continue
            aliases = {f["Name"]: f["Entity"] for f in query["From"]}
            for select in query["Select"] + [o["Expression"] for o in query.get("OrderBy", [])]:
                if "Measure" in select:
                    if select["Measure"]["Property"] not in measures:
                        errors.append(f"{section['displayName']}: unknown measure {select['Measure']['Property']!r}")
                elif "Column" in select:
                    entity = aliases[select["Column"]["Expression"]["SourceRef"]["Source"]]
                    if select["Column"]["Property"] not in columns.get(entity, set()):
                        errors.append(f"{section['displayName']}: unknown column {entity}[{select['Column']['Property']}]")
            refs = {s["Name"] for s in query["Select"]}
            for role in single.get("projections", {}).values():
                for projection in role:
                    if projection["queryRef"] not in refs:
                        errors.append(f"{section['displayName']}: projection {projection['queryRef']!r} not selected")
    fail(errors)


def check_publication_safety(named_texts):
    """named_texts: [(name, text)] of everything that ships."""
    errors = []
    for name, text in named_texts:
        for label, pattern in SECRET_PATTERNS.items():
            for match in pattern.findall(text):
                if label == "e-mail address" and match.lower() in ALLOWED_EMAILS:
                    continue
                errors.append(f"{name}: {label}: {match[:40]}")
        for term in _denied_terms(text):
            errors.append(f"{name}: denied name: {term}")
    fail(errors)
