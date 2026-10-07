// Kaller et Data Extraction-endepunkt (v1/*) og følger continuationToken til alle sider er lastet. Returnerer listen med poster.
(path as text, optional filters as nullable record) as list =>
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
    Records
