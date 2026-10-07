# 05 — Users (v4)

`api/v4/integration/users` · scope `ditioapiv3`. Manage employee accounts; matched by `employeeNumber`. Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

> **New integrations should prefer the v5 Employees API** ([`../employees-v5`](../employees-v5)). v4 remains available but gets no new features.

## Create

```bash
curl -X POST $BASE_URL/api/v4/integration/users \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{
    "companyId": "YOUR_COMPANY_ID",
    "employeeNumber": "1042",
    "firstName": "Ola", "lastName": "Nordmann",
    "mobileWork": "+4798765432",
    "birthDate": "1990-05-15",
    "employmentStartDate": "2025-03-01",
    "workTitle": "Machine Operator", "department": "Construction"
  }'
```

Required: `companyId`, `employeeNumber`, `firstName`, `lastName`, `mobileWork`, `birthDate`, `employmentStartDate`. Save `identityId` + `companyProfileId` from the response.

## Look up

```bash
curl $BASE_URL/api/v4/integration/users                                 -H "Authorization: Bearer $TOKEN"   # all
curl "$BASE_URL/api/v4/integration/users?changedSince=2025-03-04T00:00:00Z" -H "Authorization: Bearer $TOKEN"   # delta sync
curl $BASE_URL/api/v4/integration/users/by-employee-number/1042         -H "Authorization: Bearer $TOKEN"
```

## Update / disable

```bash
# PATCH uses companyProfileId in the URL
curl -X PATCH $BASE_URL/api/v4/integration/users/{companyProfileId} -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "workTitle": "Project Manager" }'

# Prefer disabling over deleting when someone leaves
curl -X PATCH $BASE_URL/api/v4/integration/users/disable/{companyProfileId} -H "Authorization: Bearer $TOKEN"
curl -X PATCH $BASE_URL/api/v4/integration/users/enable/{companyProfileId}  -H "Authorization: Bearer $TOKEN"
```

> **Prefer PATCH over PUT.** `PUT /users/{identityId}` replaces the whole user — omitted fields are wiped. Use `PATCH /users/{companyProfileId}` for partial updates. PUT and DELETE use the **identityId**, which must be URL-encoded (`auth0|abc` → `auth0%7Cabc`).

## Delete

```bash
curl -X DELETE "$BASE_URL/api/v4/integration/users/{urlEncodedIdentityId}" -H "Authorization: Bearer $TOKEN"
```

Deletion returns `204 No Content` on success and `404 Not Found` when no matching profile remains. If a loaded profile or employment disappears before the SQL save, deletion returns `409 Conflict`. Reload the user's profiles before deciding whether another delete is needed; do not blindly retry.

Identity and Mongo cleanup for each profile start after its SQL deletion succeeds. Cleanup failures after the SQL save can still require operational reconciliation; deletion is not atomic across stores.

**C#:** [`UsersExample.cs`](UsersExample.cs).
