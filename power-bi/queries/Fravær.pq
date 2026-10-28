// Fravær, én rad per fraværsdag (v1/absence-registrations). Fritekst og sykemeldingsdetaljer er utelatt.
let
    Records = DitioExtract("v1/absence-registrations", DitioDateWindow()),
    Columns = {"id", "registrationId", "date", "startTime", "qty", "absenceTypeId", "absenceTypeCode", "absenceTypeName", "employeeNumber", "userId", "projectId", "approved", "approvedByName", "approvedDateTime", "payrollApproved", "payrollApprovedDateTime", "locked", "lockedDateTime", "pdfUrl", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "registrationId", "date", "startTime", "qty", "absenceTypeId", "absenceTypeCode", "absenceTypeName", "employeeNumber", "userId", "projectId", "approved", "approvedByName", "approvedDateTime", "payrollApproved", "payrollApprovedDateTime", "locked", "lockedDateTime", "pdfUrl", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"date", DitioDateTimeFromText, type datetime},
        {"startTime", DitioDateTimeFromText, type datetime},
        {"approvedDateTime", DitioDateTimeFromText, type datetime},
        {"payrollApprovedDateTime", DitioDateTimeFromText, type datetime},
        {"lockedDateTime", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"registrationId", type text},
        {"qty", type number},
        {"absenceTypeId", type text},
        {"absenceTypeCode", type text},
        {"absenceTypeName", type text},
        {"employeeNumber", type text},
        {"userId", type text},
        {"projectId", type text},
        {"approved", type logical},
        {"approvedByName", type text},
        {"payrollApproved", type logical},
        {"locked", type logical},
        {"pdfUrl", type text},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    Derived1 = Table.AddColumn(WithoutDeleted, "absenceGroup", each
        let
            Name = if [absenceTypeName] = null then "" else Text.Lower([absenceTypeName])
        in
            if Text.Contains(Name, "barn") and (Text.Contains(Name, "syk") or Text.Contains(Name, "egenmeld")) then "Sykt barn"
            else if Text.Contains(Name, "pleie") or Text.Contains(Name, "pårørende") then "Annet fravær"
            else if Text.Contains(Name, "syk") or Text.Contains(Name, "egenmeld") then "Sykefravær"
            else if Text.Contains(Name, "ferie") then "Ferie"
            else if Text.Contains(Name, "avspas") then "Avspasering"
            else "Annet fravær",
        type text),
    WithDateKey = Table.AddColumn(Derived1, "dateKey", each Date.From([date]), type date),
    // Keep [RangeStart, RangeEnd) only: incremental-refresh partitions must not overlap.
    InPeriod = Table.SelectRows(WithDateKey, each [dateKey] <> null and [dateKey] >= Date.From(RangeStart) and [dateKey] < Date.From(RangeEnd)),
    Result = InPeriod
in
    Result
