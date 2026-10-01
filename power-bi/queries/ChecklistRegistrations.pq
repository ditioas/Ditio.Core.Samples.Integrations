// Checklists / forms (v1/checklist-registrations).
let
    Records = DitioExtract("v1/checklist-registrations", DitioDateWindow()),
    Columns = {"id", "documentNumber", "serialNumber", "templateName", "templateVersion", "baseTypeName", "statusText", "hasDeviation", "hasComment", "hasImages", "projectId", "projectNumber", "projectName", "activityNumber", "activityName", "machineId", "resourceNumber", "createdByUserName", "createdDateTime", "submittedByUserName", "submittedDateTime", "approvedByUserName", "latitude", "longitude", "modifiedDateTime", "pdfUrl", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "documentNumber", "serialNumber", "templateName", "templateVersion", "baseTypeName", "statusText", "hasDeviation", "hasComment", "hasImages", "projectId", "projectNumber", "projectName", "activityNumber", "activityName", "machineId", "resourceNumber", "createdByUserName", "createdDateTime", "submittedByUserName", "submittedDateTime", "approvedByUserName", "latitude", "longitude", "modifiedDateTime", "pdfUrl", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"createdDateTime", DitioDateTimeFromText, type datetime},
        {"submittedDateTime", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"documentNumber", type text},
        {"serialNumber", type text},
        {"templateName", type text},
        {"templateVersion", Int64.Type},
        {"baseTypeName", type text},
        {"statusText", type text},
        {"hasDeviation", type logical},
        {"hasComment", type logical},
        {"hasImages", type logical},
        {"projectId", type text},
        {"projectNumber", type text},
        {"projectName", type text},
        {"activityNumber", type text},
        {"activityName", type text},
        {"machineId", type text},
        {"resourceNumber", type text},
        {"createdByUserName", type text},
        {"submittedByUserName", type text},
        {"approvedByUserName", type text},
        {"latitude", type number},
        {"longitude", type number},
        {"pdfUrl", type text},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    Result = WithoutDeleted
in
    Result
