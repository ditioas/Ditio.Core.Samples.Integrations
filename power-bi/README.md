# Power BI template

**For customers:** see the Norwegian user guide, [`BRUKERVEILEDNING.md`](BRUKERVEILEDNING.md).

A Power BI template that loads Ditio data through the **Data Extraction API** (`v1/*` on the Reporting API) — the same paginated, documented endpoints described in [`data-extraction`](../data-extraction/README.md). The report is in Norwegian.

**File:** [`Ditio-PowerBI-mal.pbit`](Ditio-PowerBI-mal.pbit) (v2.1.0)

## What it loads

| Table | Endpoint | Filtered by date window |
|-------|----------|:---:|
| `Prosjekter` | `v1/project` | |
| `Arbeidsordrer` | `v1/project/work-breakdown-structure` | |
| `Ressurser` | `v1/resource` | |
| `Brukere` | `v1/user` | |
| `Timeføringer` | `v1/time-registrations` | ✓ |
| `Maskinregistreringer` | `v1/machine-registrations` | ✓ |
| `Fravær` | `v1/absence-registrations` | ✓ |
| `Varsler` | `v1/incident-registrations` | ✓ |
| `Sjekklister` | `v1/checklist-registrations` | ✓ |
| `Massetransport` | `v1/flow-trip-registrations` | ✓ |
| `Varetransaksjoner` | `v1/item-registrations/web-query` | ✓ |

The model also has:

- **`Dato`**: one row per day in the loaded period, with ISO weeks (Monday start), Norwegian month and weekday names. It is marked as the date table, and every registration table is related to it on its `Dato` column.
- **`Målinger`**: ready-made measures in display folders: hours (person, machine, vehicle, approved, payroll-approved, unapproved older than 14 days), machines, absence, HSE and quality (incidents per 100,000 person hours, checklists with deviations), mass transport (tonnes and m³ kept apart), items, economy (cost, sales, contribution margin and how complete prices are) and data status.
- **`Datakilder`**, **`Feltordliste`** and **`Datagrunnlag`**: the tables behind the Start and Feltordliste pages.

The registration tables are related to `Prosjekter`, `Arbeidsordrer` and `Ressurser`; `Timeføringer` and `Fravær` also to `Brukere`. Status, base type, unit and similar fields are shown in Norwegian (e.g. *Åpen / Pågår / Lukket*). Ids and other keys are hidden. Deleted records are filtered out.

### Field names

Columns carry the **same names as Ditio's Excel export** ("Kostpris maskin", "Stilling", "Prosjektnr"…), so a field reads the same in Power BI as in the Excel file. Hovering a field in Power BI shows the API field it comes from. [`feltordliste.md`](feltordliste.md) and the report's *Feltordliste* page list every field with its report label, API field and Excel column.

## Pages

| Page | Shows |
|------|-------|
| **Start** | Loaded period, last refresh, rows and newest change per table, how to use the report |
| **Feltordliste** | Every field: report label, API field, Excel column |

More pages (overview, hours, machines, mass transport, HSE and quality, payroll, absence, economy) follow in later versions.

## Set up

1. **Create an API client.** In Ditio Web → **Company Setup → Integration** (Administrator access), create an API client and copy its `client_id` and `client_secret`. The client needs the `reportingapiv1` scope — if the token request fails with `invalid_scope`, contact [support@ditio.no](mailto:support@ditio.no).
2. **Open the template** in Power BI Desktop. It asks for these parameters:

   | Parameter | Description |
   |-----------|-------------|
   | `ReportingApiUrl` | `https://core-api.ditio.app/reporting` (production) or `https://core-api.ditio.dev/reporting` (test) |
   | `IdentityUrl` | `https://identity.ditio.app` (production) or `https://identity.ditio.dev` (test). Must match `ReportingApiUrl`. |
   | `CoreApiUrl` | `https://integration.ditio.no` (production) or `https://core-api.ditio.dev/core` (test). Only for your own queries against the Core API; the template's tables don't use it. |
   | `ClientId` / `ClientSecret` | The API client from step 1. The template requests a fresh token on every refresh. |
   | `AccessToken` | Optional. A ready-made token with the `reportingapiv1` scope, used instead of `ClientId`/`ClientSecret`. |
   | `FromDate` / `ToDate` | The registrations to load, both days inclusive. Projects, work orders, resources and users are always loaded in full. |
   | `CompanyId` | Optional. Only load data registered in this Ditio company. Leave empty to load everything the API client can see. Not applied to `Varetransaksjoner`. |

3. When asked how to connect to `core-api.ditio.app` and `identity.ditio.app`, choose **Anonymous** — the template sends its own bearer token. When asked about privacy levels, set both to **Organizational** (not *Private* — Power BI won't combine two Private sources).

Large date windows are fine: every endpoint is read page by page (`continuationToken`) until all data is loaded.

The template contains no company data, credentials or tokens. You enter them when you open it.

## Scheduled refresh in the Power BI service

After publishing, open the semantic model's **Settings → Data source credentials** and, for both the Reporting API and the identity server:

- Authentication method: **Anonymous**
- Privacy level: **Organizational**
- Tick **Skip test connection**. The test connection is sent without the template's bearer token and fails; the real refresh works.

`ClientSecret` is stored as a parameter in the report. Treat the `.pbix` like a credential and only share it with people who may see the API client's data — or use a dedicated, read-only API client for Power BI.

## Upgrading

v1.x (`Ditio api - data source examples v1.6.0.pbit`) read internal Ditio endpoints that are not part of the public API and can change or stop working without notice. v2 reads only documented Data Extraction endpoints.

| v1.x table | v2.1 table |
|------------|----------|
| `ProjectTransactions_ChunckLoaded` | `Timeføringer` |
| `Users` | `Brukere` |
| `AbsenceRegistrations` | `Fravær` (one row per absence day) |
| `Notifications` | `Varsler` |
| `ItemTransactions` | `Varetransaksjoner` |
| `TripLog_ChunkLoaded` | `Massetransport` (`DumpLengdegrad`/`DumpBreddegrad` → `Dump lengdegrad`/`Dump breddegrad`) |
| — | `Prosjekter`, `Arbeidsordrer`, `Ressurser`, `Maskinregistreringer`, `Sjekklister` (new) |

The `AuthToken`, `ApiUrl`, `TransportApiUrl` and `ProjectCompanyId` parameters are replaced by `ClientId`/`ClientSecret` (or `AccessToken`), `ReportingApiUrl`/`IdentityUrl`/`CoreApiUrl` and `CompanyId`. `FromDate`/`ToDate` are now dates rather than text.

To move an existing report, add the queries from [`queries/`](queries/) to it (Power Query → **New Source → Blank Query → Advanced Editor**, one query per file, named after the file) and repoint your visuals to the new tables and fields.

## Notes

- `Varetransaksjoner` comes from an endpoint that returns semicolon-separated text, not JSON, and has no ids — only names and project numbers. Its header row is ignored and the columns are named by position. It is not paged, and a `"` inside an item name or description currently breaks parsing of that row.
- Date/time columns hold the value the API returns, without time-zone conversion, so Power BI Desktop and the Power BI service show the same times. *Sist oppdatert* is in UTC.
- `Varsler` and `Sjekklister` are loaded with `ModifiedSince = FromDate`, because their endpoints don't filter on `FromDateTime`/`ToDateTime`. Every registration table is then clipped to `FromDate`–`ToDate` on its date, so all tables cover the same period.
- A fresh access token is requested for each table. A single table that takes longer than the token lifetime to load fails with `401`; narrow `FromDate`/`ToDate` if that happens.
- `Brukere` has one row per **person**, keyed on the identity user id that registrations carry. A person with profiles in several companies (project companies, subsidiaries) is shown with the profile in their employer company; *Antall profiler* says how many profiles they have. Personal fields (birth date, address, next of kin) are left out on purpose.
- *Varsler per 100 000 persontimer* is a reporting rate, not H1/H2: Ditio doesn't register lost-time injuries.
- Economy measures (*Kostbeløp*, *Dekningsbidrag* …) are only as complete as the prices registered in Ditio; *Andel timer med kost* shows how complete they are.

## Editing the template

The template is generated; change the build scripts, never the `.pbit`, `.pq` or `feltordliste.md`:

| File | Holds |
|------|-------|
| [`build/spec.py`](build/spec.py) | Tables, columns, labels, relationships |
| [`build/queries.py`](build/queries.py) | Parameters, shared functions, Power Query per table |
| [`build/measures.py`](build/measures.py) | DAX measures |
| [`build/model.py`](build/model.py) | The tabular model |
| [`build/pages.py`](build/pages.py) | Report pages and theme |
| [`build/checks.py`](build/checks.py) | Checks run on every build |

```bash
python3 power-bi/build/build_pbit.py
# also refresh the Excel-export labels from Ditio's web translations
# (a folder with nb/translation.json and en/translation.json):
python3 power-bi/build/build_pbit.py --refresh-translations <translations folder>
```

Labels written `hdr:<Key>` in `spec.py` come from Ditio's Excel-export translations in [`build/translations/`](build/translations/). Every build fails if:

- a column has no label, or two columns in one table share a label;
- a measure or visual refers to a table, column or measure that doesn't exist, or a measure has the same name as a column;
- anything that ships contains an access token, a client secret, an e-mail address (other than support@ditio.no), a Ditio company id, a customer name or an internal host name.

Bump `VERSION` in `build_pbit.py` for a release. The file name stays `Ditio-PowerBI-mal.pbit` so links from the docs site never change; the version shows on the report's Start page.
