# 10 — Data extraction (out of Ditio)

The recommended way to pull data **out** of Ditio in bulk. Paginated **reporting API** on a different host **and** scope than the integration API:

- **Base:** `https://core-api.ditio.app/core` (test: `https://core-api.ditio.dev/core`)
- **Scope:** `reportingapiv1`

```bash
REPORTING_URL=https://core-api.ditio.app/core      # test: https://core-api.ditio.dev/core
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

> A documents extractor (`v1/documents`, non-image files) is planned but not yet available.

**C#:** [`DataExtractionExample.cs`](DataExtractionExample.cs) — fetches the first page of each endpoint and demonstrates paging.
