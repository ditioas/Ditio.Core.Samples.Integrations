// Parses an API date/time as the wall-clock value the API returns, without time-zone conversion. Empty and 0001-01-01 values become null.
(value as nullable text) as nullable datetime =>
    if value = null or value = "" or Text.StartsWith(value, "0001-01-01") then
        null
    else
        DateTime.FromText(Text.Start(value, 19), "en-US")
