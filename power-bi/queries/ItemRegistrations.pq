// Item (goods) transactions (v1/item-registrations/web-query).
let
    Token = DitioGetAccessToken(),
    // Bound outside the Query record: inside it, FromDate/ToDate would refer to the record's own fields.
    FromDateText = Date.ToText(FromDate, "yyyy-MM-dd"),
    ToDateText = Date.ToText(ToDate, "yyyy-MM-dd"),
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
            error Error.Record("Ditio API request failed", "HTTP " & Text.From(Status) & " from v1/item-registrations/web-query", Text.FromBinary(Body))
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
    Result = Table.TransformColumnTypes(TransDate, {
        {"itemName", type text},
        {"projectName", type text},
        {"projectNumber", type text},
        {"taskName", type text},
        {"transType", type text},
        {"userName", type text},
        {"machineName", type text},
        {"warehouseName", type text},
        {"description", type text}
    })
in
    Result
