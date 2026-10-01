// FromDateTime/ToDateTime query filter covering FromDate up to and including ToDate.
() as record =>
[
    FromDateTime = Date.ToText(FromDate, "yyyy-MM-dd") & "T00:00:00",
    ToDateTime = Date.ToText(ToDate, "yyyy-MM-dd") & "T23:59:59"
]
