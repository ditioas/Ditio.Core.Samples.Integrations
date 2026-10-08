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
mappings receive the same generic `404` in Enforce mode. Employee-number target lookup failures
use that same response. Added authorization-fact failures also receive generic `404` in Enforce;
API clients in Shadow keep the legacy write when only the added observation fails.

Profile and employment saves also synchronize profiles linked through an Employee employment.
Every synchronization destination must be in scope before writing, or the request receives `404`.
This covers enable/disable, SCIM deactivation, creation that updates an existing profile,
get-or-create role changes, and every existing destination or parent profile a company move or
employment restart saves. Sharing an identity without a linked employment does not trigger this
synchronization.

Employment changes can also queue transaction and trip-log maintenance. Enforced requests limit
that maintenance to records owned by your company or its descendants; unrelated records remain
unchanged while the authorized employment update completes. The same scope covers derived feed
posts, price records and payroll-summary recalculation. Transport splitting leaves the original
transaction intact when the split requires changing a record outside your scope.

Employee-number lookup considers only profiles in scope; an unknown number or a match only outside
scope receives `404`. Requests that also create or replace a parent profile or employment require
access to that parent before writing. This includes requested parent subcontractor profiles and
project employment provisioning or restarts. Tags and supervisors require access to their metadata
owner. Removing an inherited project tag requires access to the parent that owns its assignment;
otherwise the entire request receives `403` before any changes. Ordinary edits to an owned linked subcontractor profile can retain its connected company.

Creating a profile may borrow an avatar from another profile with the same phone number. A company
move that creates a destination profile also carries the source profile's avatar. If either operation
rewrites an avatar file reference, including the default avatar, its persisted company must be in scope;
otherwise the whole request receives `403` before any changes. Reusing an unchanged avatar or moving
to an existing identity profile leaves that file reference alone.

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
