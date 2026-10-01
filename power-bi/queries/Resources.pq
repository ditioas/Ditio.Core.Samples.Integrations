// Machines and other resources (v1/resource).
let
    Records = DitioExtract("v1/resource", null),
    Columns = {"id", "number", "name", "typeName", "typeBaseName", "registrationNumber", "serialNumber", "department", "emissionClass", "buildYear", "weight", "capacity", "costPrice", "price", "companyId", "companyName", "active", "isExternal", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "number", "name", "typeName", "typeBaseName", "registrationNumber", "serialNumber", "department", "emissionClass", "buildYear", "weight", "capacity", "costPrice", "price", "companyId", "companyName", "active", "isExternal", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"number", type text},
        {"name", type text},
        {"typeName", type text},
        {"typeBaseName", type text},
        {"registrationNumber", type text},
        {"serialNumber", type text},
        {"department", type text},
        {"emissionClass", type text},
        {"buildYear", Int64.Type},
        {"weight", type number},
        {"capacity", type number},
        {"costPrice", type number},
        {"price", type number},
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
