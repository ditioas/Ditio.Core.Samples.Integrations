// Filteret FromDateTime/ToDateTime fra RangeStart til (men ikke med) RangeEnd.
() as record =>
[
    FromDateTime = DateTime.ToText(RangeStart, [Format = "yyyy-MM-dd'T'HH:mm:ss", Culture = "en-US"]),
    // The API's ToDateTime is inclusive; stop one second before RangeEnd so partitions don't overlap.
    ToDateTime = DateTime.ToText(RangeEnd - #duration(0, 0, 0, 1), [Format = "yyyy-MM-dd'T'HH:mm:ss", Culture = "en-US"])
]
