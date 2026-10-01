// Varetransaksjoner: forbruk, salg, innkjøp og justering (v1/item-registrations/web-query).
let
    Token = DitioGetAccessToken(),
    // The endpoint takes whole days, both included: RangeStart's day up to the day before RangeEnd.
    FromDateText = Date.ToText(Date.From(RangeStart), "yyyy-MM-dd"),
    ToDateText = Date.ToText(Date.AddDays(Date.From(RangeEnd), -1), "yyyy-MM-dd"),
    Response = Web.Contents(
        ReportingApiUrl,
        [
            RelativePath = "v1/item-registrations/web-query",
            Query = [FromDate = FromDateText, ToDate = ToDateText],
            Headers = [Authorization = "Bearer " & Token],
            Timeout = #duration(0, 0, 10, 0),
            ManualStatusHandling = {400, 401, 403, 404, 500, 502, 503}
        ]
    ),
    Body = Binary.Buffer(Response),
    Status = Value.Metadata(Response)[Response.Status],
    Checked =
        if Status >= 400 then
            error Error.Record("Ditio API-kall feilet", "HTTP " & Text.From(Status) & " fra v1/item-registrations/web-query", Text.FromBinary(Body))
        else
            Body,
    // Semicolon-separated, one header row (localised, ignored) then one line per transaction.
    Csv = Csv.Document(Checked, [Delimiter = ";", Columns = 16, Encoding = 65001, QuoteStyle = QuoteStyle.Csv]),
    WithoutHeader = Table.Skip(Csv, 1),
    Named = Table.RenameColumns(
        WithoutHeader,
        List.Zip({Table.ColumnNames(WithoutHeader), {"itemName", "projectName", "projectNumber", "taskName", "transType", "userName", "transDate", "machineName", "qty", "costPrice", "price", "costAmount", "amount", "warehouseName", "warehouseQty", "description"}})
    ),
    // Numbers use the server's culture: accept "1,5" and "1.5", and the Unicode minus sign.
    Numbers = Table.TransformColumns(
        Named,
        List.Transform(
            {"qty", "costPrice", "price", "costAmount", "amount", "warehouseQty"},
            (column) => {column, each if _ = null or _ = "" then null else Number.FromText(Text.Replace(Text.Replace(_, ",", "."), "−", "-"), "en-US"), type number}
        )
    ),
    TransDate = Table.TransformColumns(
        Numbers,
        {"transDate", each if _ = null or _ = "" then null else DateTime.FromText(_, [Format = "dd.MM.yyyy HH:mm:ss", Culture = "en-US"]), type datetime}
    ),
    Typed = Table.TransformColumnTypes(TransDate, {
        {"itemName", type text},
        {"projectName", type text},
        {"projectNumber", type text},
        {"taskName", type text},
        {"transType", type text},
        {"userName", type text},
        {"machineName", type text},
        {"warehouseName", type text},
        {"description", type text}
    }),
    Mapped1 = Table.AddColumn(Typed, "transTypeName", each
        if [transType] = null then null
        else Record.FieldOrDefault([Consumption = "Forbruk", Sale = "Salg", Purchase = "Innkjøp", Adjustment = "Justering"], [transType], [transType]),
        type text),
    WithDateKey = Table.AddColumn(Mapped1, "dateKey", each Date.From([transDate]), type date),
    // Keep [RangeStart, RangeEnd) only: incremental-refresh partitions must not overlap.
    InPeriod = Table.SelectRows(WithDateKey, each [dateKey] <> null and [dateKey] >= Date.From(RangeStart) and [dateKey] < Date.From(RangeEnd)),
    Result = InPeriod
in
    Result
