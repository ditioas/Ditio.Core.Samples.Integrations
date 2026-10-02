# Ditio i Power BI – brukerveiledning

Med Power BI-malen fra Ditio henter du data fra Ditio rett inn i Power BI. Du lager dine egne rapporter uten å skrive spørringer selv. Malen er på norsk, og feltene heter det samme som i Ditios Excel-eksport.

**Last ned:** [`Ditio-PowerBI-mal.pbit`](Ditio-PowerBI-mal.pbit) (versjon 2.3.0)

## Dette får du

| Tabell | Innhold |
|--------|---------|
| **Timeføringer** | Timer for personer, maskiner og kjøretøy, med prosjekt, arbeidsordre, godkjenning, lønnsgodkjenning, kost og pris |
| **Maskinregistreringer** | Maskintimer per maskin, fører og prosjekt |
| **Fravær** | Fravær per person og dag, med fraværstype, fraværsgruppe og godkjenning |
| **Lønn per dag** | Lønnsgrunnlaget per ansatt og dag: arbeidede timer, normaltid, overtid 50 % og 100 %, timebank, fravær og lønnsstatus. Bare timer, ingen lønnsbeløp. |
| **Varsler** | HMS-, kvalitets-, miljø- og maskinvarsler med status |
| **Sjekklister** | Sjekklister og skjema med status, mal og avvik |
| **Massetransport** | Turer med mengde, massetype, laste- og dumpeområde, syklustid og avstand |
| **Varetransaksjoner** | Vareforbruk, salg, innkjøp og justering |
| **Prosjekter**, **Arbeidsordrer**, **Ressurser**, **Brukere** | Oppslagstabeller du filtrerer og grupperer på |
| **Dato** | Kalender med uke (ISO, mandag først), måned og år |

Malen har også:

- **Ferdige målinger** i tabellen «Målinger», for eksempel *Persontimer*, *Maskintimer*, *Andel godkjent*, *Ikke godkjent over 14 dager*, *Fraværstimer*, *Varsler per 100 000 persontimer*, *Mengde tonn*, *Mengde m³* og *Dekningsbidrag*. Hele listen med forklaring står i [feltordlisten](feltordliste.md).
- **Startsiden**, som viser perioden som er lastet, når dataene sist ble hentet, og hvor mange rader hver tabell har.
- **Ferdige rapportsider**, alle med valg av periode og prosjekt øverst:
  - *Oversikt*: nøkkeltall (persontimer, maskintimer, andel godkjent, overtidsandel, sykefravær %, HMS-varsler per 100 000 timer), timer per uke og per prosjekt.
  - *Timer*: timer per prosjekt, arbeidsordre og ressurs per måned, godkjenningsløpet og ikke godkjente timer etter alder.
  - *Maskiner*: maskin- og kjøretøytimer per uke og maskintype, timer per maskindag, og når hver maskin sist hadde timer.
  - *Lønn og overtid*: normaltid, overtid og timebank per måned, overtidsandel, lønnsstatus og avspasering.
  - *Fravær*: fravær per måned og gruppe, sykefravær % (også siste 12 måneder) og fravær per type.
- **Feltordlisten**, som viser hva hvert felt heter i rapporten, i Ditios Excel-eksport og i API-et.

Malen inneholder ingen data, passord eller nøkler. Du legger dem inn selv når du åpner den.

## Dette trenger du

- **Power BI Desktop** (gratis fra Microsoft, kun Windows).
- **En egen API-klient for Power BI.** En administrator oppretter den i Ditio web under **Oppsett → Integrasjon**. Ta vare på *client_id* og *client_secret*.
  - Gi den bare tilgangen `reportingapiv1`. Ikke bruk en API-klient som også har `ditioapiv3`.
  - Brukeren API-klienten er knyttet til, må være **administrator** for å lese prosjekter, ressurser og brukere. Derfor ser alle som kan se den publiserte rapporten, alle dataene som er lastet, også fravær og kostpriser, uansett hvilken tilgang de selv har i Ditio. Begrens hvem som kan se rapporten, eller legg på radnivåsikkerhet (*row-level security*) i Power BI.

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
   | AccessToken | La stå tom. Fyller du den ut, brukes den i stedet for ClientId/ClientSecret og fornyes aldri, så planlagt oppdatering slutter å virke når den utløper. |
   | RangeStart / RangeEnd | Perioden som lastes i Power BI Desktop, for eksempel 01.04.2026 00:00 til 01.07.2026 00:00. RangeStart tas med, RangeEnd tas ikke med. Publiserer du rapporten, holder noen få måneder: Power BI-tjenesten henter selv de siste 24 månedene (se «Slik holdes dataene oppdatert»). Bruker du bare Power BI Desktop, setter du hele perioden du vil se. |
   | CompanyId | La stå tom, med mindre du bare vil ha data fra ett av firmaene i konsernet |
   | IncludePayroll | La stå `true`. Sett til `false` hvis du ikke vil ha lønnsgrunnlaget, eller hvis oppdateringen feiler på «Lønn per dag». Sidene «Lønn og overtid» blir da tomme, og sykefravær % regnes av persontimer i stedet. |

3. **Velg pålogging.** Power BI spør hvordan den skal koble til `core-api.ditio.app` og `identity.ditio.app`. Velg **Anonym** (*Anonymous*) for begge. Malen logger inn selv med API-klienten.
4. **Velg personvernnivå.** Sett begge adressene til **Organisasjon** (*Organizational*). Ikke velg *Privat*, for da kan ikke Power BI koble dataene sammen.
5. **Vent til dataene er lastet,** og sjekk startsiden. Hver tabell skal ha rader, og «Sist endret i data» skal være nylig.

I Power BI Desktop endrer du perioden under **Transformer data → Rediger parametere** (*Transform data → Edit parameters*), og deretter **Oppdater** (*Refresh*).

Alle tabeller unntatt varetransaksjoner hentes side for side, så lange perioder går fint. Hver tabell får en ny pålogging. Tar én tabell lenger tid å laste enn påloggingen varer, feiler oppdateringen med HTTP 401; korte da ned perioden.

Varsler og sjekklister hentes som «endret siden RangeStart» og begrenses deretter til perioden.

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

## Slik holdes dataene oppdatert

Malen bruker **inkrementell oppdatering** (*incremental refresh*) for registreringene (timer, maskinregistreringer, fravær, varsler, sjekklister, massetransport og varer):

- **I Power BI Desktop** lastes bare perioden fra RangeStart til RangeEnd.
- **I Power BI-tjenesten** deler Power BI dataene i perioder etter dato. Første oppdatering etter publisering henter de siste **24 månedene**; den tar lengst tid. Deretter henter hver oppdatering **inneværende måned og de to foregående** på nytt. Eldre måneder blir liggende som de var.
- Prosjekter, arbeidsordrer, ressurser og brukere hentes alltid i sin helhet.

Dette gjør oppdateringene raske og skåner både Power BI og Ditio.

> **Eldre registreringer fryses.** Registreringer med dato før den første i måneden to måneder tilbake hentes **ikke** på nytt. Den 15. oktober gjelder det alt med dato før 1. august. En sen godkjenning, lønnslåsing, korrigering eller sletting på en slik registrering kommer ikke med før dataene hentes helt på nytt: publiser rapporten på nytt fra Power BI Desktop.

- **Ta vare på .pbix-filen.** En modell med inkrementell oppdatering kan ikke lastes ned igjen fra Power BI-tjenesten. Til første oppdatering etter en ny publisering er ferdig, viser rapporten bare perioden fra Power BI Desktop.
- **Tar første oppdatering for lang tid?** Power BI Pro stopper en oppdatering etter 2 timer. Går den første 24-månedersinnlastingen ut på tid, setter du ned antall måneder som lagres før du publiserer.

Vil du beholde flere måneder eller hente flere dager på nytt, endrer du det i Power BI Desktop: høyreklikk tabellen → **Inkrementell oppdatering** (*Incremental refresh*), før du publiserer.

## Godt å vite

- **Brukere** har én rad per person. Har en person profil i flere firma, for eksempel prosjektfirma eller datterselskap, vises profilen i arbeidsgiverfirmaet. Kolonnen «Antall profiler» viser hvor mange profiler personen har.
- **Varsler per 100 000 persontimer** viser hvor mye som rapporteres. Det er ikke H1/H2, fordi Ditio ikke registrerer fraværsskader.
- **Kost og pris** (*Kostbeløp*, *Salgsbeløp*, *Dekningsbidrag*) er bare så komplette som prisene som er lagt inn i Ditio. *Andel timer med kost* viser hvor stor del av timene som har kostpris. Er den lav, er økonomitallene ufullstendige.
- **Sykefravær %** er omtrentlig: sykefraværstimer delt på normaltid pluss alt fravær fra lønnsgrunnlaget. Ditio sender ikke hvilken kategori en fraværstype har, så kolonnen «Fraværsgruppe» leses fra navnet: navn med «syk» eller «egenmeld» blir Sykefravær (med «barn» blir det Sykt barn), «ferie» blir Ferie, «avspas» blir Avspasering, alt annet blir Annet fravær. Heter fraværstypene noe annet hos dere, endrer du regelen i spørringen «Fravær».
- **Overtid** er overtid 50 % og 100 %. Andre overtidstyper er ikke med i lønnsgrunnlagets sammendrag. *Timer til timebank* er innskudd; uttak vises som fravær av typen avspasering.
- **Lønn per dag** har bare dager med timer eller fravær, ikke helger uten registreringer. Prosjektvalget påvirker ikke lønnstallene, fordi lønnsgrunnlaget gjelder hele dagen.
- **Tidspunkter** vises slik Ditio sender dem. «Sist oppdatert» vises i UTC.
- **Varetransaksjoner** har ikke id-er, bare navn og prosjektnummer. En vare- eller kommentartekst med anførselstegn (") kan gjøre at den raden ikke leses riktig.
- **Fødselsdato, adresse og pårørende** er utelatt med vilje.

## Del rapporten trygt

*ClientSecret* (og *AccessToken*, hvis den er fylt ut) lagres **i klartekst** i rapportfilen og i den publiserte semantiske modellen. Alle som kan åpne, laste ned eller redigere rapporten, kan lese den og hente data fra Ditio som administrator, også utenfor rapporten.

- Bruk en egen API-klient for Power BI med bare `reportingapiv1`.
- Del filen og redigeringstilgang til arbeidsområdet bare med dem som kan se alle firmaets data. Gi heller andre tilgang til den publiserte rapporten.
- Lekker filen, eller slutter noen som hadde tilgang, bytter du hemmeligheten eller deaktiverer API-klienten under **Oppsett → Integrasjon**.

## Feilsøking

| Problem | Årsak | Løsning |
|---------|-------|---------|
| «Ditio-pålogging mangler» | Verken ClientId/ClientSecret eller AccessToken er fylt ut | Fyll ut ClientId og ClientSecret under Rediger parametere |
| «Ditio-pålogging feilet» med `invalid_client` | Feil ClientId/ClientSecret, eller API-klienten er laget i et annet miljø | Kontroller verdiene, og at IdentityUrl og ReportingApiUrl peker på samme miljø |
| «Ditio-pålogging feilet» med `invalid_scope` | API-klienten mangler tilgang til rapportdata | Kontakt support@ditio.no |
| «Ditio API-kall feilet» med HTTP 403 | Brukeren API-klienten er knyttet til, har ikke tilgang. Prosjekter, ressurser og brukere krever administrator. | Be en administrator sjekke at API-klienten er knyttet til en administrator i Ditio |
| *Formula.Firewall* | Personvernnivå er ikke satt eller satt til Privat | Sett begge Ditio-adressene til Organisasjon under Innstillinger for datakilde (*File → Options and settings → Data source settings*) |
| «Kan ikke teste tilkoblingen» i Power BI-tjenesten | Testen sendes uten Ditio-pålogging | Huk av for «Hopp over testtilkobling» |
| «Ditio API-kall feilet» fra v1/payroll-lines-extended | Lønnsgrunnlaget krever at API-klienten er knyttet til en administrator | Knytt API-klienten til en administrator, eller sett IncludePayroll til `false` |
| En tabell har 0 rader | Firmaet bruker ikke den delen av Ditio, eller perioden er feil | Sjekk RangeStart/RangeEnd og CompanyId |
| «Ditio API-kall feilet» med HTTP 401 | IdentityUrl og ReportingApiUrl peker på ulike miljøer (test og produksjon); en utfylt AccessToken er utløpt eller mangler `reportingapiv1`; eller én tabell tok lenger tid å laste enn påloggingen varer | Bruk produksjonsverdiene (eller testverdiene) for begge; tøm AccessToken og bruk ClientId/ClientSecret; korte ned perioden |

## Hjelp

- Feltordliste og målinger: [feltordliste.md](feltordliste.md)
- Utviklerdokumentasjon: [docs.ditio.app](https://docs.ditio.app/guides/powerbi/)
- Kundestøtte: [support@ditio.no](mailto:support@ditio.no)

Har du laget en rapport du er fornøyd med, eller savner du noe i malen? Si gjerne fra til support@ditio.no.
