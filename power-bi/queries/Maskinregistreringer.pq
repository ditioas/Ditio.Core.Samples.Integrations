// Maskinregistreringer (v1/machine-registrations).
let
    Records = DitioExtract("v1/machine-registrations", DitioDateWindow()),
    Columns = {"id", "startDateTime", "stopDateTime", "qty", "description", "approved", "machineId", "machineName", "resourceNumber", "machineTypeName", "driverId", "driverName", "driverCompanyName", "projectId", "projectNumber", "projectName", "projectExternalNumber", "projectCompanyName", "createdDateTime", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "startDateTime", "stopDateTime", "qty", "description", "approved", "machineId", "machineName", "resourceNumber", "machineTypeName", "driverId", "driverName", "driverCompanyName", "projectId", "projectNumber", "projectName", "projectExternalNumber", "projectCompanyName", "createdDateTime", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"startDateTime", DitioDateTimeFromText, type datetime},
        {"stopDateTime", DitioDateTimeFromText, type datetime},
        {"createdDateTime", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"qty", type number},
        {"description", type text},
        {"approved", type logical},
        {"machineId", type text},
        {"machineName", type text},
        {"resourceNumber", type text},
        {"machineTypeName", type text},
        {"driverId", type text},
        {"driverName", type text},
        {"driverCompanyName", type text},
        {"projectId", type text},
        {"projectNumber", type text},
        {"projectName", type text},
        {"projectExternalNumber", type text},
        {"projectCompanyName", type text},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    WithDateKey = Table.AddColumn(WithoutDeleted, "dateKey", each Date.From([stopDateTime]), type date),
    // Keep [RangeStart, RangeEnd) only: incremental-refresh partitions must not overlap.
    InPeriod = Table.SelectRows(WithDateKey, each [dateKey] <> null and [dateKey] >= Date.From(RangeStart) and [dateKey] < Date.From(RangeEnd)),
    Result = InPeriod
in
    Result
