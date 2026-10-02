// Lønnsgrunnlag, én rad per ansatt per dag med timer eller fravær (v1/payroll-lines-extended). Bare timer, ingen lønnsbeløp. Overtid utover 50 % og 100 % er ikke med.
let
    Extracted = try DitioExtract("v1/payroll-lines-extended", DitioDateWindow() & [ExportFilter = "All", UserPayrollTypeFilter = "AllUsers"]),
    Extraction =
        if Extracted[HasError] then
            error Error.Record(Extracted[Error][Reason],
                Extracted[Error][Message] & " " & "Feiler bare denne tabellen, er årsaken som regel at API-klienten ikke representerer en administrator i Ditio, som lønnsgrunnlaget krever. Sett parameteren IncludePayroll til false for å laste rapporten uten lønnstall.", Extracted[Error][Detail])
        else
            Extracted[Value],
    Records = if IncludePayroll = false then {} else Extraction,
    Columns = {"id", "transDateTime", "userId", "userName", "employeeNumber", "companyId", "qty", "standardQty", "overtime50Qty", "overtime100Qty", "timebankQty", "breakQty", "absenceQty", "absenceApprovedQty", "absenceLockedQty", "totalVerifiedQty", "approvedQty", "lockedQty", "verified", "verifiedByName", "verifiedDateTime", "approved", "approvedByName", "approvedDateTime", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "transDateTime", "userId", "userName", "employeeNumber", "companyId", "qty", "standardQty", "overtime50Qty", "overtime100Qty", "timebankQty", "breakQty", "absenceQty", "absenceApprovedQty", "absenceLockedQty", "totalVerifiedQty", "approvedQty", "lockedQty", "verified", "verifiedByName", "verifiedDateTime", "approved", "approvedByName", "approvedDateTime", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"transDateTime", DitioDateTimeFromText, type datetime},
        {"verifiedDateTime", DitioDateTimeFromText, type datetime},
        {"approvedDateTime", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"userId", type text},
        {"userName", type text},
        {"employeeNumber", type text},
        {"companyId", type text},
        {"qty", type number},
        {"standardQty", type number},
        {"overtime50Qty", type number},
        {"overtime100Qty", type number},
        {"timebankQty", type number},
        {"breakQty", type number},
        {"absenceQty", type number},
        {"absenceApprovedQty", type number},
        {"absenceLockedQty", type number},
        {"totalVerifiedQty", type number},
        {"approvedQty", type number},
        {"lockedQty", type number},
        {"verified", type logical},
        {"verifiedByName", type text},
        {"approved", type logical},
        {"approvedByName", type text},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    WithDateKey = Table.AddColumn(WithoutDeleted, "dateKey", each Date.From([transDateTime]), type date),
    // Keep [RangeStart, RangeEnd) only: incremental-refresh partitions must not overlap.
    InPeriod = Table.SelectRows(WithDateKey, each [dateKey] <> null and [dateKey] >= Date.From(RangeStart) and [dateKey] < Date.From(RangeEnd)),
    Result = InPeriod
in
    Result
