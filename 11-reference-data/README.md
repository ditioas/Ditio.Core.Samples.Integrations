# 11 — Reference data (lookups)

Read-only reference data you need when creating other entities. Core API, scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` — see [`../01-authentication`](../01-authentication/README.md).

## Machine types

Each has a `typeId` used when creating machines ([`../07-machines`](../07-machines/README.md)).

```bash
curl $BASE_URL/api/MachineType -H "Authorization: Bearer $TOKEN"
```

## Alert (notification) types

```bash
curl $BASE_URL/api/ProjNotificationTypeSetup -H "Authorization: Bearer $TOKEN"
```

## Payroll & absence types

Payroll types and absence types are configured in Ditio and referenced by **id** in the payroll export filters (`payrollTypeIds` / `absenceTypeIds` — see [`../09-payroll-export`](../09-payroll-export/README.md)). See Swagger for the lookup endpoints.

**C#:** [`ReferenceDataExample.cs`](ReferenceDataExample.cs).
