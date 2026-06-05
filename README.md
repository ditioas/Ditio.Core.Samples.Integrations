# Ditio.Core.Samples.Integrations

Example integrations for the **Ditio Core API** — one folder per integration kind, each with a runnable **C#** example and **curl** snippets, plus a **Postman** collection that covers everything.

## Swagger (full API reference)

- **Production:** https://integration.ditio.no/swagger
- **Test:** https://core-api.ditio.dev/core/swagger

## Contents

**Start here**

- [`01-authentication`](01-authentication/README.md) — get an OAuth2 token (shared by everything below)

**Send data into Ditio** (create / update)

- [`02-projects`](02-projects/README.md) — projects
- [`03-work-orders`](03-work-orders/README.md) — work orders (tasks)
- [`07-machines`](07-machines/README.md) — machines & equipment
- [`11-reference-data`](11-reference-data/README.md) — machine types, alert types (lookups you need first)
- [`05-users`](05-users/README.md) — users (v4)
- [`06-employees-v5`](06-employees-v5/README.md) — employees (v5, **recommended**)
- [`08-certificates`](08-certificates/README.md) — user certificates
- [`04-documents`](04-documents/README.md) — documents on projects & work orders

**Get data out of Ditio** (read / sync)

- [`10-data-extraction`](10-data-extraction/README.md) — projects, work orders, checklists, alerts, project transactions, absences, payroll, users, images … (paginated `v1/*`)
- [`09-payroll-export`](09-payroll-export/README.md) — dedicated payroll / absence export

**Other**

- [`postman`](postman/README.md) — Postman collection + Production/Test environments
- `PowerBI template` — Power BI data-source template

## Environments

| | Production | Test |
|---|---|---|
| **Identity** | `https://identity.ditio.app` | `https://identity.ditio.dev` |
| **Integration API** (`api/v4`, `api/v5`) | `https://integration.ditio.no` | `https://core-api.ditio.dev/core` |
| **Reporting API** (`v1/*`) | `https://core-api.ditio.app/core` | `https://core-api.ditio.dev/core` |
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

- **`PATCH`** is a partial, dynamic update — send only the fields you want to change; everything else is left exactly as it was. You don't need to fetch the full object first.
- **`PUT`** is a *full replace* — it overwrites the entire object with the body you send. **Any field you omit is wiped** (cleared or reset to its default). This is a common cause of accidental data loss during syncs.

So unless you deliberately want to replace every field, reach for **PATCH**.

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

- **Tokens are short-lived** (~30 min by default) — cache and reuse them (see [`01-authentication`](01-authentication/README.md)).
- Most write endpoints require **Administrator**-level access and are **company-scoped**.
- The authoritative schema reference is the **Swagger** above.
