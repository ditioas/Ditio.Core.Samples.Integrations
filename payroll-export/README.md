# 18 — Payroll export

Pull approved payroll data out of Ditio for import into an accounting or payroll system. This is the
**recommended** way to move payroll data out of Ditio; the alternative is a flat-file export configured
in Ditio Web (contact support@ditio.no to set one up).

`api/payroll-export/` · scope `ditioapiv3`, same host and token as the write endpoints — see
[`../authentication`](../authentication). Runnable C# example: [`PayrollExportExample.cs`](PayrollExportExample.cs).

## Get payroll summaries

```bash
curl -X GET "$BASE_URL/api/payroll-export/?fromWorkDate=2026-09-01&toWorkDate=2026-09-28&dataFilter=0" \
  -H "Authorization: Bearer $TOKEN"
```

`GET api/payroll-export/readonly` is an alias for the same JSON export.

| Parameter | Type | Required | Description |
|-----------|------|----------|--------------|
| `fromWorkDate` | string | No | Start date, e.g. `2026-09-01`. Defaults to 14 days before now. Date ranges are limited to 45 days. |
| `toWorkDate` | string | No | End date, e.g. `2026-09-28`. Defaults to now. |
| `modifiedSinceDate` | string | No | Return data modified since this date. Oldest supported value is one year back. |
| `dataFilter` | int | No | `0` = only approved (default), `5` = only locked, `10` = all data |
| `userIds` | List\<string\> | No | Filter by specific Ditio user IDs. Omitted = all users |
| `userPayrollTypeFilter` | string | No | `paid-by-hour` (default), `fixed-pay`, or `all-users` |
| `companyIds` / `projectIds` / `payrollTypeIds` / `absenceTypeIds` | List\<string\> | No | Filter by id |

See Swagger for the full parameter list (there are more filters than shown here).

## `overtimeLines`

Each summary carries `overtimeLines`: one entry per overtime payroll type booked on the day, so an
integration does not have to assume the three fixed overtime fields (`overtime50Qty`, `overtime100Qty`)
— a company can configure **any number** of overtime types. The same field is returned by the Reporting
data-extraction endpoint [`v1/payroll-lines-extended`](../data-extraction/README.md#payroll-lines-and-payroll-lines-extended).

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

## Summary lines / file export

```
GET /api/payroll-export/summary-as-lines   # payroll summaries as export lines
GET /api/payroll-export/file               # a payroll export file in the format configured in Ditio
```

See Swagger for parameters — `voucherNumber`, `dataExportType`, and the same filters as above apply.
