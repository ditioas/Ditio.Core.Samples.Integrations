// Varsler: HMS, kvalitet, miljø, maskin m.m. (v1/incident-registrations). Risiko og skadetype er utelatt fordi de sjelden fylles ut.
let
    Records = DitioExtract("v1/incident-registrations", DitioModifiedWindow()),
    Columns = {"id", "serialNumberWithPrefix", "title", "description", "typeName", "baseTypeText", "statusText", "measures", "requiresFurtherAction", "resolvedOnLocation", "companyName", "projectId", "projectNumber", "projectName", "projectCompanyName", "taskId", "taskName", "machineId", "machineName", "latitude", "longitude", "createdAt", "statusUpdatedAt", "modifiedDateTime", "pdfUrl", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "serialNumberWithPrefix", "title", "description", "typeName", "baseTypeText", "statusText", "measures", "requiresFurtherAction", "resolvedOnLocation", "companyName", "projectId", "projectNumber", "projectName", "projectCompanyName", "taskId", "taskName", "machineId", "machineName", "latitude", "longitude", "createdAt", "statusUpdatedAt", "modifiedDateTime", "pdfUrl", "isDeleted"}),
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
        {"measures", type text},
        {"requiresFurtherAction", type logical},
        {"resolvedOnLocation", type logical},
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
    Mapped1 = Table.AddColumn(WithoutDeleted, "baseTypeName", each
        if [baseTypeText] = null then null
        else Record.FieldOrDefault([HSE = "HMS", QA = "Kvalitet", Environmental = "Miljø", Machine = "Maskin", Other = "Annet"], [baseTypeText], [baseTypeText]),
        type text),
    Mapped2 = Table.AddColumn(Mapped1, "statusName", each
        if [statusText] = null then null
        else Record.FieldOrDefault([Open = "Åpen", InProgress = "Pågår", Closed = "Lukket"], [statusText], [statusText]),
        type text),
    WithDateKey = Table.AddColumn(Mapped2, "dateKey", each Date.From([createdAt]), type date),
    // Keep the loaded period only, so every row has a day in the Dato table.
    InPeriod = Table.SelectRows(WithDateKey, each [dateKey] <> null and [dateKey] >= FromDate and [dateKey] <= ToDate),
    Result = InPeriod
in
    Result
