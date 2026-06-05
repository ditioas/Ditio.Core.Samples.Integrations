# 02 — Projects

`api/v4/integration/projects` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` first — see [`../01-authentication`](../01-authentication).

## Create

```bash
curl -X POST $BASE_URL/api/v4/integration/projects \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "companyId": "YOUR_COMPANY_ID", "projectNumber": "P-1001", "name": "My project", "active": true }'
```

`companyId`, `projectNumber` are required. The call is idempotent on `projectNumber` (returns the existing project if it already exists).

## Look up

```bash
curl $BASE_URL/api/v4/integration/projects                              -H "Authorization: Bearer $TOKEN"   # all
curl $BASE_URL/api/v4/integration/projects/by-project-number/P-1001     -H "Authorization: Bearer $TOKEN"   # by number
curl $BASE_URL/api/v4/integration/projects/{id}                         -H "Authorization: Bearer $TOKEN"   # by Ditio id
```

## Update / delete

```bash
# Partial update (only the fields you send)
curl -X PATCH $BASE_URL/api/v4/integration/projects/{id} \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "name": "Renamed project" }'

# DELETE is permanent and cascades
curl -X DELETE $BASE_URL/api/v4/integration/projects/{id} -H "Authorization: Bearer $TOKEN"
```

> **Prefer PATCH over PUT.** `PUT /projects/{id}` *replaces the whole project* — any field you omit is wiped. Use `PATCH` (dynamic partial update) for syncs unless you really mean to overwrite everything.

**C#:** [`ProjectsExample.cs`](ProjectsExample.cs). Full field reference: Swagger.
