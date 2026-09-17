# 02 — Projects

`api/v4/integration/projects` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` first — see [`../authentication`](../authentication).

## Create

```bash
curl -X POST $BASE_URL/api/v4/integration/projects \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "companyId": "YOUR_COMPANY_ID", "projectNumber": "P-1001", "name": "My project", "active": true }'
```

`companyId`, `projectNumber` are required. The call is idempotent on `projectNumber` (returns the existing project if it already exists).

## External project number

`externalProjectNumber` links the project to your system. Free text for most companies.

For some companies it is validated instead against a **configured list of valid external project numbers**. That needs two things to be true — the company has the validation enabled *and* has values configured — so a list alone does not mean writes are checked. Where it does apply the value must match a configured one exactly, the field becomes **required**, and anything else fails with `400` and `Extern prosjektnummer <value> er ikke en gyldig. Velg fra listen`.

The list is maintained inside Ditio — there is no public endpoint for it. Ask your Ditio contact which values are configured, or whether the company uses a list at all. The value you set here also determines which sub project numbers a work order in this project must use — see [`../work-orders`](../work-orders).

## Look up

```bash
curl $BASE_URL/api/v4/integration/projects                              -H "Authorization: Bearer $TOKEN"   # all
curl $BASE_URL/api/v4/integration/projects/by-project-number/P-1001     -H "Authorization: Bearer $TOKEN"   # by number
curl $BASE_URL/api/v4/integration/projects/{id}                         -H "Authorization: Bearer $TOKEN"   # by Ditio id
```

Looking up a missing project ID and looking up a project that is not available to the authenticated
company both return the same `404 Not Found` response. Callers should treat either response as an
unavailable project without trying to distinguish the reason.

## Update / delete

```bash
# Partial update (only the fields you send)
curl -X PATCH $BASE_URL/api/v4/integration/projects/{id} \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "name": "Renamed project" }'

# DELETE is permanent and cascades
curl -X DELETE $BASE_URL/api/v4/integration/projects/{id} -H "Authorization: Bearer $TOKEN"
```

PATCH requires a JSON object and preserves fields you omit. If the project is missing or unavailable
to the authenticated company, it returns the same `404 Not Found` response and does not create or
change a project.

> **Prefer PATCH over PUT.** `PUT /projects/{id}` *replaces the whole project* — any field you omit is wiped. Use `PATCH` for syncs unless you really mean to overwrite everything.

**C#:** [`ProjectsExample.cs`](ProjectsExample.cs). Full field reference: Swagger.
