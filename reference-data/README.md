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

## Project external references

The lists of values Ditio validates project and work order writes against — external project numbers, sub project numbers, process codes, WBS codes and `ExternalDim02`. Unlike the rest of this page these support writes as well as reads.

```bash
# Valid external project numbers for the company
curl $BASE_URL/api/project-external-reference/type/ExternalProjectNumber -H "Authorization: Bearer $TOKEN"

# Valid sub project numbers for one project.
# The path segment is the project's externalProjectNumber, NOT its Ditio projectNumber.
curl $BASE_URL/api/project-external-reference/type/ExternalSubProjectNumber/project/40001 -H "Authorization: Bearer $TOKEN"

# Add a value. projNumber is required for ExternalSubProjectNumber and ProcessCode.
curl -X POST $BASE_URL/api/project-external-reference \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "externalRefType": "ExternalProjectNumber", "externalRefValue": "40001", "name": "Example project" }'

# Retire a value without breaking history
curl -X PUT $BASE_URL/api/project-external-reference/disable/{id} -H "Authorization: Bearer $TOKEN"
```

Read these before writing `externalProjectNumber` on a project ([`../projects`](../projects/README.md)): where a company has values configured, anything outside the list is rejected with `400`. Full reference: [Project External References](https://docs.ditio.app/api-reference/reference-data/external-references/).

## Payroll & absence types

Payroll types and absence types are configured in Ditio and referenced by **id** in the payroll export filters (`payrollTypeIds` / `absenceTypeIds` — see [`../data-extraction`](../data-extraction/README.md)). See Swagger for the lookup endpoints.

**C#:** [`ReferenceDataExample.cs`](ReferenceDataExample.cs).
