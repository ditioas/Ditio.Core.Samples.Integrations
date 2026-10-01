// Filteret ModifiedSince fra RangeStart, for endepunkter som ikke filtrerer på FromDateTime/ToDateTime. Alt som er opprettet i perioden, er endret etter RangeStart.
() as record =>
[
    ModifiedSince = DateTime.ToText(RangeStart, [Format = "yyyy-MM-dd'T'HH:mm:ss", Culture = "en-US"])
]
