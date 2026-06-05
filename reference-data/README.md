# 11 — Reference data (lookups)

Read-only reference data you need when creating other entities. Core API, scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication/README.md).

## Machine types

Each has a `typeId` used when creating machines ([`../machines`](../machines/README.md)).

```bash
curl $BASE_URL/api/MachineType -H "Authorization: Bearer $TOKEN"
```

## Alert (notification) types

```bash
curl $BASE_URL/api/ProjNotificationTypeSetup -H "Authorization: Bearer $TOKEN"
```

## Payroll & absence types

Payroll types and absence types are configured in Ditio and referenced by **id** in the payroll export filters (`payrollTypeIds` / `absenceTypeIds` — see [`../data-extraction`](../data-extraction/README.md)). See Swagger for the lookup endpoints.

**C#:** [`ReferenceDataExample.cs`](ReferenceDataExample.cs).
