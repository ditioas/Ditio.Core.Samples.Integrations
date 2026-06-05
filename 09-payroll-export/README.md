# 09 — Payroll export

Read payroll data out of Ditio. `api/payroll-export` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` — see [`../01-authentication`](../01-authentication).

## JSON export (recommended)

```bash
curl -X GET "$BASE_URL/api/payroll-export/?fromWorkDate=2025-01-01&toWorkDate=2025-01-31&dataFilter=0" \
  -H "Authorization: Bearer $TOKEN"
```

The response contains everything needed to convert into any accounting-system format. `GET /api/payroll-export/readonly` is an alias.

### Key parameters

| Parameter | Description |
|-----------|-------------|
| `fromWorkDate` / `toWorkDate` | Work-date range (e.g. `2025-01-01`). Defaults to the last 14 days. **Max range 45 days.** |
| `modifiedSinceDate` | Only data modified since this date (≤ 1 year back) — for delta sync |
| `dataFilter` | `0` = only approved (default), `5` = only locked, `10` = all |
| `userPayrollTypeFilter` | `paid-by-hour` (default), `fixed-pay`, `all-users` |
| `userIds` / `projectIds` / `companyIds` / `payrollTypeIds` / `absenceTypeIds` | Optional filters |

## Other endpoints

```bash
curl "$BASE_URL/api/payroll-export/summary-as-lines?fromWorkDate=2025-01-01&toWorkDate=2025-01-31" -H "Authorization: Bearer $TOKEN"   # summary lines
curl "$BASE_URL/api/payroll-export/file?dataExportType=6" -H "Authorization: Bearer $TOKEN"                                            # formatted file
```

To add support for a new accounting-system file format, contact support@ditio.no with a sample file and field descriptions.

**C#:** [`PayrollExportExample.cs`](PayrollExportExample.cs).
