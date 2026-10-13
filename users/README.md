# 05 — Users (v4)

`api/v4/integration/users` · scope `ditioapiv3`. Manage employee accounts; matched by `employeeNumber`. Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

> **New integrations should prefer the v5 Employees API** ([`../employees-v5`](../employees-v5)). v4 remains available but gets no new features.

## Authorization

The rules below describe the Core API contract introduced by
[the administrator and company-scope change](https://github.com/ditioas/timestyr/pull/5876).

Every v4 Users endpoint requires an Administrator or a Ditio SysOp. API clients use
their resolved company administrator or emulated user; other signed-in users receive `403`.
`POST /scim` is reserved for Ditio's SysOp SCIM service.

Except for SysOps, requests can act on the caller's own company or a company in a
structure that the caller's company heads. A subsidiary cannot reach its parent or
siblings. API clients that do not emulate a user can send `x-company-id` to act
for an allowed company; the selected company determines their reach. Clients that
emulate a user keep that user's company and permissions instead.

For callers without SysOp access:

- Company-selection checks reject an out-of-scope `companyId` or `projectCompanyId`
  with `403`. PUT checks the identity and resolved profile first, so it can return
  `404` before reaching the company check.
- A missing or out-of-scope company profile ID or identity ID receives `404`.
- PUT and PATCH check the stored profile and any company or profile destination
  changed by the request body. An unchanged linked subcontractor company can remain.
- Employee-number lookup searches the caller's company structure, then checks the
  selected profile's company. An out-of-scope selected profile receives `404`; a
  number with no matching profile receives `400`.
- An unfiltered list includes the main company and its subsidiaries for a caller
  acting as the structure's main company; other callers get only their own company.
- DELETE targets the identity's profiles in the caller's structure's main company
  and project companies. It receives `403` before deletion if any targeted profile
  is outside the caller's reach; it does not skip those profiles and partially delete
  only the permitted ones.

Phone-number changes update the shared sign-in identity and every profile that
uses it, including profiles in other companies.

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

**C#:** [`UsersExample.cs`](UsersExample.cs).
