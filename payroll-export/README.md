# 18 — Payroll export

Pull payroll data out of Ditio for import into an accounting or payroll system. Full guide with curl, Python
and C# examples: [Payroll Export](https://docs.ditio.app/guides/payroll-export/). Runnable C# example:
[`PayrollExportExample.cs`](PayrollExportExample.cs).

| | Endpoint | Host · scope | Use it for |
|---|---|---|---|
| **Recommended** | `GET v1/payroll-lines` | Reporting · `reportingapiv1` | Pulling payroll lines into your system: paginated, full and incremental sync, lines ready to map to your payroll codes |
| | `GET v1/payroll-lines-extended` | Reporting · `reportingapiv1` | The same, plus approval, verification and lock status and the full breakdown (normal time, overtime, absence, time bank) |
| Alternative | `GET api/payroll-export/file` | Integration · `ditioapiv3` | The payroll file in the format configured in your company's payroll setup |
| Alternative | `GET api/payroll-export/` | Integration · `ditioapiv3` | The older JSON export: not paginated, date range capped at 45 days |

Both hosts need an API client whose user is an **administrator** (otherwise `403`). See
[`../authentication`](../authentication) for hosts, scopes and tokens, and
[`../data-extraction`](../data-extraction/README.md) for the shared paging model.

## Locking

**Reading payroll data through any of these endpoints does not lock it.** A payroll period is locked by a
payroll administrator in Ditio Web; there is no documented API operation that locks payroll data or marks
it as already fetched.

- While a period is not locked, `ExportFilter=Approved` returns the same lines on every call. Make your
  import idempotent: upsert on the line `id`, and use incremental sync (`ModifiedSince`) with the
  `isDeleted` tombstones.
- Once a period is locked, its lines leave `Approved`. Read them with `ExportFilter=Locked` (locked only)
  or `All`.
- If Ditio Web does the locking, run your import first, then lock the period.

## Payroll lines (recommended)

```bash
# Full sync of a period: approved, not yet locked data.
curl -G "$REPORTING_URL/v1/payroll-lines" \
  -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "ExportFilter=Approved" \
  --data-urlencode "FromDateTime=2026-09-01" \
  --data-urlencode "ToDateTime=2026-09-30"
```

Get the token with `scope=reportingapiv1`. Follow `continuationToken` until it is `null`:

```bash
curl -G "$REPORTING_URL/v1/payroll-lines" -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "ExportFilter=Approved" \
  --data-urlencode "FromDateTime=2026-09-01" --data-urlencode "ToDateTime=2026-09-30" \
  --data-urlencode "ContinuationToken=THE_TOKEN"
```

Each row is a payroll summary (one employee, one day) with `payrollValuesAsExportLines`: the lines
(`typeName`, `qty`, `rate`, `projectNumber`, `taskNumber`) to map to your payroll codes.

| Parameter | Description |
|-----------|-------------|
| `ExportFilter` | `Approved` = approved and not yet locked, `Unlocked` = not yet locked (approved or not), `Locked` = locked only, `All` = everything. **Defaults to `All`, which includes unapproved data — send `Approved` for a payroll run.** |
| `UserPayrollTypeFilter` | `PaidByHour` (default), `FixedPay` or `AllUsers`. Fixed-pay employees are missing unless you send `AllUsers`. |
| `ExportAllPayrollTypes` | Include payroll types marked "do not export" in Ditio's setup. Default `false`. |
| `IncludeAllAbsence` | Include absence transactions regardless of absence type. Default `false`. |
| `FromDateTime`, `ToDateTime` | Work-date window of a full sync. No 45-day limit; chunk a large backfill month by month. |
| `ModifiedSince`, `ModifiedBefore` | Incremental sync. `ModifiedSince` must be within the last 45 days. |
| `ProjectId`, `CompanyId` | Only data on this project / registered in this company. |
| `ChunkLimit`, `ContinuationToken` | Page size (default 2500, 1–10000) and the paging cursor. |

There is no filter by user, payroll type or absence type: pull the data and filter on your side.

**Incremental sync.** Send `ModifiedSince` (the instant of your last successful sync, at most 45 days back)
instead of a date window. Lines that were deleted come back with `isDeleted: true`: remove them on your
side. Store the new watermark only after every page has been processed.

```bash
curl -G "$REPORTING_URL/v1/payroll-lines" -H "Authorization: Bearer $TOKEN" \
  --data-urlencode "ExportFilter=Approved" \
  --data-urlencode "ModifiedSince=2026-09-14T00:00:00Z"
```

## Payroll export (Integration API)

```bash
curl -X GET "$BASE_URL/api/payroll-export/?fromWorkDate=2026-09-01&toWorkDate=2026-09-28&dataFilter=0" \
  -H "Authorization: Bearer $TOKEN"
```

Get the token with `scope=ditioapiv3`. `GET api/payroll-export/readonly` is an alias for the same JSON
export.

| Parameter | Type | Required | Description |
|-----------|------|----------|--------------|
| `fromWorkDate` | string | No | Start date, e.g. `2026-09-01`. Defaults to 14 days before now. |
| `toWorkDate` | string | No | End date, e.g. `2026-09-28`. Defaults to now. |
| `modifiedSinceDate` | string | No | Return data modified since this date. Oldest supported value is one year back. |
| `dataFilter` | int | No | `0` = only approved, not yet locked (default), `5` = only locked, `10` = all data |
| `userIds` | List\<string\> | No | Filter by specific Ditio user IDs. Omitted = all users |
| `userPayrollTypeFilter` | string | No | `paid-by-hour` (default), `fixed-pay`, or `all-users` |
| `companyIds` / `projectIds` / `payrollTypeIds` / `absenceTypeIds` | List\<string\> | No | Filter by id |

See Swagger for the full parameter list (there are more filters than shown here).

> **The date range is capped at 45 days, and a longer range is not rejected.** Ditio silently shortens it
> to 45 days from `fromWorkDate` and returns only that part, with `200 OK`. Split a longer period into
> chunks of 45 days or less (a calendar month always fits).

### Payroll file

```
GET /api/payroll-export/file
```

Returns the payroll export file in the format configured in your company's payroll setup (to request a new
format, send a sample file and the accounting system's import documentation to support@ditio.no).

```bash
curl "$BASE_URL/api/payroll-export/file?fromWorkDate=2026-09-01&toWorkDate=2026-09-30&dataExportType=10" \
  -H "Authorization: Bearer $TOKEN" -o payroll.txt
```

`dataExportType`: `5` = preview, `6` = journal, Excel (default), `7` = extended journal, Excel, `10` = payroll,
`11` = secondary payroll file (only for companies set up for it), `20` = absence, `30` = cost,
`40` = transaction split. The same `dataFilter`, `userPayrollTypeFilter`, `payrollTypeIds` and
`absenceTypeIds` filters apply, and the 45-day cap above.

In the payroll file (`dataExportType=10`), a line is left out when its export ID is empty or has no letter or
digit, such as `.` or `-`. A period locked in Ditio needs `dataFilter=5`.

### Summary lines

```
GET /api/payroll-export/summary-as-lines   # payroll summaries as export lines
```

See Swagger for parameters — the same filters apply.

## `overtimeLines`

Each summary on `api/payroll-export` and `v1/payroll-lines-extended` carries `overtimeLines`: one entry per
overtime payroll type booked on the day, so an integration does not have to assume the fixed overtime
fields (`overtime50Qty`, `overtime100Qty`) — a company can configure **any number** of overtime types.

Existing fields (`overtime50Qty`, `overtime100Qty`, etc.) are unchanged — `overtimeLines` is additive.

```json
{
  "userName": "Kari Nordmann",
  "transDateTime": "2026-09-14T00:00:00Z",
  "qty": 8.5,
  "overtime50Qty": 1.5,
  "overtime100Qty": 0,
  "overtimeLines": [
    {
      "payrollTypeId": "65f1a2b3c4d5e6f7a8b9c0d1",
      "name": "Overtime 50%",
      "lineType": 20,
      "overtimeType": 0,
      "sorting": 1,
      "qty": 1.5
    },
    {
      "payrollTypeId": "65f1a2b3c4d5e6f7a8b9c0d2",
      "name": "Beredskapsovertid",
      "lineType": 41,
      "overtimeType": 1,
      "sorting": 1,
      "qty": 2
    }
  ]
}
```

| Field | Type | Description |
|-------|------|--------------|
| `payrollTypeId` | string | Ditio payroll type ID |
| `name` | string | Payroll type name |
| `lineType` | int | `20`, `30`, `40` = overtime levels 1/2/3 (Ditio's automatic overtime calculation). `41` = a **manually registered** overtime type — a company can have any number of these, freely named. |
| `overtimeType` | int | `0` = **Overtime** — deducted from ordinary hours (already netted out of `qty`/`standardQty` on the summary). `1` = **OvertimeAddition** — paid **on top of** ordinary hours, never deducted. Two companies can configure the same `lineType` with different `overtimeType` values, so always read `overtimeType` per line rather than assuming it from `lineType`. |
| `sorting` | number | The type's sorting in Ditio's payroll setup |
| `qty` | number | Hours on the day, rounded to 2 decimals |

Entries are ordered by `lineType`, then `sorting`, then `name`.
