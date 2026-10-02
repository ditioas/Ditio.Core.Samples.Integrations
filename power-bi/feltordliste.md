# Feltordliste

Hva hvert felt heter i Power BI-malen, i Data Extraction API-et og i Ditios Excel-eksport,
og hva hver måling i tabellen «Målinger» betyr.
Generert av `build/build_pbit.py` – ikke rediger for hånd.

## Målinger

| Måling | Mappe | Betydning |
|---|---|---|
| Timer totalt | Timer | Alle registrerte timer: personer, maskiner og kjøretøy. |
| Persontimer | Timer | Timer registrert på personer. |
| Maskintimer | Timer | Timer registrert på maskiner. |
| Kjøretøytimer | Timer | Timer registrert på kjøretøy. |
| Godkjente timer | Timer | Timer som er godkjent. |
| Andel godkjent | Timer | Godkjente timer delt på alle timer. |
| Ikke godkjente timer | Timer | Timer som venter på godkjenning. |
| Ikke godkjent over 14 dager | Timer | Timer som ikke er godkjent, med arbeidsdato mer enn 14 dager tilbake. |
| Lønnsgodkjente timer | Timer | Timer som er godkjent for lønn. |
| Andel lønnsgodkjent | Timer | Lønnsgodkjente timer delt på alle timer. |
| Antall personer | Timer | Personer med timer i perioden. |
| Låste timer | Timer | Timer som er låst. |
| Ikke godkjent 0–7 dager | Timer | Timer som ikke er godkjent, med arbeidsdato de siste 7 dagene (eller fram i tid). |
| Ikke godkjent 8–14 dager | Timer | Timer som ikke er godkjent, med arbeidsdato 8–14 dager tilbake. |
| Ikke godkjent 15–30 dager | Timer | Timer som ikke er godkjent, med arbeidsdato 15–30 dager tilbake. |
| Ikke godkjent over 30 dager | Timer | Timer som ikke er godkjent, med arbeidsdato mer enn 30 dager tilbake. |
| Median godkjenningstid (dager) | Timer | Median antall dager fra arbeidsdato til godkjenning, per godkjent timeføring. |
| Timer maskinregistreringer | Maskiner | Timer fra maskinregistreringer (eget skjema, ikke timeføringer). |
| Aktive maskiner | Maskiner | Maskiner og kjøretøy med timer i perioden. |
| Timer per aktiv maskin | Maskiner | Maskin- og kjøretøytimer delt på antall aktive maskiner. |
| Maskin- og kjøretøytimer | Maskiner | Timer registrert på maskiner og kjøretøy. |
| Aktive maskindager | Maskiner | Antall kombinasjoner av maskin (eller kjøretøy) og dag med timer. |
| Timer per aktiv maskindag | Maskiner | Maskin- og kjøretøytimer delt på aktive maskindager. Erstatter utnyttelsesgrad, som krever tilgjengelige timer Ditio ikke har. |
| Siste timeføring | Maskiner | Siste arbeidsdato med timer. Per maskin viser den maskiner som har stått stille. |
| Fraværstimer | Fravær | Fravær i timer. |
| Fraværsdager | Fravær | Antall fraværsdager (én rad per person per dag). |
| Godkjente fraværstimer | Fravær | Fravær i timer som er godkjent. |
| Antall varsler | HMS og kvalitet | Alle varsler. |
| Åpne varsler | HMS og kvalitet | Varsler som ikke er lukket. |
| HMS-varsler | HMS og kvalitet | Varsler med hovedtype HMS. |
| Varsler per 100 000 persontimer | HMS og kvalitet | Rapporteringsrate: varsler per 100 000 registrerte persontimer. |
| HMS-varsler per 100 000 persontimer | HMS og kvalitet | HMS-varsler per 100 000 registrerte persontimer. Ikke det samme som H1/H2, som krever fraværsskader. |
| Antall sjekklister | HMS og kvalitet | Alle sjekklister. |
| Sjekklister med avvik | HMS og kvalitet | Sjekklister med minst ett avvik. |
| Avviksandel sjekklister | HMS og kvalitet | Sjekklister med avvik delt på alle sjekklister. |
| Antall turer | Massetransport | Antall turer. |
| Mengde tonn | Massetransport | Mengde på turer registrert i tonn. |
| Mengde m³ | Massetransport | Mengde på turer registrert i m³. |
| Mengde i valgt enhet | Massetransport | Mengde når bare én enhet er valgt. Tom når tonn og m³ er blandet, så de aldri summeres sammen. |
| Turer per dag | Massetransport | Antall turer delt på antall dager med turer. |
| Snitt syklustid (min) | Massetransport | Gjennomsnittlig syklustid per tur i minutter. |
| Antall varetransaksjoner | Varer | Alle varetransaksjoner. |
| Varebeløp | Varer | Beløp på varetransaksjoner. |
| Varekost | Varer | Kost på varetransaksjoner. |
| Kostbeløp | Økonomi | Kost på timeføringer (mann og maskin). |
| Salgsbeløp | Økonomi | Salgsbeløp på timeføringer. |
| Dekningsbidrag | Økonomi | Salgsbeløp minus kostbeløp. |
| Dekningsgrad | Økonomi | Dekningsbidrag delt på salgsbeløp. |
| Andel timer med kost | Økonomi | Hvor stor del av timene som har kostpris. Lav andel betyr at kost- og marginstall er ufullstendige. |
| Andel timer med salgspris | Økonomi | Hvor stor del av timene som har salgspris. |
| Rader | Datagrunnlag | Antall rader lastet i tabellen. Brukes i tabellen over datakilder. |
| Sist endret i data | Datagrunnlag | Nyeste endring i tabellens data. Viser hvor ferske dataene er. |
| Data fra | Datagrunnlag | Første dag med registreringer i dataene som er lastet. |
| Data til | Datagrunnlag | Siste dag med registreringer i dataene som er lastet. |
| Sist oppdatert (UTC) | Datagrunnlag | Når dataene sist ble hentet fra Ditio. |

## Prosjekter

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Prosjektnr | `number` | Prosjektnr |  |
| Prosjektnavn | `name` |  |  |
| Eksternt-prosjektnr | `externalNumber` | Eksternt-prosjektnr |  |
| Ekstern dimensjon 1 (PK) | `externalDim01` | Ekstern dimensjon 1 (PK) |  |
| Ekstern dimensjon 2 | `externalDim02` | Ekstern dimensjon 2 |  |
| Firma | `companyName` | Firma |  |
| Aktiv | `active` |  |  |
| Eksternt prosjekt | `isExternal` |  |  |
| Opprettet | `createdDateTime` |  |  |
| Sist endret | `modifiedDateTime` |  |  |

## Arbeidsordrer

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Arbeidsordrenr | `number` |  |  |
| Arbeidsordre | `name` |  |  |
| Arbeidsordre med nr | `nameWithNumber` |  |  |
| Kapittel | `chapterId` |  |  |
| Produksjonskode | `externalNumber` |  |  |
| Ekstern dimensjon 1 (PK) | `externalDim01` | Ekstern dimensjon 1 (PK) |  |
| Ekstern dimensjon 2 | `externalDim02` | Ekstern dimensjon 2 |  |
| Akkord-id | `externalPieceWorkId` | Akkord-id |  |
| Overordnet arbeidsordre | `parentName` |  |  |
| Sti | `fullPathName` | Sti |  |
| Firma | `companyName` | Firma |  |
| Ekstern | `isExternal` |  |  |
| Sist endret | `modifiedDateTime` |  |  |

## Ressurser

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Ressursnr | `number` | Ressursnr |  |
| Ressursnavn | `name` | Ressursnavn |  |
| Ressurstype | `typeName` | Ressurstype |  |
| Registreringsnummer | `registrationNumber` |  |  |
| Serienummer | `serialNumber` |  |  |
| Avdeling | `department` |  |  |
| Utslippsklasse | `emissionClass` |  |  |
| Byggeår | `buildYear` |  |  |
| Vekt | `weight` |  |  |
| Kapasitet | `capacity` |  |  |
| Kostpris | `costPrice` | Kostpris |  |
| Pris | `price` |  |  |
| Firma | `companyName` | Firma |  |
| Aktiv | `active` |  |  |
| Ekstern ressurs | `isExternal` |  |  |
| Sist endret | `modifiedDateTime` |  |  |
| Ressursgruppe | `resourceGroup` |  | Norsk visning av API-feltet typeBaseName. |

## Brukere

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Ansattnummer | `employeeNumber` | Ansattnummer |  |
| Navn | `name` |  |  |
| Fornavn | `firstName` |  |  |
| Etternavn | `lastName` |  |  |
| E-post | `email` |  |  |
| Stilling | `workTitle` | Stilling |  |
| Avdeling | `department` |  |  |
| Firma | `companyName` | Firma |  |
| Ansatt i firma | `employmentCompanyName` |  |  |
| Aktivt ansatt | `isActiveEmployment` |  |  |
| Deaktivert | `isDisabled` |  |  |
| Ansatt fra | `startDate` |  |  |
| Ansatt til | `endDate` |  |  |
| Sist endret | `modifiedDateTime` |  |  |
| Antall profiler | `profileCount` |  | Antall firmaprofiler personen har. Raden viser profilen i arbeidsgiverfirmaet. |

## Timeføringer

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Arbeidsdato | `workDate` |  |  |
| Start | `startDateTime` |  |  |
| Slutt | `stopDateTime` |  |  |
| Timer netto | `qty` | Timer netto |  |
| Enhet | `unitName` | Enhet |  |
| Enhetsantall | `unitQty` | Enhetsantall |  |
| Enhetspris | `unitPrice` |  |  |
| Pris | `price` |  |  |
| Beløp | `amount` | Beløp |  |
| Kostpris grunnlag | `costPriceBase` |  |  |
| Kostpris mann | `costPriceMan` | Kostpris mann |  |
| Kostpris maskin | `costPriceMachine` | Kostpris maskin |  |
| Kost | `costAmount` | Kost |  |
| Kost mann | `costAmountMan` | Kost mann |  |
| Kost maskin | `costAmountMachine` | Kost maskin |  |
| Pause summert | `breakQty` | Pause summert |  |
| Ekstern kommentar | `description` | Ekstern kommentar |  |
| Intern kommentar | `descriptionInternal` | Intern kommentar |  |
| Godkjent | `approved` |  |  |
| Godkjent av | `approvedByName` | Godkjent av |  |
| Godkjent dato | `approvedDateTime` | Godkjent dato |  |
| Lønnsgodkjent | `payrollApproved` |  |  |
| Lønnsgodkjent dato | `payrollApprovedDateTime` |  |  |
| Låst | `locked` |  |  |
| Låst av | `lockedByName` | Låst av |  |
| Låst dato | `lockedDateTime` | Låst dato |  |
| Fakturert | `invoiced` | Fakturert |  |
| Prosjektnr | `projectNumber` | Prosjektnr |  |
| Prosjektnavn | `projectName` |  |  |
| Eksternt-prosjektnr | `projectExternalNumber` | Eksternt-prosjektnr |  |
| Prosjektfirma | `projectCompanyName` | Prosjektfirma |  |
| Prosjektleder | `projectLeaderName` |  |  |
| Anleggsleder | `projectMainForemanName` |  |  |
| Arbeidsordre | `taskName` |  |  |
| Sti | `taskNameLong` | Sti |  |
| Arbeidsordrenr | `taskWbsNumber` |  |  |
| Kapittel | `taskChapterId` |  |  |
| Akkord-id | `taskPieceWorkId` | Akkord-id |  |
| Ressursnr | `resourceNumber` | Ressursnr |  |
| Ressursnavn | `resourceName` | Ressursnavn |  |
| Ressurstype | `resourceTypeName` | Ressurstype |  |
| Primærressurs | `primaryResourceName` |  |  |
| Utslippsklasse | `machineEmissionClass` |  |  |
| Navn | `userName` |  |  |
| Ansattnummer | `userEmployeeNumber` | Ansattnummer |  |
| Firma | `userCompanyName` | Firma |  |
| Firmanr | `userCompanyOrgNr` | Firmanr |  |
| Stilling | `userWorkTitle` | Stilling |  |
| Arbeidstidsordning | `userWorkShiftSettingName` |  |  |
| Opprettet | `createdDateTime` |  |  |
| Sist endret | `modifiedDateTime` |  |  |

## Maskinregistreringer

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Start | `startDateTime` |  |  |
| Slutt | `stopDateTime` |  |  |
| Timer | `qty` |  |  |
| Kommentar | `description` |  |  |
| Godkjent | `approved` |  |  |
| Maskin | `machineName` |  |  |
| Ressursnr | `resourceNumber` | Ressursnr |  |
| Ressurstype | `machineTypeName` | Ressurstype |  |
| Fører | `driverName` |  |  |
| Førers firma | `driverCompanyName` |  |  |
| Prosjektnr | `projectNumber` | Prosjektnr |  |
| Prosjektnavn | `projectName` |  |  |
| Eksternt-prosjektnr | `projectExternalNumber` | Eksternt-prosjektnr |  |
| Prosjektfirma | `projectCompanyName` | Prosjektfirma |  |
| Opprettet | `createdDateTime` |  |  |
| Sist endret | `modifiedDateTime` |  |  |

## Fravær

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Fraværsdato | `date` |  |  |
| Start | `startTime` |  |  |
| Timer | `qty` |  |  |
| Fraværskode | `absenceTypeCode` |  |  |
| Fraværstype | `absenceTypeName` |  |  |
| Ansattnummer | `employeeNumber` | Ansattnummer |  |
| Godkjent | `approved` |  |  |
| Godkjent av | `approvedByName` | Godkjent av |  |
| Godkjent dato | `approvedDateTime` | Godkjent dato |  |
| Lønnsgodkjent | `payrollApproved` |  |  |
| Lønnsgodkjent dato | `payrollApprovedDateTime` |  |  |
| Låst | `locked` |  |  |
| Låst dato | `lockedDateTime` | Låst dato |  |
| PDF | `pdfUrl` |  |  |
| Sist endret | `modifiedDateTime` |  |  |

## Varsler

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Varselnr | `serialNumberWithPrefix` |  |  |
| Tittel | `title` |  |  |
| Beskrivelse | `description` |  |  |
| Varselstype | `typeName` |  |  |
| Tiltak | `measures` |  |  |
| Krever oppfølging | `requiresFurtherAction` |  |  |
| Løst på stedet | `resolvedOnLocation` |  |  |
| Firma | `companyName` | Firma |  |
| Prosjektnr | `projectNumber` | Prosjektnr |  |
| Prosjektnavn | `projectName` |  |  |
| Prosjektfirma | `projectCompanyName` | Prosjektfirma |  |
| Arbeidsordre | `taskName` |  |  |
| Maskin | `machineName` |  |  |
| Breddegrad | `latitude` |  |  |
| Lengdegrad | `longitude` |  |  |
| Opprettet | `createdAt` |  |  |
| Status endret | `statusUpdatedAt` |  |  |
| Sist endret | `modifiedDateTime` |  |  |
| PDF | `pdfUrl` |  |  |
| Hovedtype | `baseTypeName` |  | Norsk visning av API-feltet baseTypeText. |
| Status | `statusName` |  | Norsk visning av API-feltet statusText. |

## Sjekklister

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Dokumentnr | `documentNumber` |  |  |
| Løpenr | `serialNumber` |  |  |
| Mal | `templateName` |  |  |
| Malversjon | `templateVersion` |  |  |
| Maltype | `baseTypeName` |  |  |
| Har avvik | `hasDeviation` |  |  |
| Har kommentar | `hasComment` |  |  |
| Har bilder | `hasImages` |  |  |
| Prosjektnr | `projectNumber` | Prosjektnr |  |
| Prosjektnavn | `projectName` |  |  |
| Arbeidsordrenr | `activityNumber` |  |  |
| Arbeidsordre | `activityName` |  |  |
| Ressursnr | `resourceNumber` | Ressursnr |  |
| Opprettet av | `createdByUserName` |  |  |
| Opprettet | `createdDateTime` |  |  |
| Sendt inn av | `submittedByUserName` |  |  |
| Sendt inn | `submittedDateTime` |  |  |
| Godkjent av | `approvedByUserName` | Godkjent av |  |
| Breddegrad | `latitude` |  |  |
| Lengdegrad | `longitude` |  |  |
| Sist endret | `modifiedDateTime` |  |  |
| PDF | `pdfUrl` |  |  |
| Status | `statusName` |  | Norsk visning av API-feltet statusText. |

## Massetransport

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Lastet | `loadDateTime` |  |  |
| Tippet | `dumpDateTime` |  |  |
| Kost start | `costStartDateTime` |  |  |
| Kost slutt | `costEndDateTime` |  |  |
| Mengde | `quantity` |  |  |
| Volum m³ (omregnet) | `quantityM3` |  |  |
| Registrert mengde | `registeredQuantity` |  |  |
| Verifisert mengde | `verifiedQuantity` |  |  |
| Massetype | `massType` |  |  |
| m³ per time | `m3PerHour` |  |  |
| Syklustid (sek) | `totalCycleTimeSeconds` |  |  |
| Syklustid (timer) | `totalCycleTimeHours` |  |  |
| Utnyttelsesgrad | `utilizationRate` |  |  |
| Tur avstand | `distance` |  |  |
| Tur fart (km/t) | `averageSpeedKmh` |  |  |
| Netto høydeendring | `netAltitudeChange` |  |  |
| Total stigning | `totalAscent` |  |  |
| Maks stigningsgrad | `maxGradient` |  |  |
| Snitt stigningsgrad | `averageGradient` |  |  |
| Lasteområde | `loadLocationName` |  |  |
| Dumpeområde | `dumpLocationName` |  |  |
| Last lengdegrad | `loadLongitude` |  |  |
| Last breddegrad | `loadLatitude` |  |  |
| Dump lengdegrad | `dumpLongitude` |  |  |
| Dump breddegrad | `dumpLatitude` |  |  |
| Laster | `loaderName` |  |  |
| Lasternr | `loaderNumber` |  |  |
| Lasterfører | `loaderDriverName` |  |  |
| Dumper | `dumperName` |  |  |
| Dumpernr | `dumperNumber` |  |  |
| Dumper reg.nr | `dumperRegistrationNumber` |  |  |
| Dumperfører | `dumperDriverName` |  |  |
| Dumperførers firma | `dumperDriverCompanyName` |  |  |
| Tilhenger | `trailer` |  |  |
| Verifisert | `verified` |  |  |
| Godkjent | `approved` |  |  |
| Fakturert | `invoiced` | Fakturert |  |
| Kvitteringsnr | `receiptNumber` |  |  |
| Kunde | `customerName` |  |  |
| Kommentar | `comment` |  |  |
| Prosjektnr | `projectNumber` | Prosjektnr |  |
| Prosjektnavn | `projectName` |  |  |
| Eksternt-prosjektnr | `projectExternalNumber` | Eksternt-prosjektnr |  |
| Prosjektfirma | `projectCompanyName` | Prosjektfirma |  |
| Arbeidsordre | `taskName` |  |  |
| Fra Ditio Flow | `isFlowData` |  |  |
| Sist endret | `modifiedDateTime` |  |  |
| Enhet | `unitName` |  | Norsk visning av API-feltet unitOfMeasureText. |

## Varetransaksjoner

| Felt i rapporten | API-felt | Excel-kolonne | Beskrivelse |
|---|---|---|---|
| Vare | `itemName` |  |  |
| Prosjektnavn | `projectName` |  |  |
| Prosjektnr | `projectNumber` | Prosjektnr |  |
| Arbeidsordre | `taskName` |  |  |
| Navn | `userName` |  |  |
| Transaksjonsdato | `transDate` |  |  |
| Maskin | `machineName` |  |  |
| Antall | `qty` |  |  |
| Kostpris | `costPrice` | Kostpris |  |
| Pris | `price` |  |  |
| Kost | `costAmount` | Kost |  |
| Beløp | `amount` | Beløp |  |
| Lager | `warehouseName` |  |  |
| Lagerantall | `warehouseQty` |  |  |
| Kommentar | `description` |  |  |
| Transaksjonstype | `transTypeName` |  | Norsk visning av API-feltet transType. |
