// Én rad per dag i perioden som er lastet. Uker etter ISO 8601 (mandag først).
let
    // One row per day in the loaded window. Weeks follow ISO 8601 (Monday start), as used in Norway.
    MonthNames = {"januar", "februar", "mars", "april", "mai", "juni", "juli", "august", "september", "oktober", "november", "desember"},
    DayNames = {"mandag", "tirsdag", "onsdag", "torsdag", "fredag", "lørdag", "søndag"},
    Days = List.Dates(FromDate, Duration.Days(ToDate - FromDate) + 1, #duration(1, 0, 0, 0)),
    Rows = List.Transform(Days, (d) =>
        let
            Thursday = Date.AddDays(d, 3 - Date.DayOfWeek(d, Day.Monday))
        in
            [
                Dato = d,
                #"År" = Date.Year(d),
                Kvartal = "K" & Text.From(Date.QuarterOfYear(d)),
                #"Månedsnr" = Date.Month(d),
                #"Måned" = MonthNames{Date.Month(d) - 1},
                #"År-måned" = Date.ToText(d, "yyyy-MM"),
                #"ISO-år" = Date.Year(Thursday),
                Uke = Number.IntegerDivide(Date.DayOfYear(Thursday) - 1, 7) + 1,
                #"År-uke" = Text.From(Date.Year(Thursday)) & "-U" & Text.PadStart(Text.From(Number.IntegerDivide(Date.DayOfYear(Thursday) - 1, 7) + 1), 2, "0"),
                Ukedagnr = Date.DayOfWeek(d, Day.Monday) + 1,
                Ukedag = DayNames{Date.DayOfWeek(d, Day.Monday)},
                Helg = Date.DayOfWeek(d, Day.Monday) >= 5
            ]),
    AsTable = Table.FromRecords(Rows),
    Typed = Table.TransformColumnTypes(AsTable, {
        {"Dato", type date},
        {"År", Int64.Type},
        {"Kvartal", type text},
        {"Månedsnr", Int64.Type},
        {"Måned", type text},
        {"År-måned", type text},
        {"ISO-år", Int64.Type},
        {"Uke", Int64.Type},
        {"År-uke", type text},
        {"Ukedagnr", Int64.Type},
        {"Ukedag", type text},
        {"Helg", type logical}
    })
in
    Typed
