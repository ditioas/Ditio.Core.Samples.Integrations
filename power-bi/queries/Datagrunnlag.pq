// Når dataene sist ble hentet.
let
    Source = #table(
        type table [#"Oppdatert (UTC)" = datetime],
        {{DateTimeZone.RemoveZone(DateTimeZone.UtcNow())}}
    )
in
    Source
