# Postman

A collection covering the Ditio Core integration APIs, plus **Production** and **Test** environment files.

## Setup

1. Import `Ditio-Integration.postman_collection.json`.
2. Import one of the environment files and select it (or fill the collection variables directly):
   - `Ditio-Integration.Production.postman_environment.json`
   - `Ditio-Integration.Test.postman_environment.json`
3. Fill in `clientId`, `clientSecret`, `companyId`.
4. Run **Auth → Get integration token** (sets `accessToken`). For the **Data extraction** folder, also run **Auth → Get reporting token** (sets `reportingToken`).

## Folders

- **Auth** — integration token (`ditioapiv3`) and reporting token (`reportingapiv1`).
- **Projects / Work orders / Documents / Machines / Reference data / Users (v4) / Employees (v5) / Certificates** — send data into Ditio.
- **Payroll export** and **Data extraction** — read data out of Ditio (the data-extraction folder uses `reportingToken` + `reportingUrl`).

## Environments

| | Production | Test |
|---|---|---|
| `identityUrl` | `https://identity.ditio.app` | `https://identity.ditio.dev` |
| `baseUrl` (integration) | `https://integration.ditio.no` | `https://core-api.ditio.dev/core` |
| `reportingUrl` (v1) | `https://core-api.ditio.app/reporting` | `https://core-api.ditio.dev/reporting` |

Lookup/create requests auto-fill `projectId` / `taskId` / `machineId` / `fileReferenceId` from the response, so you can run a folder top-to-bottom.

> **Prefer PATCH over PUT** — PUT replaces the whole object and wipes any field you omit.
