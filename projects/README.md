# 02 — Projects

`api/v4/integration/projects` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` first — see [`../authentication`](../authentication).

Writes require an established relationship to the stored project (resolved owner/shared company,
assigned user, or existing parent-company management checked against SQL). Existing endpoint
permissions and read-only restrictions still apply. Knowing an ID or submitting a different
owner/sharing list does not grant access: unrelated updates return `404 Not Found` before write
effects. Create/upsert destination companies must be the resolved company or managed descendants;
existing upsert targets are checked separately. PATCH remains limited to the resolved company's
projects. Keep the intended company context on requests; do not retry a denied update as a create.

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

You can read and manage that list yourself — see [`../reference-data`](../reference-data/README.md) and the [Project External References](https://docs.ditio.app/api-reference/reference-data/external-references/) reference. List the `ExternalProjectNumber` type to see what is accepted, and add the value if it is missing. The value you set here also determines which sub project numbers a work order in this project must use — see [`../work-orders`](../work-orders).

## Look up

```bash
curl $BASE_URL/api/v4/integration/projects                              -H "Authorization: Bearer $TOKEN"   # all
curl $BASE_URL/api/v4/integration/projects/by-project-number/P-1001     -H "Authorization: Bearer $TOKEN"   # by number
curl $BASE_URL/api/v4/integration/projects/{id}                         -H "Authorization: Bearer $TOKEN"   # by Ditio id
```

Looking up a missing project ID and looking up a project that is not available to the authenticated
company both return the same `404 Not Found` response. Callers should treat either response as an
unavailable project without trying to distinguish the reason.

## Personnel and roles

Who is on a project's personnel list, and the role each person has there, as set up in Period
Planning:

```bash
curl $BASE_URL/api/v4/integration/projects/{id}/personnel                -H "Authorization: Bearer $TOKEN"   # one project
curl $BASE_URL/api/v4/integration/projects/personnel                     -H "Authorization: Bearer $TOKEN"   # all active projects
curl "$BASE_URL/api/v4/integration/projects/personnel?includeInactive=true" -H "Authorization: Bearer $TOKEN" # incl. inactive
```

Same company convention as the data-extraction endpoints: a parent company (where an API client
usually sits) gets the projects of its project companies and subsidiaries; any other company only
its own. Add `?excludeDataFromSubsidiaries=true` to leave subsidiaries out, or `?companyId=<id>` for
one company. Each project carries `projectCompanyId`, the company that owns it. The company-wide form returns a JSON array of these project objects, ordered by
project number; within a project, people are ordered by role, then name.

Each person has `userId` (the same value as `identityId` in the [Users API](../users)), `name`,
`companyId` (their employer as recorded on their profile in the project's company: the
subcontractor for subcontractor personnel), `role`, `roleName` and `isMainRole`. Like Period
Planning, only people with an active or upcoming employment in the project's company are listed.

| `role` | `roleName` (as Ditio Web shows it) |
|---|---|
| `ProjectManager` | Anleggsleder (the *Project manager* role on a user's Projects tab) |
| `Hse` | Hms |
| `Foreman` | Formann |
| `Bas` | Bas |
| `User` | Bruker (on the personnel list without a role) |

A project can have several people in each role. `isMainRole` is `true` only for the project
manager in the project's `projectLeaderId` and the foreman in its `mainForemanId`. A
missing project ID and one you cannot reach return the same `404 Not Found`.

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

> **Prefer PATCH for an update bound to the route ID.** PUT keeps the legacy upsert matching rules
> (submitted ID or integration keys); the route ID alone does not select its target. Both map
> provided fields. The write-scope check applies to the stored project actually selected.

**C#:** [`ProjectsExample.cs`](ProjectsExample.cs). Full field reference: Swagger.
