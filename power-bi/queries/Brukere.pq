// Brukere, én rad per person (v1/user). En person med profiler i flere firma (prosjektfirma, datterselskap) vises med profilen i arbeidsgiverfirmaet. Personopplysninger som fødselsdato og adresse er utelatt.
let
    Records = DitioExtract("v1/user", null),
    Columns = {"id", "identityUserId", "employeeNumber", "name", "firstName", "lastName", "email", "workTitle", "department", "companyId", "companyName", "employmentCompanyId", "employmentCompanyName", "isActiveEmployment", "isDisabled", "startDate", "endDate", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "identityUserId", "employeeNumber", "name", "firstName", "lastName", "email", "workTitle", "department", "companyId", "companyName", "employmentCompanyId", "employmentCompanyName", "isActiveEmployment", "isDisabled", "startDate", "endDate", "modifiedDateTime", "isDeleted"}),
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
        {"employmentCompanyId", type text},
        {"employmentCompanyName", type text},
        {"isActiveEmployment", type logical},
        {"isDisabled", type logical},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    WithKey = Table.SelectRows(WithoutDeleted, each [identityUserId] <> null and [identityUserId] <> ""),
    Ranked = Table.AddColumn(WithKey, "isEmployerProfile", each [companyId] <> null and [companyId] = [employmentCompanyId], type logical),
    Grouped = Table.Group(Ranked, {"identityUserId"}, {
        {"Profile", each Table.First(Table.Sort(_, {
            {"isEmployerProfile", Order.Descending}, {"isActiveEmployment", Order.Descending},
            {"isDisabled", Order.Ascending}, {"modifiedDateTime", Order.Descending}})), type record},
        {"profileCount", each Table.RowCount(_), Int64.Type}
    }),
    Expanded = Table.ExpandRecordColumn(Grouped, "Profile", {"id", "employeeNumber", "name", "firstName", "lastName", "email", "workTitle", "department", "companyId", "companyName", "employmentCompanyId", "employmentCompanyName", "isActiveEmployment", "isDisabled", "startDate", "endDate", "modifiedDateTime", "isDeleted"}),
    // Grouping drops column types; restore them.
    Retyped = Table.TransformColumnTypes(Expanded, {
        {"id", type text},
        {"employeeNumber", type text},
        {"name", type text},
        {"firstName", type text},
        {"lastName", type text},
        {"email", type text},
        {"workTitle", type text},
        {"department", type text},
        {"companyId", type text},
        {"companyName", type text},
        {"employmentCompanyId", type text},
        {"employmentCompanyName", type text},
        {"isActiveEmployment", type logical},
        {"isDisabled", type logical},
        {"startDate", type datetime},
        {"endDate", type datetime},
        {"modifiedDateTime", type datetime},
        {"isDeleted", type logical}
    }),
    // Keys must be unique for the model relationships.
    Result = Table.ReorderColumns(Retyped, {"id", "identityUserId", "employeeNumber", "name", "firstName", "lastName", "email", "workTitle", "department", "companyId", "companyName", "employmentCompanyId", "employmentCompanyName", "isActiveEmployment", "isDisabled", "startDate", "endDate", "modifiedDateTime", "isDeleted", "profileCount"})
in
    Result
