# Ditio.Core.Samples.Integrations

Example integrations for the **Ditio Core API** — one folder per integration kind, each with a runnable **C#** example and **curl** snippets, plus a **Postman** collection that covers everything.

## Swagger (full API reference)

- **Production:** https://integration.ditio.no/swagger
- **Test:** https://core-api.ditio.dev/core/swagger

## Contents

**Start here**

- [`authentication`](authentication/README.md) — get an OAuth2 token (shared by everything below)

**Send data into Ditio** (create / update)

- [`projects`](projects/README.md) — projects, and who is on each project with their role (Anleggsleder, Hms, Formann, Bas)
- [`work-orders`](work-orders/README.md) — work orders (tasks)
- [`machines`](machines/README.md) — machines & equipment
- [`reference-data`](reference-data/README.md) — machine types, alert types (lookups you need first)
- [`users`](users/README.md) — users (v4)
- [`employees-v5`](employees-v5/README.md) — employees (v5, **recommended**)
- [`certificates`](certificates/README.md) — user certificates, and image/PDF scans of the card attached to them
- [`documents`](documents/README.md) — documents on projects & work orders
- [`absences`](absences/README.md) — push absences granted in your payroll/HR system, in one call, and list what Ditio holds for a period

**Get data out of Ditio** (read / sync)

- [`data-extraction`](data-extraction/README.md) — projects, work orders, checklists, alerts, project transactions, absences, payroll, users, images … (paginated `v1/*`)
- [`data-extraction-sync`](data-extraction-sync/README.md) — keep a local database in sync with small deltas (`ModifiedSince` + `continuationToken`, deletions included), for Power BI or your own reporting
- [`payroll-export`](payroll-export/README.md) — approved payroll data for import into accounting/payroll systems, including per-line `overtimeLines`

**Send data to another system**

- [`crew-list-chkbox`](crew-list-chkbox/README.md) — poll the crew list (*mannskapsliste*) and push check-in/check-out passages to Infotech's ChkBox / byggekortleser HSE register

**Other**

- [`postman`](postman/README.md) — Postman collection + Production/Test environments
- [`power-bi`](power-bi/README.md) — Power BI template built on the data-extraction endpoints

## Environments

| | Production | Test |
|---|---|---|
| **Identity** | `https://identity.ditio.app` | `https://identity.ditio.dev` |
| **Integration API** (`api/v4`, `api/v5`) | `https://integration.ditio.no` | `https://core-api.ditio.dev/core` |
| **Reporting API** (`v1/*`) | `https://core-api.ditio.app/reporting` | `https://core-api.ditio.dev/reporting` |
| **Scope** — integration / core | `ditioapiv3` | `ditioapiv3` |
| **Scope** — reporting | `reportingapiv1` | `reportingapiv1` |

## Run the C# examples

1. In Ditio Web → **Company Setup → Integration** (Administrator access), create an API client and copy its `client_id` / `client_secret`.
2. Copy `appsettings.example.json` → `appsettings.json` and fill in `ClientId` / `ClientSecret` / `CompanyId`. Defaults are **production**; test values are in the comments. `appsettings.json` is git-ignored.
3. Run:

```bash
dotnet run            # interactive menu
dotnet run -- 2       # run example #2 (projects) directly
```

## curl

Each folder's README has curl snippets. Set these once:

```bash
# Production (test values in comments)
IDENTITY=https://identity.ditio.app      # test: https://identity.ditio.dev
BASE_URL=https://integration.ditio.no    # test: https://core-api.ditio.dev/core

TOKEN=$(curl -s -X POST $IDENTITY/connect/token \
  -d "grant_type=client_credentials" \
  -d "client_id=YOUR_CLIENT_ID" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "scope=ditioapiv3" | jq -r '.access_token')
```

## Updating: prefer PATCH over PUT

**Use `PATCH` for updates, not `PUT`.**

- **`PATCH`** is a partial, dynamic update — send only the fields you want to change. Check the entity guide for explicit `null` handling and business effects. You don't need to fetch the full object first.
- **`PUT`** can replace the entire stored object with the body you send. On full-replace endpoints, **any field you omit is wiped** (cleared or reset to its default). This is a common cause of accidental data loss during syncs.

So unless you deliberately want to replace every field, reach for **PATCH**.

> **Machines and v4 Users are exceptions.** `PUT /api/v4/integration/machines/{id}` and
> `PUT /api/v4/integration/machines/update/array` are preserve-on-omit: a field left out of the body
> keeps its stored value. See [`machines/README.md`](machines/README.md).
>
> v4 Users PUT also merges supplied fields onto the stored user instead of clearing omitted fields.
> PUT still validates its required fields and applies explicit `null`, subject to validation and
> service rules; v4 Users PATCH ignores explicit `null`. Both use the normal business save, which
> can also update derived fields and the shared sign-in identity. See
> [`users/README.md`](users/README.md#update--disable).

| Entity | PATCH endpoint |
|--------|----------------|
| Projects | `PATCH /api/v4/integration/projects/{id}` — dynamic, any subset of fields |
| Work orders | `PATCH /api/v4/integration/tasks/{id}` — dynamic |
| Machines | `PATCH /api/v4/integration/machines/{id}` · ESG: `PATCH /machines/{id}/esg` |
| Users (v4) | `PATCH /api/v4/integration/users/{companyProfileId}` |
| Employees (v5) | `PATCH /api/v5/integration/employees/{employeeNumber}` — omitted = unchanged, explicit `null` = clear |

Each folder's README shows the PATCH call for that entity.

## Postman

Import [`postman/Ditio-Integration.postman_collection.json`](postman/README.md) plus the **Production** or **Test** environment file. See [`postman/README.md`](postman/README.md).

## Notes

- **Tokens are short-lived** (~30 min by default) — cache and reuse them (see [`authentication`](authentication/README.md)).
- Most write endpoints require **Administrator**-level access and are **company-scoped**.
- The authoritative schema reference is the **Swagger** above.
