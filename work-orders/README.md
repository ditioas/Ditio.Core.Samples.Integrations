# 03 — Work orders (tasks)

`api/v4/integration/tasks` · scope `ditioapiv3`. A work order belongs to a project — create/find the project first ([`../projects`](../projects)). Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

## Create

```bash
curl -X POST $BASE_URL/api/v4/integration/tasks \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "companyId": "YOUR_COMPANY_ID", "projectId": "PROJECT_ID", "externalId": "WO-100", "name": "Foundation work", "active": true }'
```

`companyId`, `projectId`, `externalId` are required.

### Optional settings & template work orders

A handful of settings are **optional** — `safeJobAnalysisApprovalRequired`, `measureUnitQty`, `unitId`, `costPrice`, `price`, `fixedResourcePrice`. Omitting them means "not provided" (they are **not** forced to `false`/`0`):

- If the project has a **template work order** (one work order marked as the template in the Ditio backoffice), an omitted setting is copied from that template. Values you send always win — "payload wins, template fills the gaps".
- A work order you create or update through the API is never itself the template; the template flag is managed only in the backoffice.

```bash
# Provide some settings explicitly; omit the rest to inherit from the project's template (if any).
curl -X POST $BASE_URL/api/v4/integration/tasks \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "companyId": "YOUR_COMPANY_ID", "projectId": "PROJECT_ID", "externalId": "WO-101", "name": "Earthworks", "active": true, "safeJobAnalysisApprovalRequired": true, "costPrice": 1200.0 }'
```

## External project / sub project numbers

`externalProjectNumber` and `externalSubProjectNumber` link the work order to your system. For most companies they are free text and can be omitted.

Some companies have **configured lists of valid values** held in Ditio. Where a list exists, the field stops being free text:

- the value must match a configured value exactly;
- it becomes **required** — omitting it is rejected too;
- anything else fails with `400` and `Delprosjektnummer <value> er ikke en gyldig. Velg fra listen`.

Sub project numbers are scoped **per project**, not per company: the valid set is the one configured for this work order's `externalProjectNumber`, so changing that changes which sub project numbers are accepted.

```bash
curl -X POST $BASE_URL/api/v4/integration/tasks \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "companyId": "YOUR_COMPANY_ID", "projectId": "PROJECT_ID", "externalId": "WO-102", "name": "Rep/Ved", "active": true, "externalProjectNumber": "259086", "externalSubProjectNumber": "259086-01" }'
```

The lists are maintained inside Ditio — there is no public endpoint for them. If you get the error above, ask your Ditio contact which values are configured for the project, or whether the company uses lists at all.

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

`PUT` is a full replace: omitted fields reset to their defaults (and the template is not consulted on update — template fill-in is create-only). Use `PATCH` to change only some fields without resetting the rest.

**C#:** [`WorkOrdersExample.cs`](WorkOrdersExample.cs).
