// Filteret ModifiedSince fra FromDate, for endepunkter som ikke filtrerer på FromDateTime/ToDateTime. Alt som er opprettet i perioden, er endret etter FromDate.
() as record =>
[
    ModifiedSince = Date.ToText(FromDate, "yyyy-MM-dd") & "T00:00:00"
]
