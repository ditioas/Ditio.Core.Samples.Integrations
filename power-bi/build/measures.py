"""DAX measures of the Ditio Power BI template, hosted in the Målinger table.

Each measure: (name, display folder, DAX, format string, description). Measures refer to
tables and columns by their Norwegian labels; the build checks every reference exists.
"""

HOURS = "#,0.0"
COUNT = "#,0"
PERCENT = "0.0 %"
AMOUNT = "#,0"
DATETIME = "dd.MM.yyyy HH:mm"
DATE = "dd.MM.yyyy"


def _per_table(tables, expression_for):
    """SWITCH over Datakilder[Tabell], one branch per table that has a value."""
    branches = [f'"{t["name"]}", {expression_for(t)}' for t in tables if expression_for(t)]
    return "SWITCH(SELECTEDVALUE('Datakilder'[Tabell]),\n    " + ",\n    ".join(branches) + "\n)"


def build_measures(tables, label_of):
    """tables: every data table (spec dicts); label_of(table, api): the column's label."""

    def row_count(table):
        # + 0 so an empty table shows 0 instead of disappearing from the list.
        return f"COUNTROWS('{table['name']}') + 0"

    def last_modified(table):
        if any(api == "modifiedDateTime" for api, _, _ in table["columns"]):
            return f"MAX('{table['name']}'[{label_of(table, 'modifiedDateTime')}])"
        return None

    return [
        # --- Timer
        ("Timer totalt", "Timer", "SUM('Timeføringer'[Timer netto])", HOURS,
         "Alle registrerte timer: personer, maskiner og kjøretøy."),
        ("Persontimer", "Timer", "CALCULATE([Timer totalt], 'Ressurser'[Ressursgruppe] = \"Person\")", HOURS,
         "Timer registrert på personer."),
        ("Maskintimer", "Timer", "CALCULATE([Timer totalt], 'Ressurser'[Ressursgruppe] = \"Maskin\")", HOURS,
         "Timer registrert på maskiner."),
        ("Kjøretøytimer", "Timer", "CALCULATE([Timer totalt], 'Ressurser'[Ressursgruppe] = \"Kjøretøy\")", HOURS,
         "Timer registrert på kjøretøy."),
        ("Godkjente timer", "Timer", "CALCULATE([Timer totalt], 'Timeføringer'[Godkjent] = TRUE())", HOURS,
         "Timer som er godkjent."),
        ("Andel godkjent", "Timer", "DIVIDE([Godkjente timer], [Timer totalt])", PERCENT,
         "Godkjente timer delt på alle timer."),
        ("Ikke godkjente timer", "Timer", "CALCULATE([Timer totalt], 'Timeføringer'[Godkjent] = FALSE())", HOURS,
         "Timer som venter på godkjenning."),
        ("Ikke godkjent over 14 dager", "Timer",
         "CALCULATE([Ikke godkjente timer], 'Timeføringer'[Arbeidsdato] < TODAY() - 14)", HOURS,
         "Timer som ikke er godkjent, med arbeidsdato mer enn 14 dager tilbake."),
        ("Lønnsgodkjente timer", "Timer", "CALCULATE([Timer totalt], 'Timeføringer'[Lønnsgodkjent] = TRUE())", HOURS,
         "Timer som er godkjent for lønn."),
        ("Andel lønnsgodkjent", "Timer", "DIVIDE([Lønnsgodkjente timer], [Timer totalt])", PERCENT,
         "Lønnsgodkjente timer delt på alle timer."),
        ("Antall personer", "Timer",
         "CALCULATE(DISTINCTCOUNT('Timeføringer'[Bruker-id]), 'Ressurser'[Ressursgruppe] = \"Person\")", COUNT,
         "Personer med timer i perioden."),
        # --- Maskiner
        ("Timer maskinregistreringer", "Maskiner", "SUM('Maskinregistreringer'[Timer])", HOURS,
         "Timer fra maskinregistreringer (eget skjema, ikke timeføringer)."),
        ("Aktive maskiner", "Maskiner",
         "CALCULATE(DISTINCTCOUNT('Timeføringer'[Ressurs-id]), 'Ressurser'[Ressursgruppe] IN {\"Maskin\", \"Kjøretøy\"})",
         COUNT, "Maskiner og kjøretøy med timer i perioden."),
        ("Timer per aktiv maskin", "Maskiner", "DIVIDE([Maskintimer] + [Kjøretøytimer], [Aktive maskiner])", HOURS,
         "Maskin- og kjøretøytimer delt på antall aktive maskiner."),
        # --- Fravær
        ("Fraværstimer", "Fravær", "SUM('Fravær'[Timer])", HOURS, "Fravær i timer."),
        ("Fraværsdager", "Fravær", "COUNTROWS('Fravær')", COUNT, "Antall fraværsdager (én rad per person per dag)."),
        ("Godkjente fraværstimer", "Fravær", "CALCULATE([Fraværstimer], 'Fravær'[Godkjent] = TRUE())", HOURS,
         "Fravær i timer som er godkjent."),
        # --- HMS og kvalitet
        ("Antall varsler", "HMS og kvalitet", "COUNTROWS('Varsler')", COUNT, "Alle varsler."),
        ("Åpne varsler", "HMS og kvalitet",
         "CALCULATE([Antall varsler], 'Varsler'[Status] IN {\"Åpen\", \"Pågår\"})", COUNT,
         "Varsler som ikke er lukket."),
        ("HMS-varsler", "HMS og kvalitet", "CALCULATE([Antall varsler], 'Varsler'[Hovedtype] = \"HMS\")", COUNT,
         "Varsler med hovedtype HMS."),
        ("Varsler per 100 000 persontimer", "HMS og kvalitet",
         "DIVIDE([Antall varsler], [Persontimer]) * 100000", "#,0.0",
         "Rapporteringsrate: varsler per 100 000 registrerte persontimer."),
        ("HMS-varsler per 100 000 persontimer", "HMS og kvalitet",
         "DIVIDE([HMS-varsler], [Persontimer]) * 100000", "#,0.0",
         "HMS-varsler per 100 000 registrerte persontimer. Ikke det samme som H1/H2, som krever fraværsskader."),
        ("Antall sjekklister", "HMS og kvalitet", "COUNTROWS('Sjekklister')", COUNT, "Alle sjekklister."),
        ("Sjekklister med avvik", "HMS og kvalitet",
         "CALCULATE([Antall sjekklister], 'Sjekklister'[Har avvik] = TRUE())", COUNT, "Sjekklister med minst ett avvik."),
        ("Avviksandel sjekklister", "HMS og kvalitet", "DIVIDE([Sjekklister med avvik], [Antall sjekklister])", PERCENT,
         "Sjekklister med avvik delt på alle sjekklister."),
        # --- Massetransport
        ("Antall turer", "Massetransport", "COUNTROWS('Massetransport')", COUNT, "Antall turer."),
        ("Mengde tonn", "Massetransport",
         "CALCULATE(SUM('Massetransport'[Mengde]), 'Massetransport'[Enhet] = \"tonn\")", "#,0.0",
         "Mengde på turer registrert i tonn."),
        ("Mengde m³", "Massetransport",
         "CALCULATE(SUM('Massetransport'[Mengde]), 'Massetransport'[Enhet] = \"m³\")", "#,0.0",
         "Mengde på turer registrert i m³."),
        ("Mengde i valgt enhet", "Massetransport",
         "IF(HASONEVALUE('Massetransport'[Enhet]), SUM('Massetransport'[Mengde]))", "#,0.0",
         "Mengde når bare én enhet er valgt. Tom når tonn og m³ er blandet, så de aldri summeres sammen."),
        ("Turer per dag", "Massetransport",
         "DIVIDE([Antall turer], DISTINCTCOUNTNOBLANK('Massetransport'[Dato]))", "#,0.0",
         "Antall turer delt på antall dager med turer."),
        ("Snitt syklustid (min)", "Massetransport",
         "DIVIDE(AVERAGE('Massetransport'[Syklustid (sek)]), 60)", "#,0.0",
         "Gjennomsnittlig syklustid per tur i minutter."),
        # --- Varer
        ("Antall varetransaksjoner", "Varer", "COUNTROWS('Varetransaksjoner')", COUNT, "Alle varetransaksjoner."),
        ("Varebeløp", "Varer", "SUM('Varetransaksjoner'[Beløp])", AMOUNT, "Beløp på varetransaksjoner."),
        ("Varekost", "Varer", "SUM('Varetransaksjoner'[Kost])", AMOUNT, "Kost på varetransaksjoner."),
        # --- Økonomi
        ("Kostbeløp", "Økonomi", "SUM('Timeføringer'[Kost])", AMOUNT, "Kost på timeføringer (mann og maskin)."),
        ("Salgsbeløp", "Økonomi", "SUM('Timeføringer'[Beløp])", AMOUNT, "Salgsbeløp på timeføringer."),
        ("Dekningsbidrag", "Økonomi", "[Salgsbeløp] - [Kostbeløp]", AMOUNT, "Salgsbeløp minus kostbeløp."),
        ("Dekningsgrad", "Økonomi", "DIVIDE([Dekningsbidrag], [Salgsbeløp])", PERCENT,
         "Dekningsbidrag delt på salgsbeløp."),
        ("Andel timer med kost", "Økonomi",
         "DIVIDE(CALCULATE([Timer totalt], 'Timeføringer'[Kost] > 0), [Timer totalt])", PERCENT,
         "Hvor stor del av timene som har kostpris. Lav andel betyr at kost- og marginstall er ufullstendige."),
        ("Andel timer med salgspris", "Økonomi",
         "DIVIDE(CALCULATE([Timer totalt], 'Timeføringer'[Beløp] > 0), [Timer totalt])", PERCENT,
         "Hvor stor del av timene som har salgspris."),
        # --- Datagrunnlag
        ("Rader", "Datagrunnlag", _per_table(tables, row_count), COUNT,
         "Antall rader lastet i tabellen. Brukes i tabellen over datakilder."),
        ("Sist endret i data", "Datagrunnlag", _per_table(tables, last_modified), DATETIME,
         "Nyeste endring i tabellens data. Viser hvor ferske dataene er."),
        ("Data fra", "Datagrunnlag", "MIN('Dato'[Dato])", DATE, "Første dag i perioden som er lastet."),
        ("Data til", "Datagrunnlag", "MAX('Dato'[Dato])", DATE, "Siste dag i perioden som er lastet."),
        ("Sist oppdatert (UTC)", "Datagrunnlag", "MAX('Datagrunnlag'[Oppdatert (UTC)])", DATETIME,
         "Når dataene sist ble hentet fra Ditio."),
    ]
