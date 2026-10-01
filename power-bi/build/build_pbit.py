#!/usr/bin/env python3
"""Build the Ditio Power BI template (.pbit) and its readable Power Query sources.

Single source of truth for the template: the TABLES / RELATIONSHIPS spec below.
Running this script writes

  power-bi/queries/*.pq            one file per query, for review and copy-paste
  power-bi/Ditio Data Extraction v<VERSION>.pbit

Usage:  python3 power-bi/build/build_pbit.py
Needs only the Python 3 standard library.
"""

import json
import uuid
import zipfile
from pathlib import Path

VERSION = "2.0.0"

HERE = Path(__file__).resolve().parent
POWER_BI_DIR = HERE.parent
STATIC_DIR = HERE / "static"
QUERIES_DIR = POWER_BI_DIR / "queries"
PBIT_PATH = POWER_BI_DIR / f"Ditio Data Extraction v{VERSION}.pbit"

PRODUCTION_REPORTING_URL = "https://core-api.ditio.app/reporting"
TEST_REPORTING_URL = "https://core-api.ditio.dev/reporting"
PRODUCTION_IDENTITY_URL = "https://identity.ditio.app"
TEST_IDENTITY_URL = "https://identity.ditio.dev"

# Column type -> (Power Query type, tabular model dataType)
COLUMN_TYPES = {
    "text": ("type text", "string"),
    "number": ("type number", "double"),
    "int": ("Int64.Type", "int64"),
    "bool": ("type logical", "boolean"),
    "datetime": ("type datetime", "dateTime"),
}

# Each table: query name, endpoint, whether it is filtered by the FromDate/ToDate
# window (facts) or pulled in full (dimensions), and the scalar columns to keep.
# Columns are a subset of the v1/* response schemas; nested arrays are left out.
TABLES = [
    {
        "name": "Projects",
        "path": "v1/project",
        "window": False,
        "description": "Projects visible to the API client (v1/project).",
        "columns": [
            ("id", "text"), ("number", "text"), ("name", "text"),
            ("externalNumber", "text"), ("externalDim01", "text"), ("externalDim02", "text"),
            ("companyId", "text"), ("companyName", "text"),
            ("active", "bool"), ("isExternal", "bool"),
            ("createdDateTime", "datetime"), ("modifiedDateTime", "datetime"),
            ("isDeleted", "bool"),
        ],
    },
    {
        "name": "WorkOrders",
        "path": "v1/project/work-breakdown-structure",
        "window": False,
        "description": "Work orders / WBS (v1/project/work-breakdown-structure).",
        "columns": [
            ("id", "text"), ("projectId", "text"), ("number", "text"), ("name", "text"),
            ("nameWithNumber", "text"), ("chapterId", "text"), ("externalNumber", "text"),
            ("externalDim01", "text"), ("externalDim02", "text"), ("externalPieceWorkId", "text"),
            ("parentName", "text"), ("fullPathName", "text"),
            ("companyId", "text"), ("companyName", "text"), ("isExternal", "bool"),
            ("modifiedDateTime", "datetime"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "Resources",
        "path": "v1/resource",
        "window": False,
        "description": "Machines and other resources (v1/resource).",
        "columns": [
            ("id", "text"), ("number", "text"), ("name", "text"),
            ("typeName", "text"), ("typeBaseName", "text"),
            ("registrationNumber", "text"), ("serialNumber", "text"),
            ("department", "text"), ("emissionClass", "text"), ("buildYear", "int"),
            ("weight", "number"), ("capacity", "number"),
            ("costPrice", "number"), ("price", "number"),
            ("companyId", "text"), ("companyName", "text"),
            ("active", "bool"), ("isExternal", "bool"),
            ("modifiedDateTime", "datetime"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "Users",
        "path": "v1/user",
        "window": False,
        "description": "Users, one row per company profile (v1/user). Personal fields such as birth date and address are left out on purpose.",
        "columns": [
            ("id", "text"), ("identityUserId", "text"), ("employeeNumber", "text"),
            ("name", "text"), ("firstName", "text"), ("lastName", "text"), ("email", "text"),
            ("workTitle", "text"), ("department", "text"),
            ("companyId", "text"), ("companyName", "text"), ("employmentCompanyName", "text"),
            ("isActiveEmployment", "bool"), ("isDisabled", "bool"),
            ("startDate", "datetime"), ("endDate", "datetime"),
            ("modifiedDateTime", "datetime"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "TimeRegistrations",
        "path": "v1/time-registrations",
        "window": True,
        "description": "Project transactions: hours, machine hours and quantities (v1/time-registrations).",
        "columns": [
            ("id", "text"), ("workDate", "datetime"),
            ("startDateTime", "datetime"), ("stopDateTime", "datetime"),
            ("qty", "number"), ("unitName", "text"), ("unitQty", "number"), ("unitPrice", "number"),
            ("price", "number"), ("amount", "number"),
            ("costPriceBase", "number"), ("costPriceMan", "number"), ("costPriceMachine", "number"),
            ("costAmount", "number"), ("costAmountMan", "number"), ("costAmountMachine", "number"),
            ("breakQty", "number"), ("description", "text"), ("descriptionInternal", "text"),
            ("approved", "bool"), ("approvedByName", "text"), ("approvedDateTime", "datetime"),
            ("payrollApproved", "bool"), ("payrollApprovedDateTime", "datetime"),
            ("locked", "bool"), ("lockedByName", "text"), ("lockedDateTime", "datetime"),
            ("invoiced", "bool"),
            ("projectId", "text"), ("projectNumber", "text"), ("projectName", "text"),
            ("projectExternalNumber", "text"), ("projectCompanyId", "text"), ("projectCompanyName", "text"),
            ("projectLeaderName", "text"), ("projectMainForemanName", "text"),
            ("taskId", "text"), ("taskName", "text"), ("taskNameLong", "text"),
            ("taskWbsNumber", "text"), ("taskChapterId", "text"), ("taskPieceWorkId", "text"),
            ("resourceId", "text"), ("resourceNumber", "text"), ("resourceName", "text"),
            ("resourceTypeName", "text"), ("primaryResourceName", "text"), ("machineEmissionClass", "text"),
            ("userId", "text"), ("userName", "text"), ("userEmployeeNumber", "text"),
            ("userCompanyName", "text"), ("userCompanyOrgNr", "text"),
            ("userWorkTitle", "text"), ("userWorkShiftSettingName", "text"),
            ("createdDateTime", "datetime"), ("modifiedDateTime", "datetime"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "MachineRegistrations",
        "path": "v1/machine-registrations",
        "window": True,
        "description": "Machine registrations (v1/machine-registrations).",
        "columns": [
            ("id", "text"), ("startDateTime", "datetime"), ("stopDateTime", "datetime"),
            ("qty", "number"), ("description", "text"), ("approved", "bool"),
            ("machineId", "text"), ("machineName", "text"), ("resourceNumber", "text"),
            ("machineTypeName", "text"),
            ("driverId", "text"), ("driverName", "text"), ("driverCompanyName", "text"),
            ("projectId", "text"), ("projectNumber", "text"), ("projectName", "text"),
            ("projectExternalNumber", "text"), ("projectCompanyName", "text"),
            ("createdDateTime", "datetime"), ("modifiedDateTime", "datetime"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "AbsenceRegistrations",
        "path": "v1/absence-registrations",
        "window": True,
        "description": "Absence, one row per absence day (v1/absence-registrations). Free-text and sick-leave details are left out.",
        "columns": [
            ("id", "text"), ("registrationId", "text"), ("date", "datetime"), ("startTime", "datetime"),
            ("qty", "number"),
            ("absenceTypeId", "text"), ("absenceTypeCode", "text"), ("absenceTypeName", "text"),
            ("employeeNumber", "text"), ("userId", "text"), ("projectId", "text"),
            ("approved", "bool"), ("approvedByName", "text"), ("approvedDateTime", "datetime"),
            ("payrollApproved", "bool"), ("payrollApprovedDateTime", "datetime"),
            ("locked", "bool"), ("lockedDateTime", "datetime"),
            ("pdfUrl", "text"), ("modifiedDateTime", "datetime"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "IncidentRegistrations",
        "path": "v1/incident-registrations",
        "window": True,
        "description": "Alerts / incidents, the old Notifications table (v1/incident-registrations).",
        "columns": [
            ("id", "text"), ("serialNumberWithPrefix", "text"), ("title", "text"), ("description", "text"),
            ("typeName", "text"), ("baseTypeText", "text"), ("statusText", "text"),
            ("riskText", "text"), ("damageTypeText", "text"), ("measures", "text"),
            ("requiresFurtherAction", "bool"), ("resolvedOnLocation", "bool"), ("absenceDays", "number"),
            ("companyName", "text"),
            ("projectId", "text"), ("projectNumber", "text"), ("projectName", "text"),
            ("projectCompanyName", "text"),
            ("taskId", "text"), ("taskName", "text"),
            ("machineId", "text"), ("machineName", "text"),
            ("latitude", "number"), ("longitude", "number"),
            ("createdAt", "datetime"), ("statusUpdatedAt", "datetime"), ("modifiedDateTime", "datetime"),
            ("pdfUrl", "text"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "ChecklistRegistrations",
        "path": "v1/checklist-registrations",
        "window": True,
        "description": "Checklists / forms (v1/checklist-registrations).",
        "columns": [
            ("id", "text"), ("documentNumber", "text"), ("serialNumber", "text"),
            ("templateName", "text"), ("templateVersion", "int"), ("baseTypeName", "text"),
            ("statusText", "text"), ("hasDeviation", "bool"), ("hasComment", "bool"), ("hasImages", "bool"),
            ("projectId", "text"), ("projectNumber", "text"), ("projectName", "text"),
            ("activityNumber", "text"), ("activityName", "text"),
            ("machineId", "text"), ("resourceNumber", "text"),
            ("createdByUserName", "text"), ("createdDateTime", "datetime"),
            ("submittedByUserName", "text"), ("submittedDateTime", "datetime"),
            ("approvedByUserName", "text"),
            ("latitude", "number"), ("longitude", "number"),
            ("modifiedDateTime", "datetime"), ("pdfUrl", "text"), ("isDeleted", "bool"),
        ],
    },
    {
        "name": "FlowTripRegistrations",
        "path": "v1/flow-trip-registrations",
        "window": True,
        "description": "Mass transport trips from Ditio Flow and legacy mass haul, the old TripLog table (v1/flow-trip-registrations).",
        "columns": [
            ("id", "text"), ("loadDateTime", "datetime"), ("dumpDateTime", "datetime"),
            ("costStartDateTime", "datetime"), ("costEndDateTime", "datetime"),
            ("quantity", "number"), ("quantityM3", "number"),
            ("registeredQuantity", "number"), ("verifiedQuantity", "number"),
            ("unitOfMeasureText", "text"), ("massTypeId", "text"), ("massType", "text"),
            ("m3PerHour", "number"), ("totalCycleTimeSeconds", "int"), ("totalCycleTimeHours", "number"),
            ("utilizationRate", "number"), ("distance", "number"), ("averageSpeedKmh", "number"),
            ("netAltitudeChange", "number"), ("totalAscent", "number"),
            ("maxGradient", "number"), ("averageGradient", "number"),
            ("loadLocationName", "text"), ("dumpLocationName", "text"),
            ("loadLongitude", "number"), ("loadLatitude", "number"),
            ("dumpLongitude", "number"), ("dumpLatitude", "number"),
            ("loaderId", "text"), ("loaderName", "text"), ("loaderNumber", "text"), ("loaderDriverName", "text"),
            ("dumperId", "text"), ("dumperName", "text"), ("dumperNumber", "text"),
            ("dumperRegistrationNumber", "text"), ("dumperDriverName", "text"), ("dumperDriverCompanyName", "text"),
            ("trailer", "bool"), ("verified", "bool"), ("approved", "bool"), ("invoiced", "bool"),
            ("receiptNumber", "text"), ("customerName", "text"), ("comment", "text"),
            ("projectId", "text"), ("projectNumber", "text"), ("projectName", "text"),
            ("projectExternalNumber", "text"), ("projectCompanyName", "text"),
            ("taskId", "text"), ("taskName", "text"),
            ("isFlowData", "bool"), ("modifiedDateTime", "datetime"), ("isDeleted", "bool"),
        ],
        # GeoJSON points are [longitude, latitude]; split them into number columns.
        "coordinates": {"loadCoordinates": "load", "dumpCoordinates": "dump"},
    },
]

# v1/item-registrations/web-query returns semicolon-separated text, not JSON, and is
# not paginated. Its header row is localised and does not line up with the data rows,
# so the query skips it and names the columns by position.
ITEM_TABLE = {
    "name": "ItemRegistrations",
    "path": "v1/item-registrations/web-query",
    "description": "Item (goods) transactions (v1/item-registrations/web-query).",
    "columns": [
        ("itemName", "text"), ("projectName", "text"), ("projectNumber", "text"), ("taskName", "text"),
        ("transType", "text"), ("userName", "text"), ("transDate", "datetime"), ("machineName", "text"),
        ("qty", "number"), ("costPrice", "number"), ("price", "number"),
        ("costAmount", "number"), ("amount", "number"),
        ("warehouseName", "text"), ("warehouseQty", "number"), ("description", "text"),
    ],
}

# Only keys proven unique on the dimension side and populated from the same id space on
# the fact side. WorkOrders -> Projects is deliberately not related: it would give
# TimeRegistrations two paths to Projects.
RELATIONSHIPS = [
    ("TimeRegistrations", "projectId", "Projects", "id"),
    ("TimeRegistrations", "taskId", "WorkOrders", "id"),
    ("TimeRegistrations", "resourceId", "Resources", "id"),
    ("MachineRegistrations", "projectId", "Projects", "id"),
    ("MachineRegistrations", "machineId", "Resources", "id"),
    ("AbsenceRegistrations", "projectId", "Projects", "id"),
    ("IncidentRegistrations", "projectId", "Projects", "id"),
    ("IncidentRegistrations", "taskId", "WorkOrders", "id"),
    ("ChecklistRegistrations", "projectId", "Projects", "id"),
    ("FlowTripRegistrations", "projectId", "Projects", "id"),
    ("FlowTripRegistrations", "taskId", "WorkOrders", "id"),
]

# --------------------------------------------------------------------------- parameters

PARAMETERS = [
    {
        "name": "ReportingApiUrl",
        "value": f'"{PRODUCTION_REPORTING_URL}"',
        "meta": f'IsParameterQuery=true, List={{"{PRODUCTION_REPORTING_URL}", "{TEST_REPORTING_URL}"}}, '
                f'DefaultValue="{PRODUCTION_REPORTING_URL}", Type="Text", IsParameterQueryRequired=true',
        "result_type": "Text",
        "description": "Ditio Reporting API. Production unless you are testing against core-api.ditio.dev.",
    },
    {
        "name": "IdentityUrl",
        "value": f'"{PRODUCTION_IDENTITY_URL}"',
        "meta": f'IsParameterQuery=true, List={{"{PRODUCTION_IDENTITY_URL}", "{TEST_IDENTITY_URL}"}}, '
                f'DefaultValue="{PRODUCTION_IDENTITY_URL}", Type="Text", IsParameterQueryRequired=true',
        "result_type": "Text",
        "description": "Ditio identity server that issues the access token. Must match the environment of ReportingApiUrl.",
    },
    {
        "name": "ClientId",
        "value": "null",
        "meta": 'IsParameterQuery=true, Type="Text", IsParameterQueryRequired=false',
        "result_type": "Text",
        "description": "client_id of a Ditio API client (Company Setup > Integration). Leave empty when using AccessToken.",
    },
    {
        "name": "ClientSecret",
        "value": "null",
        "meta": 'IsParameterQuery=true, Type="Text", IsParameterQueryRequired=false',
        "result_type": "Text",
        "description": "client_secret of the same API client. Leave empty when using AccessToken.",
    },
    {
        "name": "AccessToken",
        "value": "null",
        "meta": 'IsParameterQuery=true, Type="Text", IsParameterQueryRequired=false',
        "result_type": "Text",
        "description": "Optional. A ready-made access token with the reportingapiv1 scope. When set, ClientId/ClientSecret are not used.",
    },
    {
        "name": "FromDate",
        "value": "null",
        "meta": 'IsParameterQuery=true, Type="Date", IsParameterQueryRequired=true',
        "result_type": "Date",
        "description": "First day of registrations to load (inclusive).",
    },
    {
        "name": "ToDate",
        "value": "null",
        "meta": 'IsParameterQuery=true, Type="Date", IsParameterQueryRequired=true',
        "result_type": "Date",
        "description": "Last day of registrations to load (inclusive).",
    },
    {
        "name": "CompanyId",
        "value": "null",
        "meta": 'IsParameterQuery=true, Type="Text", IsParameterQueryRequired=false',
        "result_type": "Text",
        "description": "Optional. Only return data registered in this Ditio company. Leave empty to get everything the API client can see.",
    },
]

# --------------------------------------------------------------------------- functions

FUNCTIONS = [
    {
        "name": "DitioGetAccessToken",
        "description": "Returns AccessToken if set, otherwise requests a client-credentials token with the reportingapiv1 scope.",
        "m": """() as text =>
let
    UseProvidedToken = AccessToken <> null and AccessToken <> "",
    Token =
        if UseProvidedToken then
            AccessToken
        else if ClientId = null or ClientId = "" or ClientSecret = null or ClientSecret = "" then
            error Error.Record("Ditio credentials missing", "Set the ClientId and ClientSecret parameters, or AccessToken.")
        else
            let
                Response = Web.Contents(
                    IdentityUrl,
                    [
                        RelativePath = "connect/token",
                        Headers = [#"Content-Type" = "application/x-www-form-urlencoded", Accept = "application/json"],
                        Content = Text.ToBinary(
                            Uri.BuildQueryString(
                                [
                                    grant_type = "client_credentials",
                                    client_id = ClientId,
                                    client_secret = ClientSecret,
                                    scope = "reportingapiv1"
                                ]
                            )
                        ),
                        ManualStatusHandling = {400, 401, 403}
                    ]
                ),
                Body = Binary.Buffer(Response),
                Status = Value.Metadata(Response)[Response.Status]
            in
                if Status >= 400 then
                    error Error.Record("Ditio token request failed", "HTTP " & Text.From(Status) & " from " & IdentityUrl, Text.FromBinary(Body))
                else
                    Json.Document(Body)[access_token]
in
    Token""",
    },
    {
        "name": "DitioExtract",
        "description": "Calls a Data Extraction v1/* endpoint and follows continuationToken until every page is loaded. Returns the list of records.",
        "m": """(path as text, optional filters as nullable record) as list =>
let
    Token = DitioGetAccessToken(),
    CompanyFilter = if CompanyId = null or CompanyId = "" then [] else Record.FromList({CompanyId}, {"CompanyId"}),
    BaseQuery = (if filters = null then [] else filters) & CompanyFilter & [ChunkLimit = "5000"],
    GetPage = (continuationToken as nullable text) as record =>
        let
            PageQuery = if continuationToken = null then BaseQuery else BaseQuery & [ContinuationToken = continuationToken],
            Response = Web.Contents(
                ReportingApiUrl,
                [
                    RelativePath = path,
                    Query = PageQuery,
                    Headers = [Authorization = "Bearer " & Token, Accept = "application/json"],
                    Timeout = #duration(0, 0, 10, 0),
                    ManualStatusHandling = {400, 401, 403, 404, 500, 502, 503}
                ]
            ),
            Body = Binary.Buffer(Response),
            Status = Value.Metadata(Response)[Response.Status]
        in
            if Status >= 400 then
                error Error.Record("Ditio API request failed", "HTTP " & Text.From(Status) & " from " & path, Text.FromBinary(Body))
            else
                Json.Document(Body),
    Pages = List.Generate(
        () => GetPage(null),
        each _ <> null,
        each
            let
                Next = Record.FieldOrDefault(_, "continuationToken", null)
            in
                if Next = null or Next = "" then null else GetPage(Next),
        each Record.FieldOrDefault(_, "data", {})
    ),
    Records = List.Buffer(List.Combine(List.Transform(Pages, each if _ = null then {} else _)))
in
    Records""",
    },
    {
        "name": "DitioDateTimeFromText",
        "description": "Parses an API date/time as the wall-clock value the API returns, without time-zone conversion. Empty and 0001-01-01 values become null.",
        "m": """(value as nullable text) as nullable datetime =>
    if value = null or value = "" or Text.StartsWith(value, "0001-01-01") then
        null
    else
        DateTime.FromText(Text.Start(value, 19), "en-US")""",
    },
    {
        "name": "DitioDateWindow",
        "description": "FromDateTime/ToDateTime query filter covering FromDate up to and including ToDate.",
        "m": """() as record =>
[
    FromDateTime = Date.ToText(FromDate, "yyyy-MM-dd") & "T00:00:00",
    ToDateTime = Date.ToText(ToDate, "yyyy-MM-dd") & "T23:59:59"
]""",
    },
]


# --------------------------------------------------------------------------- M rendering

def m_column_types(columns):
    return ",\n        ".join(f'{{"{name}", {COLUMN_TYPES[kind][0]}}}' for name, kind in columns)


def render_table_query(table):
    filters = "DitioDateWindow()" if table["window"] else "null"
    coordinates = table.get("coordinates", {})
    # Coordinate pairs are fetched as raw lists and split before typing.
    api_columns = [(name, kind) for name, kind in table["columns"]
                   if not any(name.startswith(prefix) and name[len(prefix):] in ("Longitude", "Latitude")
                              for prefix in coordinates.values())]
    raw_columns = api_columns + [(field, "any") for field in coordinates]

    raw_names = ", ".join(f'"{name}"' for name, _ in raw_columns)
    lines = [
        "let",
        f'    Records = DitioExtract("{table["path"]}", {filters}),',
        f"    Columns = {{{raw_names}}},",
        "    Raw =",
        "        if List.IsEmpty(Records) then",
        "            #table(Columns, {})",
        "        else",
        "            Table.ExpandRecordColumn(",
        '                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),',
        '                "Record",',
        "                Columns",
        "            ),",
    ]
    step = "Raw"
    for field, prefix in coordinates.items():
        lines += [
            f'    {prefix.capitalize()}Split = Table.AddColumn(Table.AddColumn({step},',
            f'        "{prefix}Longitude", each try [{field}]{{0}} otherwise null),',
            f'        "{prefix}Latitude", each try [{field}]{{1}} otherwise null),',
        ]
        step = f"{prefix.capitalize()}Split"
    if coordinates:
        removed = ", ".join(f'"{field}"' for field in coordinates)
        lines.append(f"    WithoutRawCoordinates = Table.RemoveColumns({step}, {{{removed}}}),")
        step = "WithoutRawCoordinates"
    column_order = ", ".join(f'"{name}"' for name, _ in table["columns"])
    lines += [
        f"    Ordered = Table.ReorderColumns({step}, {{{column_order}}}),",
        "    Dates = Table.TransformColumns(Ordered, {",
        "        " + ",\n        ".join(f'{{"{name}", DitioDateTimeFromText, type datetime}}'
                                      for name, kind in table["columns"] if kind == "datetime"),
        "    }),",
        "    Typed = Table.TransformColumnTypes(Dates, {",
        f"        {m_column_types([c for c in table['columns'] if c[1] != 'datetime'])}",
        '    }, "en-US"),',
        "    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),",
    ]
    if table["window"]:
        lines += ["    Result = WithoutDeleted", "in", "    Result"]
    else:
        lines += [
            "    // Keys must be unique for the model relationships.",
            '    Result = Table.Distinct(WithoutDeleted, {"id"})',
            "in",
            "    Result",
        ]
    return "\n".join(lines)


def render_item_query(table):
    names = ", ".join(f'"{name}"' for name, _ in table["columns"])
    number_columns = ", ".join(f'"{name}"' for name, kind in table["columns"] if kind == "number")
    text_types = m_column_types([(n, k) for n, k in table["columns"] if k == "text"])
    return f"""let
    Token = DitioGetAccessToken(),
    // Bound outside the Query record: inside it, FromDate/ToDate would refer to the record's own fields.
    FromDateText = Date.ToText(FromDate, "yyyy-MM-dd"),
    ToDateText = Date.ToText(ToDate, "yyyy-MM-dd"),
    Response = Web.Contents(
        ReportingApiUrl,
        [
            RelativePath = "{table['path']}",
            Query = [FromDate = FromDateText, ToDate = ToDateText],
            Headers = [Authorization = "Bearer " & Token],
            Timeout = #duration(0, 0, 10, 0),
            ManualStatusHandling = {{400, 401, 403, 404, 500, 502, 503}}
        ]
    ),
    Body = Binary.Buffer(Response),
    Status = Value.Metadata(Response)[Response.Status],
    Checked =
        if Status >= 400 then
            error Error.Record("Ditio API request failed", "HTTP " & Text.From(Status) & " from {table['path']}", Text.FromBinary(Body))
        else
            Body,
    // Semicolon-separated, one header row (localised, ignored) then one line per transaction.
    Csv = Csv.Document(Checked, [Delimiter = ";", Columns = {len(table['columns'])}, Encoding = 65001, QuoteStyle = QuoteStyle.Csv]),
    WithoutHeader = Table.Skip(Csv, 1),
    Named = Table.RenameColumns(
        WithoutHeader,
        List.Zip({{Table.ColumnNames(WithoutHeader), {{{names}}}}})
    ),
    // Numbers use the server's culture: accept "1,5" and "1.5", and the Unicode minus sign.
    Numbers = Table.TransformColumns(
        Named,
        List.Transform(
            {{{number_columns}}},
            (column) => {{column, each if _ = null or _ = "" then null else Number.FromText(Text.Replace(Text.Replace(_, ",", "."), "\u2212", "-"), "en-US"), type number}}
        )
    ),
    TransDate = Table.TransformColumns(
        Numbers,
        {{"transDate", each if _ = null or _ = "" then null else DateTime.FromText(_, [Format = "dd.MM.yyyy HH:mm:ss", Culture = "en-US"]), type datetime}}
    ),
    Result = Table.TransformColumnTypes(TransDate, {{
        {text_types}
    }})
in
    Result"""


def render_parameter(parameter):
    return f'{parameter["value"]} meta [{parameter["meta"]}]'


# --------------------------------------------------------------------------- model

NOT_SUMMED_SUFFIXES = ("year", "version", "latitude", "longitude", "price", "gradient", "speedkmh", "rate", "perhour")


def lineage(*parts):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, "ditio-powerbi/" + "/".join(parts)))


def model_column(table_name, name, kind):
    data_type = COLUMN_TYPES[kind][1]
    column = {
        "name": name,
        "dataType": data_type,
        "sourceColumn": name,
        "lineageTag": lineage(table_name, name),
        "summarizeBy": "sum" if data_type in ("double", "int64") and not name.lower().endswith(NOT_SUMMED_SUFFIXES) else "none",
        "annotations": [{"name": "SummarizationSetBy", "value": "Automatic"}],
    }
    if data_type == "dateTime":
        column["formatString"] = "General Date"
    return column


def model_table(name, description, columns, m):
    return {
        "name": name,
        "description": description,
        "lineageTag": lineage(name),
        "columns": [model_column(name, column, kind) for column, kind in columns],
        "partitions": [{
            "name": f"{name}-partition",
            "mode": "import",
            "queryGroup": "Data",
            "source": {"type": "m", "expression": m.split("\n")},
        }],
        "annotations": [
            {"name": "PBI_NavigationStepName", "value": "Navigation"},
            {"name": "PBI_ResultType", "value": "Table"},
        ],
    }


def model_expression(name, m, group, result_type, description):
    return {
        "name": name,
        "kind": "m",
        "expression": m.split("\n"),
        "queryGroup": group,
        "description": description,
        "lineageTag": lineage("expression", name),
        "annotations": [
            {"name": "PBI_NavigationStepName", "value": "Navigation"},
            {"name": "PBI_ResultType", "value": result_type},
        ],
    }


def build_model(table_queries):
    tables = [model_table(t["name"], t["description"], t["columns"], m) for t, m in table_queries]
    expressions = [model_expression(p["name"], render_parameter(p), "Parameters", p["result_type"], p["description"])
                   for p in PARAMETERS]
    expressions += [model_expression(f["name"], f["m"], "Functions", "Function", f["description"]) for f in FUNCTIONS]
    relationships = [{
        "name": lineage("relationship", from_table, from_column, to_table, to_column),
        "fromTable": from_table, "fromColumn": from_column,
        "toTable": to_table, "toColumn": to_column,
    } for from_table, from_column, to_table, to_column in RELATIONSHIPS]
    query_order = [p["name"] for p in PARAMETERS] + [f["name"] for f in FUNCTIONS] + [t["name"] for t, _ in table_queries]
    return {
        "name": lineage("model"),
        "compatibilityLevel": 1550,
        "model": {
            "culture": "en-US",
            "dataAccessOptions": {"legacyRedirects": True, "returnErrorValuesAsNull": True},
            "defaultPowerBIDataSourceVersion": "powerBI_V3",
            "sourceQueryCulture": "en-US",
            "tables": tables,
            "relationships": relationships,
            "expressions": expressions,
            "queryGroups": [
                {"folder": "Parameters", "annotations": [{"name": "PBI_QueryGroupOrder", "value": "0"}]},
                {"folder": "Functions", "annotations": [{"name": "PBI_QueryGroupOrder", "value": "1"}]},
                {"folder": "Data", "annotations": [{"name": "PBI_QueryGroupOrder", "value": "2"}]},
            ],
            "annotations": [
                {"name": "__PBI_TimeIntelligenceEnabled", "value": "0"},
                {"name": "PBI_QueryOrder", "value": json.dumps(query_order)},
            ],
        },
    }


# --------------------------------------------------------------------------- report

def card(table_name, key_column, index):
    per_row, width, height, gap = 4, 280, 120, 20
    x = 40 + (index % per_row) * (width + gap)
    y = 100 + (index // per_row) * (height + gap)
    query_name = f"Count({table_name}.{key_column})"
    display = f"{table_name} rows"
    column = lambda source: {"Column": {"Expression": {"SourceRef": source}, "Property": key_column}}
    aggregation = lambda source: {"Aggregation": {"Expression": column(source), "Function": 2}}
    prototype = {
        "Version": 2,
        "From": [{"Name": "t", "Entity": table_name, "Type": 0}],
        "Select": [{**aggregation({"Source": "t"}), "Name": query_name, "NativeReferenceName": display}],
    }
    config = {
        "name": lineage("visual", table_name).replace("-", "")[:20],
        "layouts": [{"id": 0, "position": {"x": x, "y": y, "z": index, "width": width, "height": height}}],
        "singleVisual": {
            "visualType": "cardVisual",
            "projections": {"Data": [{"queryRef": query_name}]},
            "prototypeQuery": prototype,
            "columnProperties": {query_name: {"displayName": display}},
            "drillFilterOtherVisuals": True,
        },
    }
    return {
        "x": x, "y": y, "z": index, "width": width, "height": height,
        "config": json.dumps(config), "filters": "[]",
    }


def build_layout(cards):
    report_config = {
        "version": "5.53",
        "themeCollection": {"baseTheme": {"name": "CY19SU06", "version": "5.3", "type": 2}},
        "activeSectionIndex": 0,
        "defaultDrillFilterOtherVisuals": True,
        "settings": {"useStylableVisualContainerHeader": True, "exportDataMode": 1,
                     "useNewFilterPaneExperience": True, "allowChangeFilterTypes": True},
    }
    return {
        "id": 0,
        "resourcePackages": [{"resourcePackage": {
            "name": "SharedResources", "type": 2, "disabled": False,
            "items": [{"type": 202, "path": "BaseThemes/CY19SU06.json", "name": "CY19SU06"}],
        }}],
        "sections": [{
            "id": 0, "name": "ReportSection", "displayName": "Overview", "filters": "[]",
            "ordinal": 0, "config": "{}", "displayOption": 1, "width": 1280, "height": 720,
            "visualContainers": [card(name, key, i) for i, (name, key) in enumerate(cards)],
        }],
        "config": json.dumps(report_config),
        "layoutOptimization": 0,
    }


def build_diagram(table_names):
    return {"version": "1.1.0", "diagrams": [{
        "ordinal": 0, "scrollPosition": {"x": 0, "y": 0}, "name": "All tables",
        "nodes": [{"location": {"x": 40 + (i % 6) * 280, "y": 40 + (i // 6) * 420},
                   "nodeIndex": name, "size": {"height": 360, "width": 234}, "zIndex": i}
                  for i, name in enumerate(table_names)],
    }]}


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


def utf16(value):
    text = value if isinstance(value, str) else json.dumps(value, ensure_ascii=False)
    return text.encode("utf-16-le")


def main():
    table_queries = [(t, render_table_query(t)) for t in TABLES]
    table_queries.append((ITEM_TABLE, render_item_query(ITEM_TABLE)))

    QUERIES_DIR.mkdir(exist_ok=True)
    for stale in QUERIES_DIR.glob("*.pq"):
        stale.unlink()
    for p in PARAMETERS:
        (QUERIES_DIR / f"{p['name']}.pq").write_text(f"// {p['description']}\n{render_parameter(p)}\n")
    for f in FUNCTIONS:
        (QUERIES_DIR / f"{f['name']}.pq").write_text(f"// {f['description']}\n{f['m']}\n")
    for t, m in table_queries:
        (QUERIES_DIR / f"{t['name']}.pq").write_text(f"// {t['description']}\n{m}\n")

    metadata = {
        "Version": 5, "AutoCreatedRelationships": [], "CreatedFrom": "Cloud", "CreatedFromRelease": "2024.04",
        "FileDescription": (
            f"Ditio Data Extraction template v{VERSION}. Loads projects, work orders, resources, users, "
            "time, machine, absence, incident, checklist, mass transport and item registrations from the "
            "Ditio Reporting API (v1/*). Fill in ClientId/ClientSecret (or AccessToken) and the date window."
        ),
    }
    cards = [(t["name"], t["columns"][0][0]) for t, _ in table_queries]

    with zipfile.ZipFile(PBIT_PATH, "w", zipfile.ZIP_DEFLATED) as archive:
        def write(name, data):
            # A fixed timestamp keeps the .pbit byte-identical across rebuilds.
            archive.writestr(zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0)), data, zipfile.ZIP_DEFLATED)

        write("Version", (STATIC_DIR / "Version").read_bytes())
        write("[Content_Types].xml", CONTENT_TYPES.encode("utf-8"))
        write("DataModelSchema", utf16(json.dumps(build_model(table_queries), indent=2, ensure_ascii=False)))
        write("DiagramLayout", utf16(build_diagram([t["name"] for t, _ in table_queries])))
        write("Report/Layout", utf16(build_layout(cards)))
        write("Settings", (STATIC_DIR / "Settings").read_bytes())
        write("Metadata", utf16(metadata))
        theme = "Report/StaticResources/SharedResources/BaseThemes/CY19SU06.json"
        write(theme, (STATIC_DIR / theme).read_bytes())

    print(f"Wrote {PBIT_PATH.relative_to(POWER_BI_DIR.parent)} and {len(list(QUERIES_DIR.glob('*.pq')))} queries")


if __name__ == "__main__":
    main()
