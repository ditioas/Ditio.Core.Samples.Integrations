// Filteret ModifiedSince fra RangeStart, for endepunkter som ikke filtrerer på FromDateTime/ToDateTime. Alt som er opprettet i perioden, er endret etter RangeStart.
() as record =>
[
    ModifiedSince = Date.ToText(Date.From(RangeStart), "yyyy-MM-dd") & "T00:00:00"
]
