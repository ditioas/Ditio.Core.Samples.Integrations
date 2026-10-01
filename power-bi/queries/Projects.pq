// Projects visible to the API client (v1/project).
let
    Records = DitioExtract("v1/project", null),
    Columns = {"id", "number", "name", "externalNumber", "externalDim01", "externalDim02", "companyId", "companyName", "active", "isExternal", "createdDateTime", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "number", "name", "externalNumber", "externalDim01", "externalDim02", "companyId", "companyName", "active", "isExternal", "createdDateTime", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"createdDateTime", DitioDateTimeFromText, type datetime},
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"number", type text},
        {"name", type text},
        {"externalNumber", type text},
        {"externalDim01", type text},
        {"externalDim02", type text},
        {"companyId", type text},
        {"companyName", type text},
        {"active", type logical},
        {"isExternal", type logical},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    // Keys must be unique for the model relationships.
    Result = Table.Distinct(WithoutDeleted, {"id"})
in
    Result
