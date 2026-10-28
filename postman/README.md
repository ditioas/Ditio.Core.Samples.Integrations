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
- **Projects / Work orders / Documents / Machines / Reference data / Users (v4) / Employees (v5) / Certificates** — send data into Ditio. **Projects** also reads who is on each project and their role (personnel).
- **Payroll export** and **Data extraction** — read data out of Ditio (the data-extraction folder uses `reportingToken` + `reportingUrl`).

## Environments

| | Production | Test |
|---|---|---|
| `identityUrl` | `https://identity.ditio.app` | `https://identity.ditio.dev` |
| `baseUrl` (integration) | `https://integration.ditio.no` | `https://core-api.ditio.dev/core` |
| `reportingUrl` (v1) | `https://core-api.ditio.app/reporting` | `https://core-api.ditio.dev/reporting` |

Lookup/create requests auto-fill `projectId` / `taskId` / `machineId` / `fileReferenceId` from the response, so you can run a folder top-to-bottom.

The **Certificates** folder's **Upload certificate attachments** request takes `employeeNumber` and `certificateType` in the query string (set to your values, create the certificate with **Create/update certificates** first) and one or more `file` parts; add `certificateNumber` to pick the card when the employee holds more than one.

The **Documents** folder's by-project-number requests use `projectNumber` plus `sectionPath` (a `/`-separated folder path, default `Dokumenter/Tillegg og endringer/Endringsmeldinger`), `fileName` (filled by the section-path upload) and your own ids `folderSectionId` (`sp-dokumenter`), `subFolderSectionId` (`sp-tillegg`), `sectionId` (the page, `sp-meldinger`) and `fileId` (`sp-file-0042`) — set these to your system's values. The upload requests also store `pageExternalId` and `pageId` (Ditio's id of the Info Center page, for example to link to it); the list, **Describe …** and **Put section …** responses carry `pageId` too. Run the three **Put section …** requests in order (folder, sub-folder, page) before **Upload with your section and file id**. Download and delete by `fileReferenceId` use `projectId`; they have no by-project-number variant. Work-order uploads take a one-level `section` only; section paths, `sectionId` and `fileId` are project-only.

> **Prefer PATCH over PUT** — PUT replaces the whole object and wipes any field you omit. The exception is **Put section …** in **Documents**: omitting `description` there keeps the current text.
