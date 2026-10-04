# 05 — Users (v4)

`api/v4/integration/users` · scope `ditioapiv3`. Manage employee accounts; matched by `employeeNumber`. Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

> **New integrations should prefer the v5 Employees API** ([`../employees-v5`](../employees-v5)). v4 remains available but gets no new features.

## Authorization

Every v4 Users endpoint requires an Administrator, including an API client acting as its company
administrator or emulating an administrator. Other signed-in users receive `403`.

Requests can act on the caller's company and its subsidiaries and project companies. Parent,
sibling, and unrelated companies are outside that scope. A foreign `companyId` receives `403`;
a foreign `companyProfileId` receives `404`, matching an unknown profile. This applies to lookup,
disable/enable, and profile or company changes submitted through PUT, PATCH, or SCIM.

The unfiltered list returns only profiles in that scope. Deleting an `identityId` removes only its
profiles in scope, preserving profiles held elsewhere. SCIM checks its resolved organization
company and matched profile before writing; root-owned worktime arrangements require access to
that root.

A phone-number change updates the shared sign-in identity and every company profile it holds.
Every affected profile must be in scope; otherwise the entire update receives `404` before any
changes. The bound integration and SCIM system clients retain cross-company provisioning access.

By-profile-id reads use the exact persisted profile ID. Missing, foreign, or unavailable profile
mappings receive the same generic `404` in Enforce mode.

Profile and employment saves also synchronize profiles linked through an Employee employment.
Every synchronization destination must be in scope before writing, or the request receives `404`.
This covers enable/disable, SCIM deactivation, creation that updates an existing profile,
get-or-create role changes, and every existing destination or parent profile a company move or
employment restart saves. Sharing an identity without a linked employment does not trigger this
synchronization.

Employee-number lookup considers only profiles in scope; an unknown number or a match only outside
scope receives `404`. Requests that also create or replace a parent profile or employment require
access to that parent before writing. This includes requested parent subcontractor profiles and
project employment provisioning or restarts. Tags and supervisors require access to their metadata
owner. Ordinary edits to an owned linked subcontractor profile can retain its connected company.

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
