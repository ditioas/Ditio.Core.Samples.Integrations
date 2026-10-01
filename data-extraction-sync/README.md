# 13 — Data extraction: incremental sync to a local database

Keep a local copy of your Ditio data current with small deltas, and point Power BI (or anything else) at the copy.

The [Data Extraction API](../data-extraction/README.md) (`v1/*` on the reporting host, scope `reportingapiv1`) is built for this. Ask it for everything once, then ask only for what changed since your last run (`ModifiedSince`). Changed records come back in full and deleted records come back as tombstones. Apply both and your copy matches Ditio again.

Power BI on its own can't do this. A refresh re-reads every record in the window, and Power BI's incremental refresh only re-reads recent date partitions. Neither notices that a registration from three months ago was approved, corrected or deleted today. A sync job that runs on `ModifiedSince` does.

```
          every 15–60 min                       any time
Ditio  ───────────────────▶  sync job  ───▶  database  ◀───  Power BI / Excel / SQL
 v1/*    ModifiedSince +                  (tables + views)
         continuationToken
```

**C#:** [`IncrementalSyncExample.cs`](IncrementalSyncExample.cs) syncs into a local **SQLite** file. The database code is in [`SyncStore.cs`](SyncStore.cs), the endpoints and view columns in [`SyncEndpoint.cs`](SyncEndpoint.cs) and the settings in [`IncrementalSyncConfig.cs`](IncrementalSyncConfig.cs).

## How the API behaves

These rules apply to every `v1/*` extraction endpoint used here.

**Full or incremental is decided by `ModifiedSince`.** Without it, the call is a full sync and the response says `"syncTypeName": "full"`. With it, the call is incremental (`"syncTypeName": "incremental"`) and returns only records created, changed or deleted since that time, oldest change first.

**Follow `continuationToken` until it is empty.** Pass the token back exactly as you got it. Treat it as opaque. In incremental mode, **send `ModifiedSince` again on every page**, because the API reads the token as a modified-time position only when `ModifiedSince` is present. In a full sync, the token is a position in a different order (work date, load date or id, depending on the endpoint), so never mix the tokens from a full sync and an incremental sync.

**Pages never split a timestamp.** A page ends only where the modified time moves on. All records that share a timestamp land on the same page, so none are skipped or repeated between pages. This means a page can hold a few more records than `ChunkLimit`. `ChunkLimit` defaults to 2,500 and is capped at 10,000.

**Deletions come back only in incremental responses**, as records of the same type with `"isDeleted": true` and a `deletedDateTime`:

- **Registrations** (time, machine, absence, incident) come back as a tombstone. It has the `id`, `isDeleted`, `deletedDateTime` and `modifiedDateTime` (equal to the deletion time); the other fields are empty. Look the record up by `id`, and don't overwrite your copy with the empty fields.
- **Projects, work orders, resources and users** come back as their last stored version with `isDeleted: true`. Their `modifiedDateTime` is the last change *before* the deletion, so use `deletedDateTime` for when it was deleted.
- **Checklists** come back as the full record with `isDeleted: true`.
- `deletedRecordCount` tells you how many deletions the page holds. `recordCount` counts both live and deleted records.
- The same deletion can appear on more than one page or more than one run. Applying it twice must be harmless.

A full sync never returns deleted records, so a full sync is a complete snapshot of what exists now.

**`ModifiedBefore`** closes the window (`ModifiedSince` ≤ modified < `ModifiedBefore`). It needs `ModifiedSince` and must be later than it. You don't need it for a sync job.

**Date filters.** `FromDateTime`/`ToDateTime` filter on the registration's own date and are meant for full loads of a period. `v1/incident-registrations` and `v1/checklist-registrations` ignore them. All the endpoints here honour `ModifiedSince`.

## How the job uses it

For each endpoint, in order:

1. **Note the start time, before the first request.** Anything that changes while the job pages through is either on a page it still reads or after this time, so the next run picks it up.
2. **Choose the mode.** With no stored watermark, the job runs a full sync. Otherwise it runs an incremental sync with `ModifiedSince` set to *previous run's start − overlap*.
3. **Page** with `ChunkLimit=5000` and the `continuationToken` until the token is empty.
4. **Apply** every record in one database transaction. A live record is upserted by `id`. A record with `isDeleted: true` sets `deleted = 1` (with `modified` set to its `deletedDateTime`) and keeps the last version the job saw.
5. **Save the start time as the new watermark**, as the last write in the same transaction, then commit.

If anything fails (network, 401, 500, a crash), the transaction rolls back and the watermark stays where it was. The next run asks for the same window again, so nothing is lost. The other endpoints carry on, and the process exits with code `1` so your scheduler can alert you.

**Why the overlap (default 10 minutes).** The watermark comes from this machine's clock, but Ditio compares it with its own. The overlap absorbs clock skew between the two, plus any write that landed at the edge of the previous run. The cost is that a few records are read twice. Upserts by `id` make that harmless.

**Deleted records are flagged, not removed.** Each table keeps a deleted row with `deleted = 1` and its last known JSON, so you can still see what was deleted. The views leave deleted rows out. If you'd rather drop them, change `MarkDeleted` in `SyncStore.cs` to a `DELETE`.

**A full sync replaces the table** in the same transaction, because a full sync is the complete truth. To force one, set `ForceFullSync` to `true` for a single run, or delete the endpoint's row from `sync_state`.

## What ends up in the database

| Endpoint | Table | View |
|----------|-------|------|
| `v1/time-registrations` | `time_registrations` | `vw_time_registrations` |
| `v1/absence-registrations` | `absence_registrations` | `vw_absence_registrations` |
| `v1/machine-registrations` | `machine_registrations` | `vw_machine_registrations` |
| `v1/incident-registrations` | `incident_registrations` | `vw_incident_registrations` |
| `v1/checklist-registrations` | `checklist_registrations` | `vw_checklist_registrations` |
| `v1/flow-trip-registrations` | `flow_trip_registrations` | `vw_flow_trip_registrations` |
| `v1/project` | `projects` | `vw_projects` |
| `v1/project/work-breakdown-structure` | `work_orders` | `vw_work_orders` |
| `v1/resource` | `resources` | `vw_resources` |
| `v1/user` | `users` | `vw_users` |

- **Tables** have one row per record: `id TEXT PRIMARY KEY`, `modified TEXT`, `deleted INTEGER`, and `json TEXT` holding the record exactly as the API returned it. No field is lost, even fields added to the API later.
- **Views** pull the commonly used fields out of the JSON with `json_extract` and leave out deleted rows. To add a column, add the field name to the endpoint's list in [`SyncEndpoint.cs`](SyncEndpoint.cs). The views are recreated on every run. Field names are the API's (camelCase); see Swagger for the full list.
- **`sync_state`** has one row per endpoint: `last_sync_started_utc` (the watermark), `last_sync_completed_utc` and `last_full_sync_utc`. Show `last_sync_completed_utc` in your report so readers can see how fresh the data is.

For example, hours per project and month, approved only:

```sql
SELECT projectNumber, substr(workDate, 1, 7) AS month, SUM(qty) AS hours
FROM vw_time_registrations
WHERE approved = 1
GROUP BY projectNumber, month;
```

> The database holds your company's data, including personal data (users, time and absence registrations). Store it where only the right people can read it, and keep it out of source control. `ditio-sync.db*` is git-ignored in this repo.

## Run it

1. Create an API client with the `reportingapiv1` scope and fill in `appsettings.json` (see the [root README](../README.md#run-the-c-examples)).
2. Optional: add a `DataExtractionSync` section to `appsettings.json`. Every setting has a default:

   | Setting | Default | |
   |---------|---------|---|
   | `DatabasePath` | `ditio-sync.db` | SQLite file, created on the first run |
   | `Endpoints` | all of the above | A subset, e.g. `["v1/time-registrations", "v1/project"]` |
   | `OverlapMinutes` | `10` | How far before the previous run's start the next run reads from |
   | `ChunkLimit` | `5000` | Records per page (max 10,000) |
   | `ForceFullSync` | `false` | Re-read everything once |

3. Run it:

   ```bash
   dotnet run -- 13
   ```

   The first run is a full sync and takes as long as your data is big. Later runs typically take seconds.

## Schedule it

Publish once, then run the published build on a schedule from the folder that holds `appsettings.json`:

```bash
dotnet publish -c Release -o /opt/ditio-sync
cp appsettings.json /opt/ditio-sync/
```

**Linux / macOS (cron), every 30 minutes.** `flock` stops two runs from overlapping:

```cron
*/30 * * * * cd /opt/ditio-sync && flock -n /tmp/ditio-sync.lock dotnet Ditio.Samples.dll 13 >> sync.log 2>&1
```

**Windows (Task Scheduler)**, every 30 minutes:

```powershell
schtasks /Create /TN "Ditio sync" /SC MINUTE /MO 30 `
  /TR "cmd /c cd /d C:\ditio-sync && dotnet Ditio.Samples.dll 13 >> sync.log 2>&1"
```

Task Scheduler won't start a second copy while one is still running (its default, *Do not start a new instance*).

Every 15–60 minutes is plenty. Ditio data is not real-time, and a sync more often than every few minutes only adds load.

## Point Power BI at it

**SQLite file (one person, one machine).**

1. Install a SQLite ODBC driver, and create an ODBC data source (DSN) that points at the `.db` file.
2. In Power BI Desktop, choose **Get data → ODBC**, pick the DSN, and load the `vw_*` views.
3. Relate the registrations to the dimensions on their ids, for example `vw_time_registrations.projectId` → `vw_projects.id`, `taskId` → `vw_work_orders.id`, `resourceId` → `vw_resources.id`.

Power BI only reads the local file, so a refresh is fast and never touches Ditio. For scheduled refresh in the Power BI service, the machine with the file needs an on-premises data gateway.

**SQL Server, Azure SQL or PostgreSQL (recommended for shared use).** It's the same pattern with a server database: Power BI has native connectors, many people can read it, and you get proper backups. The sample keeps the SQL in one place ([`SyncStore.cs`](SyncStore.cs)) and uses `DbConnection` throughout. To switch, you change three things:

- the connection: `SqlConnection` (Microsoft.Data.SqlClient) or `NpgsqlConnection` (Npgsql) instead of `SqliteConnection`;
- the upserts: `INSERT … ON CONFLICT` works as it is on PostgreSQL; SQL Server needs `MERGE`;
- the views: `json_extract(json, '$.qty')` becomes `JSON_VALUE(json, '$.qty')` on SQL Server, or `json->>'qty'` on PostgreSQL (with a `jsonb` column).

If you'd rather not run the job yourself, the [Power BI template](../power-bi/README.md) reads the API directly. It is simpler to set up, but it re-reads the whole period on every refresh.

## curl

```bash
IDENTITY=https://identity.ditio.app                  # test: https://identity.ditio.dev
REPORTING_URL=https://core-api.ditio.app/reporting   # test: https://core-api.ditio.dev/reporting
TOKEN=$(curl -s -X POST $IDENTITY/connect/token \
  -d "grant_type=client_credentials" -d "client_id=YOUR_CLIENT_ID" -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "scope=reportingapiv1" | jq -r '.access_token')
```

A function that reads every page of one endpoint. Give it a `ModifiedSince` for an incremental sync, or leave it empty for a full sync:

```bash
# usage: sync_pages <endpoint> [ModifiedSince]
sync_pages() {
  local endpoint=$1 modified_since=$2 token="" page=1
  while :; do
    local url="$REPORTING_URL/$endpoint?ChunkLimit=5000"
    [ -n "$modified_since" ] && url="$url&ModifiedSince=$modified_since"   # on EVERY page
    [ -n "$token" ] && url="$url&ContinuationToken=$(jq -rn --arg t "$token" '$t|@uri')"

    curl -sf "$url" -H "Authorization: Bearer $TOKEN" > "page-$page.json" || return 1
    jq '{syncTypeName, recordCount, deletedRecordCount}' "page-$page.json"

    token=$(jq -r '.continuationToken // empty' "page-$page.json")
    [ -z "$token" ] && break
    page=$((page + 1))
  done
}
```

**First run: full sync.** Note the time *before* you start. It becomes your watermark once every page is stored.

```bash
SYNC_STARTED=$(date -u +%Y-%m-%dT%H:%M:%SZ)
sync_pages v1/time-registrations && echo "$SYNC_STARTED" > watermark.txt
```

**Later runs: incremental**, from the previous start minus the overlap:

```bash
SYNC_STARTED=$(date -u +%Y-%m-%dT%H:%M:%SZ)
SINCE=$(date -u -d "@$(( $(date -u -d "$(cat watermark.txt)" +%s) - 600 ))" +%Y-%m-%dT%H:%M:%SZ)   # GNU date; macOS: gdate
sync_pages v1/time-registrations "$SINCE" && echo "$SYNC_STARTED" > watermark.txt
```

An incremental page looks like this. The second record is a deletion:

```json
{
  "data": [
    { "id": "…", "workDate": "2026-09-28T00:00:00Z", "qty": 7.5, "approved": true,
      "modifiedDateTime": "2026-10-01T06:02:11Z", "isDeleted": false, "…": "…" },
    { "id": "…", "isDeleted": true, "deletedDateTime": "2026-10-01T06:04:40Z",
      "modifiedDateTime": "2026-10-01T06:04:40Z", "…": "…" }
  ],
  "continuationToken": "…",
  "syncTypeName": "incremental",
  "recordCount": 2,
  "deletedRecordCount": 1
}
```

Split a page into upserts and deletions:

```bash
jq -c '.data[] | select(.isDeleted | not)' page-1.json   # upsert by id
jq -r '.data[] | select(.isDeleted) | .id'  page-1.json   # mark deleted
```
