// Perioden som er lastet og når dataene sist ble hentet.
let
    Source = #table(
        type table [#"Fra dato" = date, #"Til dato" = date, #"Oppdatert (UTC)" = datetime],
        {{FromDate, ToDate, DateTimeZone.RemoveZone(DateTimeZone.UtcNow())}}
    )
in
    Source
