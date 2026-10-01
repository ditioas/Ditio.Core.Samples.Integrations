// Filteret FromDateTime/ToDateTime fra RangeStart til RangeEnd, hele dager.
() as record =>
[
    // Whole days. The API's ToDateTime is inclusive, so a row exactly at RangeEnd is fetched by two
    // neighbouring partitions; the [RangeStart, RangeEnd) clip in each table keeps it in one.
    FromDateTime = Date.ToText(Date.From(RangeStart), "yyyy-MM-dd") & "T00:00:00",
    ToDateTime = Date.ToText(Date.From(RangeEnd), "yyyy-MM-dd") & "T00:00:00"
]
