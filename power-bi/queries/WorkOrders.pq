// Work orders / WBS (v1/project/work-breakdown-structure).
let
    Records = DitioExtract("v1/project/work-breakdown-structure", null),
    Columns = {"id", "projectId", "number", "name", "nameWithNumber", "chapterId", "externalNumber", "externalDim01", "externalDim02", "externalPieceWorkId", "parentName", "fullPathName", "companyId", "companyName", "isExternal", "modifiedDateTime", "isDeleted"},
    Raw =
        if List.IsEmpty(Records) then
            #table(Columns, {})
        else
            Table.ExpandRecordColumn(
                Table.FromList(Records, Splitter.SplitByNothing(), {"Record"}, null, ExtraValues.Error),
                "Record",
                Columns
            ),
    Ordered = Table.ReorderColumns(Raw, {"id", "projectId", "number", "name", "nameWithNumber", "chapterId", "externalNumber", "externalDim01", "externalDim02", "externalPieceWorkId", "parentName", "fullPathName", "companyId", "companyName", "isExternal", "modifiedDateTime", "isDeleted"}),
    Dates = Table.TransformColumns(Ordered, {
        {"modifiedDateTime", DitioDateTimeFromText, type datetime}
    }),
    Typed = Table.TransformColumnTypes(Dates, {
        {"id", type text},
        {"projectId", type text},
        {"number", type text},
        {"name", type text},
        {"nameWithNumber", type text},
        {"chapterId", type text},
        {"externalNumber", type text},
        {"externalDim01", type text},
        {"externalDim02", type text},
        {"externalPieceWorkId", type text},
        {"parentName", type text},
        {"fullPathName", type text},
        {"companyId", type text},
        {"companyName", type text},
        {"isExternal", type logical},
        {"isDeleted", type logical}
    }, "en-US"),
    WithoutDeleted = Table.SelectRows(Typed, each [isDeleted] <> true),
    // Keys must be unique for the model relationships.
    Result = Table.Distinct(WithoutDeleted, {"id"})
in
    Result
