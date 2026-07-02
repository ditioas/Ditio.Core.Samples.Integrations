# 10 — Data extraction (out of Ditio)

The recommended way to pull data **out** of Ditio in bulk. Paginated **reporting API** on a different host **and** scope than the integration API:

- **Base:** `https://core-api.ditio.app/reporting` (test: `https://core-api.ditio.dev/reporting`)
- **Scope:** `reportingapiv1`

```bash
REPORTING_URL=https://core-api.ditio.app/reporting   # test: https://core-api.ditio.dev/reporting
TOKEN=$(curl -s -X POST $IDENTITY/connect/token \
  -d "grant_type=client_credentials" -d "client_id=YOUR_CLIENT_ID" -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "scope=reportingapiv1" | jq -r '.access_token')
```

## Endpoints

| Data | Endpoint |
|------|----------|
| Projects | `GET $REPORTING_URL/v1/project` |
| Work orders (WBS) | `GET $REPORTING_URL/v1/project/work-breakdown-structure` |
| Users | `GET $REPORTING_URL/v1/user` |
| Resources (machines) | `GET $REPORTING_URL/v1/resource` |
| Checklists | `GET $REPORTING_URL/v1/checklist-registrations` |
| Alerts (incidents) | `GET $REPORTING_URL/v1/incident-registrations` |
| Project transactions (time) | `GET $REPORTING_URL/v1/time-registrations` |
| Machine registrations | `GET $REPORTING_URL/v1/machine-registrations` |
| Absences | `GET $REPORTING_URL/v1/absence-registrations` |
| Payroll lines | `GET $REPORTING_URL/v1/payroll-lines` (and `/v1/payroll-lines-extended`) |
| Items | `GET $REPORTING_URL/v1/item-registrations/web-query` |
| Images | `GET $REPORTING_URL/v1/images` |
| Machine checklists / incidents | `GET $REPORTING_URL/v1/machine-checklist-registrations`, `/v1/machine-incident-registrations` |
| Ditio Flow (locations, mass types, trips) | `GET $REPORTING_URL/v1/flow-locations`, `/v1/flow-mass-types`, `/v1/flow-trip-registrations` |

Example:

```bash
curl "$REPORTING_URL/v1/checklist-registrations" -H "Authorization: Bearer $TOKEN"
```

## Pagination

Responses are paged. When more data is available, the response includes a `continuationToken`; pass it back to fetch the next page until it is empty:

```bash
curl "$REPORTING_URL/v1/project?continuationToken=THE_TOKEN" -H "Authorization: Bearer $TOKEN"
```

Most endpoints accept date-window filters (`modifiedSince` / `modifiedBefore`, `fromDateTime` / `toDateTime`) for incremental sync. See Swagger for each endpoint's filter.

## Generated PDFs (`pdfUrl`)

Ditio generates PDFs for checklists, alerts (incidents) and absences. Each record on these endpoints carries a `pdfUrl` — an **absolute** link to the rendered PDF:

| PDF | Endpoint | Field |
|-----|----------|-------|
| Checklist / form | `v1/checklist-registrations`, `v1/machine-checklist-registrations` | `pdfUrl` |
| Alert / incident | `v1/incident-registrations`, `v1/machine-incident-registrations` | `pdfUrl` |
| Absence | `v1/absence-registrations` | `pdfUrl` |

Combine with `modifiedSince` for incremental sync, then download each non-null `pdfUrl`:

```bash
curl -s "$REPORTING_URL/v1/checklist-registrations?ProjectId=$PROJECT_ID&modifiedSince=2026-06-01T00:00:00Z" \
  -H "Authorization: Bearer $TOKEN" \
| jq -r '.data[] | select(.pdfUrl != null) | .pdfUrl' \
| while read -r url; do curl -sL -H "Authorization: Bearer $TOKEN" -O "$url"; done
```

`pdfUrl` is `null` until the PDF has been generated (on submit/report) — skip those records and pick them up on a later sync. Checklists also expose files attached inside the checklist via `sections[].attachments[].url` and `sections[].images[].url`.

**Keeping in sync with regenerated PDFs.** A record's PDF is regenerated whenever its status changes (submitted, reported, approved, rejected), synchronously as part of that change — so the record reappears in your next `modifiedSince` pull with its `pdfUrl` already pointing at the current PDF. Sync on `modifiedSince` and re-fetch `pdfUrl` each time a record reappears.

> A generic documents extractor (`v1/documents`, arbitrary non-image files) is planned but not yet available.

**C#:** [`DataExtractionExample.cs`](DataExtractionExample.cs) — fetches the first page of each endpoint, demonstrates paging, and downloads checklist/alert/absence PDFs via `pdfUrl`.
