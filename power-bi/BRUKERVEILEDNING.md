# Ditio i Power BI – brukerveiledning

Med Power BI-malen fra Ditio henter du data fra Ditio rett inn i Power BI. Du lager dine egne rapporter uten å skrive spørringer selv. Malen er på norsk, og feltene heter det samme som i Ditios Excel-eksport.

**Last ned:** [`Ditio-PowerBI-mal.pbit`](Ditio-PowerBI-mal.pbit) (versjon 2.1.0)

## Dette får du

| Tabell | Innhold |
|--------|---------|
| **Timeføringer** | Timer for personer, maskiner og kjøretøy, med prosjekt, arbeidsordre, godkjenning, lønnsgodkjenning, kost og pris |
| **Maskinregistreringer** | Maskintimer per maskin, fører og prosjekt |
| **Fravær** | Fravær per person og dag, med fraværstype og godkjenning |
| **Varsler** | HMS-, kvalitets-, miljø- og maskinvarsler med status |
| **Sjekklister** | Sjekklister og skjema med status, mal og avvik |
| **Massetransport** | Turer med mengde, massetype, laste- og dumpeområde, syklustid og avstand |
| **Varetransaksjoner** | Vareforbruk, salg, innkjøp og justering |
| **Prosjekter**, **Arbeidsordrer**, **Ressurser**, **Brukere** | Oppslagstabeller du filtrerer og grupperer på |
| **Dato** | Kalender med uke (ISO, mandag først), måned og år |

Malen har også:

- **Ferdige målinger** i tabellen «Målinger», for eksempel *Persontimer*, *Maskintimer*, *Andel godkjent*, *Ikke godkjent over 14 dager*, *Fraværstimer*, *Varsler per 100 000 persontimer*, *Mengde tonn*, *Mengde m³* og *Dekningsbidrag*. Hele listen med forklaring står i [feltordlisten](feltordliste.md).
- **Startsiden**, som viser perioden som er lastet, når dataene sist ble hentet, og hvor mange rader hver tabell har.
- **Feltordlisten**, som viser hva hvert felt heter i rapporten, i Ditios Excel-eksport og i API-et.

Malen inneholder ingen data, passord eller nøkler. Du legger dem inn selv når du åpner den.

## Dette trenger du

- **Power BI Desktop** (gratis fra Microsoft, kun Windows).
- **En API-klient i Ditio.** En administrator oppretter den i Ditio web under **Firma → Integrasjon** («API-klienter for Ditio API»). Ta vare på *client_id* og *client_secret*. API-klienten ser de samme dataene som brukeren den er knyttet til.

Får du feilen `invalid_scope`, mangler API-klienten tilgang til rapportdata (`reportingapiv1`). Kontakt [support@ditio.no](mailto:support@ditio.no).

## Kom i gang

1. **Åpne malen** (`.pbit`) i Power BI Desktop.
2. **Fyll ut parameterne** Power BI spør om:

   | Parameter | Hva du fyller inn |
   |-----------|-------------------|
   | ReportingApiUrl | La stå: `https://core-api.ditio.app/reporting` |
   | IdentityUrl | La stå: `https://identity.ditio.app` |
   | CoreApiUrl | La stå: `https://integration.ditio.no`. Brukes bare hvis du lager egne spørringer mot Core API. |
   | ClientId / ClientSecret | Fra API-klienten din |
   | AccessToken | La stå tom |
   | FromDate / ToDate | Perioden du vil se på, for eksempel 01.01.2026–31.12.2026. Begge dagene tas med. |
   | CompanyId | La stå tom, med mindre du bare vil ha data fra ett av firmaene i konsernet |

3. **Velg pålogging.** Power BI spør hvordan den skal koble til `core-api.ditio.app` og `identity.ditio.app`. Velg **Anonym** (*Anonymous*) for begge. Malen logger inn selv med API-klienten.
4. **Velg personvernnivå.** Sett begge adressene til **Organisasjon** (*Organizational*). Ikke velg *Privat*, for da kan ikke Power BI koble dataene sammen.
5. **Vent til dataene er lastet,** og sjekk startsiden. Hver tabell skal ha rader, og «Sist endret i data» skal være nylig.

Du endrer perioden senere under **Transformer data → Rediger parametere** (*Transform data → Edit parameters*), og deretter **Oppdater** (*Refresh*).

Store perioder går fint. Malen henter dataene side for side, og jo mer dere registrerer, jo lenger tar det.

## Lage egne visualiseringer

- **Bruk målingene,** ikke summer av kolonner. Målingene håndterer ting som er lette å gjøre feil:
  - *Mengde tonn* og *Mengde m³* summerer aldri tonn og m³ sammen.
  - *Persontimer* skiller personer fra maskiner.
- **Bruk Dato-tabellen** til tid (år, måned, uke, dag). Da filtrerer én datovelger alle tabellene samtidig.
- **Bruk Prosjekter, Arbeidsordrer, Ressurser og Brukere** til å filtrere og gruppere. De gjelder for alle registreringene som hører til.
- **Kolonnenavnene finner du i feltordlisten.** Vet du hva en kolonne heter i Excel-eksporten fra Ditio, heter den det samme her. Holder du musen over et felt i Power BI, ser du hvilket API-felt det kommer fra.

## Publisere og oppdatere automatisk

Når rapporten er publisert til Power BI-tjenesten, går du til datasettets **Innstillinger → Legitimasjon for datakilde** (*Settings → Data source credentials*). Gjør dette for **begge** Ditio-adressene:

- Godkjenningsmetode: **Anonym**
- Personvernnivå: **Organisasjon**
- Huk av for **Hopp over testtilkobling** (*Skip test connection*). Testen sendes uten Ditio-pålogging og feiler alltid; selve oppdateringen virker.

Deretter setter du opp **planlagt oppdatering**. Med Power BI Pro kan du oppdatere opptil 8 ganger i døgnet. Du trenger ingen gateway.

## Godt å vite

- **Brukere** har én rad per person. Har en person profil i flere firma, for eksempel prosjektfirma eller datterselskap, vises profilen i arbeidsgiverfirmaet. Kolonnen «Antall profiler» viser hvor mange profiler personen har.
- **Varsler per 100 000 persontimer** viser hvor mye som rapporteres. Det er ikke H1/H2, fordi Ditio ikke registrerer fraværsskader.
- **Kost og pris** (*Kostbeløp*, *Salgsbeløp*, *Dekningsbidrag*) er bare så komplette som prisene som er lagt inn i Ditio. *Andel timer med kost* viser hvor stor del av timene som har kostpris. Er den lav, er økonomitallene ufullstendige.
- **Tidspunkter** vises slik Ditio sender dem. «Sist oppdatert» vises i UTC.
- **Varetransaksjoner** har ikke id-er, bare navn og prosjektnummer. En vare- eller kommentartekst med anførselstegn (") kan gjøre at den raden ikke leses riktig.
- **Fødselsdato, adresse og pårørende** er utelatt med vilje.

## Del rapporten trygt

*ClientSecret* lagres i rapportfilen. Behandle `.pbix`-filen som et passord: del den bare med dem som skal se dataene. Gi heller andre tilgang til den publiserte rapporten. Bruk gjerne en egen API-klient bare for Power BI, så kan du slå den av uten å påvirke andre integrasjoner.

## Feilsøking

| Problem | Årsak | Løsning |
|---------|-------|---------|
| «Ditio-pålogging mangler» | ClientId eller ClientSecret er tom | Fyll dem ut under Rediger parametere |
| «Ditio-pålogging feilet» med `invalid_client` | Feil ClientId/ClientSecret, eller API-klienten er laget i et annet miljø | Kontroller verdiene, og at IdentityUrl og ReportingApiUrl peker på samme miljø |
| «Ditio-pålogging feilet» med `invalid_scope` | API-klienten mangler tilgang til rapportdata | Kontakt support@ditio.no |
| «Ditio API-kall feilet» med HTTP 403 | Brukeren API-klienten er knyttet til, har ikke tilgang. Prosjekter, ressurser og brukere krever administrator. | Be en administrator sjekke at API-klienten er knyttet til en administrator i Ditio |
| *Formula.Firewall* | Personvernnivå er ikke satt eller satt til Privat | Sett begge Ditio-adressene til Organisasjon under Innstillinger for datakilde (*File → Options and settings → Data source settings*) |
| «Kan ikke teste tilkoblingen» i Power BI-tjenesten | Testen sendes uten Ditio-pålogging | Huk av for «Hopp over testtilkobling» |
| En tabell har 0 rader | Firmaet bruker ikke den delen av Ditio, eller perioden er feil | Sjekk FromDate/ToDate og CompanyId |
| HTTP 401 midt i en lang oppdatering | Påloggingen utløp mens en svært stor tabell ble lastet | Del opp perioden |

## Hjelp

- Feltordliste og målinger: [feltordliste.md](feltordliste.md)
- Utviklerdokumentasjon: [docs.ditio.app](https://docs.ditio.app/guides/powerbi/)
- Kundestøtte: [support@ditio.no](mailto:support@ditio.no)

Har du laget en rapport du er fornøyd med, eller savner du noe i malen? Si gjerne fra til support@ditio.no.
