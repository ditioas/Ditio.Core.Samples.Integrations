# Power BI template

A Power BI template that loads Ditio data through the **Data Extraction API** (`v1/*` on the Reporting API) — the same paginated, documented endpoints described in [`data-extraction`](../data-extraction/README.md).

**File:** [`Ditio Data Extraction v2.0.0.pbit`](Ditio%20Data%20Extraction%20v2.0.0.pbit)

## What it loads

| Table | Endpoint | Filtered by date window |
|-------|----------|:---:|
| `Projects` | `v1/project` | |
| `WorkOrders` | `v1/project/work-breakdown-structure` | |
| `Resources` | `v1/resource` | |
| `Users` | `v1/user` | |
| `TimeRegistrations` | `v1/time-registrations` | ✓ |
| `MachineRegistrations` | `v1/machine-registrations` | ✓ |
| `AbsenceRegistrations` | `v1/absence-registrations` | ✓ |
| `IncidentRegistrations` | `v1/incident-registrations` | ✓ |
| `ChecklistRegistrations` | `v1/checklist-registrations` | ✓ |
| `FlowTripRegistrations` | `v1/flow-trip-registrations` | ✓ |
| `ItemRegistrations` | `v1/item-registrations/web-query` | ✓ |

The registration tables are related to `Projects` (`projectId`), `WorkOrders` (`taskId`) and `Resources` (`resourceId` / `machineId`). Each table keeps the scalar fields of the endpoint; see the [Data Extraction API reference](https://docs.ditio.app/api-reference/data-extraction/) for what each field means. Deleted records are filtered out.

## Set up

1. **Create an API client.** In Ditio Web → **Company Setup → Integration** (Administrator access), create an API client and copy its `client_id` and `client_secret`. The client needs the `reportingapiv1` scope — if the token request fails with `invalid_scope`, contact [support@ditio.no](mailto:support@ditio.no).
2. **Open the template** in Power BI Desktop. It asks for these parameters:

   | Parameter | Description |
   |-----------|-------------|
   | `ReportingApiUrl` | `https://core-api.ditio.app/reporting` (production) or `https://core-api.ditio.dev/reporting` (test) |
   | `IdentityUrl` | `https://identity.ditio.app` (production) or `https://identity.ditio.dev` (test). Must match `ReportingApiUrl`. |
   | `ClientId` / `ClientSecret` | The API client from step 1. The template requests a fresh token on every refresh. |
   | `AccessToken` | Optional. A ready-made token with the `reportingapiv1` scope, used instead of `ClientId`/`ClientSecret`. |
   | `FromDate` / `ToDate` | The registrations to load, both days inclusive. Projects, work orders, resources and users are always loaded in full. |
   | `CompanyId` | Optional. Only load data registered in this Ditio company. Leave empty to load everything the API client can see. Not applied to `ItemRegistrations`. |

3. When asked how to connect to `core-api.ditio.app` and `identity.ditio.app`, choose **Anonymous** — the template sends its own bearer token. When asked about privacy levels, set both to **Organizational** (not *Private* — Power BI won't combine two Private sources).

Large date windows are fine: every endpoint is read page by page (`continuationToken`) until all data is loaded.

## Scheduled refresh in the Power BI service

After publishing, open the semantic model's **Settings → Data source credentials** and, for both the Reporting API and the identity server:

- Authentication method: **Anonymous**
- Privacy level: **Organizational**
- Tick **Skip test connection**. The test connection is sent without the template's bearer token and fails; the real refresh works.

`ClientSecret` is stored as a parameter in the report. Treat the `.pbix` like a credential and only share it with people who may see the API client's data — or use a dedicated, read-only API client for Power BI.

## Upgrading from the v1.x template

v1.x (`Ditio api - data source examples v1.6.0.pbit`) read internal Ditio endpoints that are not part of the public API and can change or stop working without notice. v2 reads only documented Data Extraction endpoints. Field names are now camelCase, as returned by the API.

| v1.x table | v2 table |
|------------|----------|
| `ProjectTransactions_ChunckLoaded` | `TimeRegistrations` |
| `Users` | `Users` |
| `AbsenceRegistrations` | `AbsenceRegistrations` (one row per absence day) |
| `Notifications` | `IncidentRegistrations` |
| `ItemTransactions` | `ItemRegistrations` |
| `TripLog_ChunkLoaded` | `FlowTripRegistrations` (`DumpLengdegrad`/`DumpBreddegrad` → `dumpLongitude`/`dumpLatitude`) |
| — | `Projects`, `WorkOrders`, `Resources`, `MachineRegistrations`, `ChecklistRegistrations` (new) |

The `AuthToken`, `ApiUrl`, `TransportApiUrl` and `ProjectCompanyId` parameters are replaced by `ClientId`/`ClientSecret` (or `AccessToken`), `ReportingApiUrl`/`IdentityUrl` and `CompanyId`. `FromDate`/`ToDate` are now dates rather than text.

To move an existing report, add the v2 queries from [`queries/`](queries/) to it (Power Query → **New Source → Blank Query → Advanced Editor**, one query per file, named after the file) and repoint your visuals to the new tables and fields.

## Notes

- `ItemRegistrations` comes from an endpoint that returns semicolon-separated text, not JSON, and has no ids — only names and project numbers. Its header row is ignored and the columns are named by position. It is not paged, and a `"` inside an item name or description currently breaks parsing of that row.
- Date/time columns hold the value the API returns, without time-zone conversion, so Power BI Desktop and the Power BI service show the same times.
- A fresh access token is requested for each table. A single table that takes longer than the token lifetime to load fails with `401`; narrow `FromDate`/`ToDate` if that happens.
- `Users` has one row per company profile. Personal fields (birth date, address, next of kin) are left out on purpose; add them in Power Query if your report needs them.

## Editing the template

The template is generated. [`build/build_pbit.py`](build/build_pbit.py) holds the tables, columns, relationships and Power Query code, and writes both the `.pbit` and the readable [`queries/*.pq`](queries/):

```bash
python3 power-bi/build/build_pbit.py
```

Change the script rather than the `.pbit` or the `.pq` files, then rebuild and bump `VERSION` for a release.
