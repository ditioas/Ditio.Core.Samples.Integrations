// Tabellene i rapporten og Ditio-endepunktet hver av dem lastes fra.
let
    Source = #table(
        type table [Tabell = text, Endepunkt = text, Innhold = text],
        {
        {"Prosjekter", "v1/project", "Prosjekter API-klienten har tilgang til (v1/project)."},
        {"Arbeidsordrer", "v1/project/work-breakdown-structure", "Arbeidsordrer (v1/project/work-breakdown-structure)."},
        {"Ressurser", "v1/resource", "Ressurser: personer, maskiner, kjøretøy m.m. (v1/resource)."},
        {"Brukere", "v1/user", "Brukere, én rad per person (v1/user). En person med profiler i flere firma (prosjektfirma, datterselskap) vises med profilen i arbeidsgiverfirmaet. Personopplysninger som fødselsdato og adresse er utelatt."},
        {"Timeføringer", "v1/time-registrations", "Prosjekttransaksjoner: timer for personer, maskiner og kjøretøy (v1/time-registrations)."},
        {"Maskinregistreringer", "v1/machine-registrations", "Maskinregistreringer (v1/machine-registrations)."},
        {"Fravær", "v1/absence-registrations", "Fravær, én rad per fraværsdag (v1/absence-registrations). Fritekst og sykemeldingsdetaljer er utelatt."},
        {"Varsler", "v1/incident-registrations", "Varsler: HMS, kvalitet, miljø, maskin m.m. (v1/incident-registrations). Risiko og skadetype er utelatt fordi de sjelden fylles ut."},
        {"Sjekklister", "v1/checklist-registrations", "Sjekklister og skjema (v1/checklist-registrations)."},
        {"Massetransport", "v1/flow-trip-registrations", "Massetransport: turer fra Ditio Flow og eldre massetransport (v1/flow-trip-registrations)."},
        {"Varetransaksjoner", "v1/item-registrations/web-query", "Varetransaksjoner: forbruk, salg, innkjøp og justering (v1/item-registrations/web-query)."}
        }
    )
in
    Source
