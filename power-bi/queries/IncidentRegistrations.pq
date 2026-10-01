// Alerts / incidents, the old Notifications table (v1/incident-registrations).
let
    Records = DitioExtract("v1/incident-registrations", DitioDateWindow()),
    Columns = {"id", "serialNumberWithPrefix", "title", "description", "typeName", "baseTypeText", "statusText", "riskText", "damageTypeText", "measures", "requiresFurtherAction", "resolvedOnLocation", "absenceDays", "companyName", "projectId", "projectNumber", "projectName", "projectCompanyName", "taskId", "taskName", "machineId", "machineName", "latitude", "longitude", "createdAt", "statusUpdatedAt", "modifiedDateTime", "pdfUrl", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "serialNumberWithPrefix", "title", "description", "typeName", "baseTypeText", "statusText", "riskText", "damageTypeText", "measures", "requiresFurtherAction", "resolvedOnLocation", "absenceDays", "companyName", "projectId", "projectNumber", "projectName", "projectCompanyName", "taskId", "taskName", "machineId", "machineName", "latitude", "longitude", "createdAt", "statusUpdatedAt", "modifiedDateTime", "pdfUrl", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"createdAt", DitioDateTimeFromText, type datetime},
        {"statusUpdatedAt", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"serialNumberWithPrefix", type text},
        {"title", type text},
        {"description", type text},
        {"typeName", type text},
        {"baseTypeText", type text},
        {"statusText", type text},
        {"riskText", type text},
        {"damageTypeText", type text},
        {"measures", type text},
        {"requiresFurtherAction", type logical},
        {"resolvedOnLocation", type logical},
        {"absenceDays", type number},
        {"companyName", type text},
        {"projectId", type text},
        {"projectNumber", type text},
        {"projectName", type text},
        {"projectCompanyName", type text},
        {"taskId", type text},
        {"taskName", type text},
        {"machineId", type text},
        {"machineName", type text},
        {"latitude", type number},
        {"longitude", type number},
        {"pdfUrl", type text},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    Result = WithoutDeleted
in
    Result
