// Leser dato/tid slik API-et sender den, uten tidssonekonvertering. Tomme verdier og 0001-01-01 blir tomme.
(value as nullable text) as nullable datetime =>
    if value = null or value = "" or Text.StartsWith(value, "0001-01-01") then
        null
    else
        DateTime.FromText(Text.Start(value, 19), "en-US")
