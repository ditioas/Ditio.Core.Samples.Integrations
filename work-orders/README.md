# 03 — Work orders (tasks)

`api/v4/integration/tasks` · scope `ditioapiv3`. A work order belongs to a project — create/find the project first ([`../projects`](../projects)). Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

## Create

```bash
curl -X POST $BASE_URL/api/v4/integration/tasks \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "companyId": "YOUR_COMPANY_ID", "projectId": "PROJECT_ID", "externalId": "WO-100", "name": "Foundation work", "active": true }'
```

`companyId`, `projectId`, `externalId` are required.

## Look up

```bash
curl $BASE_URL/api/v4/integration/tasks/project/{projectId}                  -H "Authorization: Bearer $TOKEN"   # all in a project (by id)
curl $BASE_URL/api/v4/integration/tasks/by-project-number/{projectNumber}    -H "Authorization: Bearer $TOKEN"   # all in a project (by number)
curl $BASE_URL/api/v4/integration/tasks/{id}                                 -H "Authorization: Bearer $TOKEN"   # by Ditio id
```

There are also lookups by task number and external ids — see Swagger.

## Update / delete

```bash
curl -X PATCH  $BASE_URL/api/v4/integration/tasks/{id} -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "name": "Foundation (phase 2)" }'
curl -X DELETE $BASE_URL/api/v4/integration/tasks/{id} -H "Authorization: Bearer $TOKEN"
```

A work order can't be deleted while it has time registrations; deactivate (`active:false`) instead.

**C#:** [`WorkOrdersExample.cs`](WorkOrdersExample.cs).
