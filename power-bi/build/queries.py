"""Power Query (M) of the Ditio Power BI template: parameters, shared functions and one
query per table."""

from spec import (
    COLUMN_TYPES, DATE_KEY_COLUMN,
    PRODUCTION_CORE_URL, TEST_CORE_URL,
    PRODUCTION_REPORTING_URL, TEST_REPORTING_URL,
    PRODUCTION_IDENTITY_URL, TEST_IDENTITY_URL,
)


def _url_parameter(name, production, test, description):
    return {
        "name": name,
        "value": f'"{production}"',
        "meta": f'IsParameterQuery=true, List={{"{production}", "{test}"}}, '
                f'DefaultValue="{production}", Type="Text", IsParameterQueryRequired=true',
        "result_type": "Text",
        "description": description,
    }


def _optional_parameter(name, kind, description, required=False):
    return {
        "name": name,
        "value": "null",
        "meta": f'IsParameterQuery=true, Type="{kind}", IsParameterQueryRequired={"true" if required else "false"}',
        "result_type": kind,
        "description": description,
    }


PARAMETERS = [
    _url_parameter("ReportingApiUrl", PRODUCTION_REPORTING_URL, TEST_REPORTING_URL,
                   "Ditio Reporting API (Data Extraction). Produksjon, eller core-api.ditio.dev/reporting for test."),
    _url_parameter("IdentityUrl", PRODUCTION_IDENTITY_URL, TEST_IDENTITY_URL,
                   "Ditio-pålogging som utsteder tilgangsnøkkelen. Må være samme miljø som ReportingApiUrl."),
    _url_parameter("CoreApiUrl", PRODUCTION_CORE_URL, TEST_CORE_URL,
                   "Ditio Core API. Brukes bare av egne spørringer mot Core API; malens tabeller bruker ReportingApiUrl."),
    _optional_parameter("ClientId", "Text",
                        "client_id for en API-klient fra Ditio (Firmaoppsett > Integrasjon). La stå tom hvis du bruker AccessToken."),
    _optional_parameter("ClientSecret", "Text",
                        "client_secret for samme API-klient. La stå tom hvis du bruker AccessToken."),
    _optional_parameter("AccessToken", "Text",
                        "Valgfri. En ferdig tilgangsnøkkel med scope reportingapiv1. Når den er satt, brukes ikke ClientId/ClientSecret."),
    # RangeStart/RangeEnd are Power BI's reserved incremental-refresh parameters (names are case-sensitive).
    # In Power BI Desktop they are the period you load; in the Power BI service the refresh policy sets
    # them per partition, so only recent partitions are re-read on each refresh.
    _optional_parameter("RangeStart", "DateTime",
                        "Starten på perioden som lastes i Power BI Desktop (tas med). I Power BI-tjenesten styres den av oppdateringspolicyen.",
                        required=True),
    _optional_parameter("RangeEnd", "DateTime",
                        "Slutten på perioden som lastes i Power BI Desktop (tas ikke med). I Power BI-tjenesten styres den av oppdateringspolicyen.",
                        required=True),
    _optional_parameter("CompanyId", "Text",
                        "Valgfri. Last bare data registrert i dette Ditio-firmaet. La stå tom for alt API-klienten har tilgang til."),
]

FUNCTIONS = [
    {
        "name": "DitioGetAccessToken",
        "description": "Returnerer AccessToken hvis den er satt, ellers hentes en client-credentials-nøkkel med scope reportingapiv1.",
        "m": """() as text =>
let
    UseProvidedToken = AccessToken <> null and AccessToken <> "",
    Token =
        if UseProvidedToken then
            AccessToken
        else if ClientId = null or ClientId = "" or ClientSecret = null or ClientSecret = "" then
            error Error.Record("Ditio-pålogging mangler", "Fyll ut parameterne ClientId og ClientSecret, eller AccessToken.")
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
                    error Error.Record("Ditio-pålogging feilet", "HTTP " & Text.From(Status) & " fra " & IdentityUrl, Text.FromBinary(Body))
                else
                    Json.Document(Body)[access_token]
in
    Token""",
    },
    {
        "name": "DitioExtract",
        "description": "Kaller et Data Extraction-endepunkt (v1/*) og følger continuationToken til alle sider er lastet. Returnerer listen med poster.",
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
                error Error.Record("Ditio API-kall feilet", "HTTP " & Text.From(Status) & " fra " & path, Text.FromBinary(Body))
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
        "description": "Leser dato/tid slik API-et sender den, uten tidssonekonvertering. Tomme verdier og 0001-01-01 blir tomme.",
        "m": """(value as nullable text) as nullable datetime =>
    if value = null or value = "" or Text.StartsWith(value, "0001-01-01") then
        null
    else
        DateTime.FromText(Text.Start(value, 19), "en-US")""",
    },
    {
        "name": "DitioModifiedWindow",
        "description": "Filteret ModifiedSince fra RangeStart, for endepunkter som ikke filtrerer på FromDateTime/ToDateTime. Alt som er opprettet i perioden, er endret etter RangeStart.",
        "m": """() as record =>
[
    ModifiedSince = Date.ToText(Date.From(RangeStart), "yyyy-MM-dd") & "T00:00:00"
]""",
    },
    {
        "name": "DitioDateWindow",
        "description": "Filteret FromDateTime/ToDateTime fra RangeStart til RangeEnd, hele dager.",
        "m": """() as record =>
[
    // Whole days. The API's ToDateTime is inclusive, so a row exactly at RangeEnd is fetched by two
    // neighbouring partitions; the [RangeStart, RangeEnd) clip in each table keeps it in one.
    FromDateTime = Date.ToText(Date.From(RangeStart), "yyyy-MM-dd") & "T00:00:00",
    ToDateTime = Date.ToText(Date.From(RangeEnd), "yyyy-MM-dd") & "T00:00:00"
]""",
    },
]


def m_text(value):
    return '"' + str(value).replace('"', '""') + '"'


def m_name(name):
    return name if name.isidentifier() and name.isascii() else "#" + m_text(name)


def m_column_types(columns):
    return ",\n        ".join(f'{{{m_text(name)}, {COLUMN_TYPES[kind][0]}}}' for name, kind in columns)


def _mapping_record(mapping):
    return "[" + ", ".join(f"{m_name(key)} = {m_text(value)}" for key, value in mapping.items()) + "]"


def _mapped_steps(table, step):
    lines = []
    for index, (new_field, source_field, mapping, _label) in enumerate(table.get("mapped", [])):
        name = f"Mapped{index + 1}"
        lines += [
            f"    {name} = Table.AddColumn({step}, {m_text(new_field)}, each",
            f"        if [{source_field}] = null then null",
            f"        else Record.FieldOrDefault({_mapping_record(mapping)}, [{source_field}], [{source_field}]),",
            "        type text),",
        ]
        step = name
    return lines, step


def _date_key_step(table, step):
    if "date_key" not in table:
        return [], step
    return [
        f'    WithDateKey = Table.AddColumn({step}, "{DATE_KEY_COLUMN}", each Date.From([{table["date_key"]}]), type date),',
        "    // Keep [RangeStart, RangeEnd) only: incremental-refresh partitions must not overlap.",
        f'    InPeriod = Table.SelectRows(WithDateKey, each [{DATE_KEY_COLUMN}] <> null and [{DATE_KEY_COLUMN}] >= Date.From(RangeStart) and [{DATE_KEY_COLUMN}] < Date.From(RangeEnd)),',
    ], "InPeriod"


def output_columns(table):
    """Columns the query returns, by api field: the spec columns, mapped columns, date key."""
    columns = [(api, kind) for api, kind, _ in table["columns"]]
    columns += [(new_field, "text") for new_field, _, _, _ in table.get("mapped", [])]
    if "per_person" in table:
        columns.append((table["per_person"]["count_field"], "int"))
    if "date_key" in table:
        columns.append((DATE_KEY_COLUMN, "date"))
    return columns


def render_table_query(table):
    filters = ("DitioModifiedWindow()" if table.get("window_by_modified") else "DitioDateWindow()") if table["window"] else "null"
    coordinates = table.get("coordinates", {})
    derived = {f"{prefix}{axis}" for prefix in coordinates.values() for axis in ("Longitude", "Latitude")}
    raw_columns = [api for api, _, _ in table["columns"] if api not in derived] + list(coordinates)
    raw_names = ", ".join(m_text(name) for name in raw_columns)
    spec_columns = [(api, kind) for api, kind, _ in table["columns"]]
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
        removed = ", ".join(m_text(field) for field in coordinates)
        lines.append(f"    WithoutRawCoordinates = Table.RemoveColumns({step}, {{{removed}}}),")
        step = "WithoutRawCoordinates"
    column_order = ", ".join(m_text(api) for api, _ in spec_columns)
    lines += [
        f"    Ordered = Table.ReorderColumns({step}, {{{column_order}}}),",
        "    Dates = Table.TransformColumns(Ordered, {",
        "        " + ",\n        ".join(f'{{{m_text(api)}, DitioDateTimeFromText, type datetime}}'
                                      for api, kind in spec_columns if kind == "datetime"),
        "    }),",
        "    Typed = Table.TransformColumnTypes(Dates, {",
        f"        {m_column_types([c for c in spec_columns if c[1] != 'datetime'])}",
        '    }, "en-US"),',
        "    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),",
    ]
    step = "WithoutDeleted"
    mapped, step = _mapped_steps(table, step)
    lines += mapped
    date_key, step = _date_key_step(table, step)
    lines += date_key
    if table["window"]:
        lines += [f"    Result = {step}", "in", "    Result"]
    elif "per_person" in table:
        lines += _per_person_steps(table, step)
    else:
        lines += [
            "    // Keys must be unique for the model relationships.",
            f'    Result = Table.Distinct({step}, {{"id"}})',
            "in",
            "    Result",
        ]
    return "\n".join(lines)


def _per_person_steps(table, step):
    """One row per person: keep the employer profile, then active, enabled, most recently changed."""
    key = table["per_person"]["key"]
    count = table["per_person"]["count_field"]
    fields = ", ".join(m_text(api) for api, _, _ in table["columns"] if api != key)
    return [
        f"    WithKey = Table.SelectRows({step}, each [{key}] <> null and [{key}] <> \"\"),",
        "    Ranked = Table.AddColumn(WithKey, \"isEmployerProfile\", each [companyId] <> null and [companyId] = [employmentCompanyId], type logical),",
        f"    Grouped = Table.Group(Ranked, {{{m_text(key)}}}, {{",
        "        {\"Profile\", each Table.First(Table.Sort(_, {",
        "            {\"isEmployerProfile\", Order.Descending}, {\"isActiveEmployment\", Order.Descending},",
        "            {\"isDisabled\", Order.Ascending}, {\"modifiedDateTime\", Order.Descending}})), type record},",
        f"        {{{m_text(count)}, each Table.RowCount(_), Int64.Type}}",
        "    }),",
        f"    Expanded = Table.ExpandRecordColumn(Grouped, \"Profile\", {{{fields}}}),",
        "    // Grouping drops column types; restore them.",
        "    Retyped = Table.TransformColumnTypes(Expanded, {",
        f"        {m_column_types([(api, kind) for api, kind, _ in table['columns'] if api != key])}",
        "    }),",
        "    // Keys must be unique for the model relationships.",
        f"    Result = Table.ReorderColumns(Retyped, {{{', '.join(m_text(api) for api, _ in output_columns(table))}}})",
        "in",
        "    Result",
    ]


def render_item_query(table):
    spec_columns = [(api, kind) for api, kind, _ in table["columns"]]
    names = ", ".join(m_text(api) for api, _ in spec_columns)
    number_columns = ", ".join(m_text(api) for api, kind in spec_columns if kind == "number")
    text_types = m_column_types([(n, k) for n, k in spec_columns if k == "text"])
    head = f"""let
    Token = DitioGetAccessToken(),
    // The endpoint takes whole days, both included: RangeStart's day up to the day before RangeEnd.
    FromDateText = Date.ToText(Date.From(RangeStart), "yyyy-MM-dd"),
    ToDateText = Date.ToText(Date.AddDays(Date.From(RangeEnd), -1), "yyyy-MM-dd"),
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
            error Error.Record("Ditio API-kall feilet", "HTTP " & Text.From(Status) & " fra {table['path']}", Text.FromBinary(Body))
        else
            Body,
    // Semicolon-separated, one header row (localised, ignored) then one line per transaction.
    Csv = Csv.Document(Checked, [Delimiter = ";", Columns = {len(spec_columns)}, Encoding = 65001, QuoteStyle = QuoteStyle.Csv]),
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
            (column) => {{column, each if _ = null or _ = "" then null else Number.FromText(Text.Replace(Text.Replace(_, ",", "."), "−", "-"), "en-US"), type number}}
        )
    ),
    TransDate = Table.TransformColumns(
        Numbers,
        {{"transDate", each if _ = null or _ = "" then null else DateTime.FromText(_, [Format = "dd.MM.yyyy HH:mm:ss", Culture = "en-US"]), type datetime}}
    ),
    Typed = Table.TransformColumnTypes(TransDate, {{
        {text_types}
    }}),"""
    lines = head.split("\n")
    step = "Typed"
    mapped, step = _mapped_steps(table, step)
    lines += mapped
    date_key, step = _date_key_step(table, step)
    lines += date_key
    lines += [f"    Result = {step}", "in", "    Result"]
    return "\n".join(lines)


# --------------------------------------------------------------------------- static tables

MONTHS = ["januar", "februar", "mars", "april", "mai", "juni", "juli", "august",
          "september", "oktober", "november", "desember"]
WEEKDAYS = ["mandag", "tirsdag", "onsdag", "torsdag", "fredag", "lørdag", "søndag"]

# (column, M expression of d, Power Query type) for the Dato table, in order.
DATE_COLUMNS = [
    ("Dato", "d", "date"),
    ("År", "Date.Year(d)", "int"),
    ("Kvartal", '"K" & Text.From(Date.QuarterOfYear(d))', "text"),
    ("Månedsnr", "Date.Month(d)", "int"),
    ("Måned", "MonthNames{Date.Month(d) - 1}", "text"),
    ("År-måned", 'Date.ToText(d, "yyyy-MM")', "text"),
    ("ISO-år", "Date.Year(Thursday)", "int"),
    ("Uke", "Number.IntegerDivide(Date.DayOfYear(Thursday) - 1, 7) + 1", "int"),
    ("År-uke", 'Text.From(Date.Year(Thursday)) & "-U" & Text.PadStart(Text.From(Number.IntegerDivide(Date.DayOfYear(Thursday) - 1, 7) + 1), 2, "0")', "text"),
    ("Ukedagnr", "Date.DayOfWeek(d, Day.Monday) + 1", "int"),
    ("Ukedag", "DayNames{Date.DayOfWeek(d, Day.Monday)}", "text"),
    ("Helg", "Date.DayOfWeek(d, Day.Monday) >= 5", "bool"),
]


def render_date_query():
    fields = ",\n                ".join(f"{m_name(name)} = {expression}" for name, expression, _ in DATE_COLUMNS)
    types = m_column_types([(name, kind) for name, _, kind in DATE_COLUMNS])
    months = ", ".join(m_text(m) for m in MONTHS)
    days = ", ".join(m_text(d) for d in WEEKDAYS)
    return f"""let
    // One row per day from three years back (or RangeStart, if earlier) to the end of next year.
    // It doesn't follow RangeStart/RangeEnd alone: in the Power BI service those only describe a partition.
    // Weeks follow ISO 8601 (Monday start), as used in Norway.
    MonthNames = {{{months}}},
    DayNames = {{{days}}},
    Today = Date.From(DateTimeZone.UtcNow()),
    FirstDay = List.Min({{Date.From(RangeStart), Date.StartOfYear(Date.AddYears(Today, -3))}}),
    LastDay = List.Max({{Date.AddDays(Date.From(RangeEnd), -1), Date.EndOfYear(Date.AddYears(Today, 1))}}),
    Days = List.Dates(FirstDay, Duration.Days(LastDay - FirstDay) + 1, #duration(1, 0, 0, 0)),
    Rows = List.Transform(Days, (d) =>
        let
            Thursday = Date.AddDays(d, 3 - Date.DayOfWeek(d, Day.Monday))
        in
            [
                {fields}
            ]),
    AsTable = Table.FromRecords(Rows),
    Typed = Table.TransformColumnTypes(AsTable, {{
        {types}
    }})
in
    Typed"""


def render_static_table(columns, rows):
    """columns: [(name, kind)], rows: list of tuples. A query with no data source."""
    header = ", ".join(f"{m_name(name)} = {COLUMN_TYPES[kind][0].replace('type ', '')}" for name, kind in columns)
    body = ",\n        ".join("{" + ", ".join(m_text(v) for v in row) + "}" for row in rows)
    return f"""let
    Source = #table(
        type table [{header}],
        {{
        {body}
        }}
    )
in
    Source"""


MEASURE_HOST_QUERY = """let
    // Holds the report's measures; it has no data of its own.
    Source = #table(type table [#"Målinger" = text], {})
in
    Source"""

DATA_STATUS_QUERY = """let
    Source = #table(
        type table [#"Oppdatert (UTC)" = datetime],
        {{DateTimeZone.RemoveZone(DateTimeZone.UtcNow())}}
    )
in
    Source"""


def render_parameter(parameter):
    return f'{parameter["value"]} meta [{parameter["meta"]}]'
