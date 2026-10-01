// Prosjekttransaksjoner: timer for personer, maskiner og kjøretøy (v1/time-registrations).
let
    Records = DitioExtract("v1/time-registrations", DitioDateWindow()),
    Columns = {"id", "workDate", "startDateTime", "stopDateTime", "qty", "unitName", "unitQty", "unitPrice", "price", "amount", "costPriceBase", "costPriceMan", "costPriceMachine", "costAmount", "costAmountMan", "costAmountMachine", "breakQty", "description", "descriptionInternal", "approved", "approvedByName", "approvedDateTime", "payrollApproved", "payrollApprovedDateTime", "locked", "lockedByName", "lockedDateTime", "invoiced", "projectId", "projectNumber", "projectName", "projectExternalNumber", "projectCompanyId", "projectCompanyName", "projectLeaderName", "projectMainForemanName", "taskId", "taskName", "taskNameLong", "taskWbsNumber", "taskChapterId", "taskPieceWorkId", "resourceId", "resourceNumber", "resourceName", "resourceTypeName", "primaryResourceName", "machineEmissionClass", "userId", "userName", "userEmployeeNumber", "userCompanyName", "userCompanyOrgNr", "userWorkTitle", "userWorkShiftSettingName", "createdDateTime", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "workDate", "startDateTime", "stopDateTime", "qty", "unitName", "unitQty", "unitPrice", "price", "amount", "costPriceBase", "costPriceMan", "costPriceMachine", "costAmount", "costAmountMan", "costAmountMachine", "breakQty", "description", "descriptionInternal", "approved", "approvedByName", "approvedDateTime", "payrollApproved", "payrollApprovedDateTime", "locked", "lockedByName", "lockedDateTime", "invoiced", "projectId", "projectNumber", "projectName", "projectExternalNumber", "projectCompanyId", "projectCompanyName", "projectLeaderName", "projectMainForemanName", "taskId", "taskName", "taskNameLong", "taskWbsNumber", "taskChapterId", "taskPieceWorkId", "resourceId", "resourceNumber", "resourceName", "resourceTypeName", "primaryResourceName", "machineEmissionClass", "userId", "userName", "userEmployeeNumber", "userCompanyName", "userCompanyOrgNr", "userWorkTitle", "userWorkShiftSettingName", "createdDateTime", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"workDate", DitioDateTimeFromText, type datetime},
        {"startDateTime", DitioDateTimeFromText, type datetime},
        {"stopDateTime", DitioDateTimeFromText, type datetime},
        {"approvedDateTime", DitioDateTimeFromText, type datetime},
        {"payrollApprovedDateTime", DitioDateTimeFromText, type datetime},
        {"lockedDateTime", DitioDateTimeFromText, type datetime},
        {"createdDateTime", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"qty", type number},
        {"unitName", type text},
        {"unitQty", type number},
        {"unitPrice", type number},
        {"price", type number},
        {"amount", type number},
        {"costPriceBase", type number},
        {"costPriceMan", type number},
        {"costPriceMachine", type number},
        {"costAmount", type number},
        {"costAmountMan", type number},
        {"costAmountMachine", type number},
        {"breakQty", type number},
        {"description", type text},
        {"descriptionInternal", type text},
        {"approved", type logical},
        {"approvedByName", type text},
        {"payrollApproved", type logical},
        {"locked", type logical},
        {"lockedByName", type text},
        {"invoiced", type logical},
        {"projectId", type text},
        {"projectNumber", type text},
        {"projectName", type text},
        {"projectExternalNumber", type text},
        {"projectCompanyId", type text},
        {"projectCompanyName", type text},
        {"projectLeaderName", type text},
        {"projectMainForemanName", type text},
        {"taskId", type text},
        {"taskName", type text},
        {"taskNameLong", type text},
        {"taskWbsNumber", type text},
        {"taskChapterId", type text},
        {"taskPieceWorkId", type text},
        {"resourceId", type text},
        {"resourceNumber", type text},
        {"resourceName", type text},
        {"resourceTypeName", type text},
        {"primaryResourceName", type text},
        {"machineEmissionClass", type text},
        {"userId", type text},
        {"userName", type text},
        {"userEmployeeNumber", type text},
        {"userCompanyName", type text},
        {"userCompanyOrgNr", type text},
        {"userWorkTitle", type text},
        {"userWorkShiftSettingName", type text},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    WithDateKey = Table.AddColumn(WithoutDeleted, "dateKey", each Date.From([workDate]), type date),
    // Keep the loaded period only, so every row has a day in the Dato table.
    InPeriod = Table.SelectRows(WithDateKey, each [dateKey] <> null and [dateKey] >= FromDate and [dateKey] <= ToDate),
    Result = InPeriod
in
    Result
