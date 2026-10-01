// Massetransport: turer fra Ditio Flow og eldre massetransport (v1/flow-trip-registrations).
let
    Records = DitioExtract("v1/flow-trip-registrations", DitioDateWindow()),
    Columns = {"id", "loadDateTime", "dumpDateTime", "costStartDateTime", "costEndDateTime", "quantity", "quantityM3", "registeredQuantity", "verifiedQuantity", "unitOfMeasureText", "massTypeId", "massType", "m3PerHour", "totalCycleTimeSeconds", "totalCycleTimeHours", "utilizationRate", "distance", "averageSpeedKmh", "netAltitudeChange", "totalAscent", "maxGradient", "averageGradient", "loadLocationName", "dumpLocationName", "loaderId", "loaderName", "loaderNumber", "loaderDriverName", "dumperId", "dumperName", "dumperNumber", "dumperRegistrationNumber", "dumperDriverName", "dumperDriverCompanyName", "trailer", "verified", "approved", "invoiced", "receiptNumber", "customerName", "comment", "projectId", "projectNumber", "projectName", "projectExternalNumber", "projectCompanyName", "taskId", "taskName", "isFlowData", "modifiedDateTime", "isDeleted", "loadCoordinates", "dumpCoordinates"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    LoadSplit = Table.AddColumn(Table.AddColumn(Raw,
        "loadLongitude", each try [loadCoordinates]{0} otherwise null),
        "loadLatitude", each try [loadCoordinates]{1} otherwise null),
    DumpSplit = Table.AddColumn(Table.AddColumn(LoadSplit,
        "dumpLongitude", each try [dumpCoordinates]{0} otherwise null),
        "dumpLatitude", each try [dumpCoordinates]{1} otherwise null),
    WithoutRawCoordinates = Table.RemoveColumns(DumpSplit, {"loadCoordinates", "dumpCoordinates"}),
    Ordered = Table.ReorderColumns(WithoutRawCoordinates, {"id", "loadDateTime", "dumpDateTime", "costStartDateTime", "costEndDateTime", "quantity", "quantityM3", "registeredQuantity", "verifiedQuantity", "unitOfMeasureText", "massTypeId", "massType", "m3PerHour", "totalCycleTimeSeconds", "totalCycleTimeHours", "utilizationRate", "distance", "averageSpeedKmh", "netAltitudeChange", "totalAscent", "maxGradient", "averageGradient", "loadLocationName", "dumpLocationName", "loadLongitude", "loadLatitude", "dumpLongitude", "dumpLatitude", "loaderId", "loaderName", "loaderNumber", "loaderDriverName", "dumperId", "dumperName", "dumperNumber", "dumperRegistrationNumber", "dumperDriverName", "dumperDriverCompanyName", "trailer", "verified", "approved", "invoiced", "receiptNumber", "customerName", "comment", "projectId", "projectNumber", "projectName", "projectExternalNumber", "projectCompanyName", "taskId", "taskName", "isFlowData", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"loadDateTime", DitioDateTimeFromText, type datetime},
        {"dumpDateTime", DitioDateTimeFromText, type datetime},
        {"costStartDateTime", DitioDateTimeFromText, type datetime},
        {"costEndDateTime", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"quantity", type number},
        {"quantityM3", type number},
        {"registeredQuantity", type number},
        {"verifiedQuantity", type number},
        {"unitOfMeasureText", type text},
        {"massTypeId", type text},
        {"massType", type text},
        {"m3PerHour", type number},
        {"totalCycleTimeSeconds", Int64.Type},
        {"totalCycleTimeHours", type number},
        {"utilizationRate", type number},
        {"distance", type number},
        {"averageSpeedKmh", type number},
        {"netAltitudeChange", type number},
        {"totalAscent", type number},
        {"maxGradient", type number},
        {"averageGradient", type number},
        {"loadLocationName", type text},
        {"dumpLocationName", type text},
        {"loadLongitude", type number},
        {"loadLatitude", type number},
        {"dumpLongitude", type number},
        {"dumpLatitude", type number},
        {"loaderId", type text},
        {"loaderName", type text},
        {"loaderNumber", type text},
        {"loaderDriverName", type text},
        {"dumperId", type text},
        {"dumperName", type text},
        {"dumperNumber", type text},
        {"dumperRegistrationNumber", type text},
        {"dumperDriverName", type text},
        {"dumperDriverCompanyName", type text},
        {"trailer", type logical},
        {"verified", type logical},
        {"approved", type logical},
        {"invoiced", type logical},
        {"receiptNumber", type text},
        {"customerName", type text},
        {"comment", type text},
        {"projectId", type text},
        {"projectNumber", type text},
        {"projectName", type text},
        {"projectExternalNumber", type text},
        {"projectCompanyName", type text},
        {"taskId", type text},
        {"taskName", type text},
        {"isFlowData", type logical},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    Mapped1 = Table.AddColumn(WithoutDeleted, "unitName", each
        if [unitOfMeasureText] = null then null
        else Record.FieldOrDefault([Ton = "tonn", CubicMeter = "m³"], [unitOfMeasureText], [unitOfMeasureText]),
        type text),
    WithDateKey = Table.AddColumn(Mapped1, "dateKey", each Date.From([loadDateTime]), type date),
    // Keep the loaded period only, so every row has a day in the Dato table.
    InPeriod = Table.SelectRows(WithDateKey, each [dateKey] <> null and [dateKey] >= FromDate and [dateKey] <= ToDate),
    Result = InPeriod
in
    Result
