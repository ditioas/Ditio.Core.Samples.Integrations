// Users, one row per company profile (v1/user). Personal fields such as birth date and address are left out on purpose.
let
    Records = DitioExtract("v1/user", null),
    Columns = {"id", "identityUserId", "employeeNumber", "name", "firstName", "lastName", "email", "workTitle", "department", "companyId", "companyName", "employmentCompanyName", "isActiveEmployment", "isDisabled", "startDate", "endDate", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "identityUserId", "employeeNumber", "name", "firstName", "lastName", "email", "workTitle", "department", "companyId", "companyName", "employmentCompanyName", "isActiveEmployment", "isDisabled", "startDate", "endDate", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"startDate", DitioDateTimeFromText, type datetime},
        {"endDate", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"identityUserId", type text},
        {"employeeNumber", type text},
        {"name", type text},
        {"firstName", type text},
        {"lastName", type text},
        {"email", type text},
        {"workTitle", type text},
        {"department", type text},
        {"companyId", type text},
        {"companyName", type text},
        {"employmentCompanyName", type text},
        {"isActiveEmployment", type logical},
        {"isDisabled", type logical},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    // Keys must be unique for the model relationships.
    Result = Table.Distinct(WithoutDeleted, {"id"})
in
    Result
