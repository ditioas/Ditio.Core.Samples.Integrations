# 15 — Checklist registrations: mark as reported

Read approved checklist registrations from the Data Extraction API, process them in your own system (an HSE or document system, say), then set them to **Reported** in Ditio. Reporting is the same step an approver takes in the Ditio app.

`api/integration/checklist-registrations/reported` · scope `ditioapiv3`. Reading uses `v1/checklist-registrations` on the reporting API · scope `reportingapiv1`. Runnable C# example: [`ChecklistRegistrationsReportedExample.cs`](ChecklistRegistrationsReportedExample.cs).

> **Auth:** your API client must have **Administrator** access. A token with the wrong scope returns **401**, not 403: the scope is validated as the token's audience.

## 1. Read approved registrations

```bash
curl "$REPORTING_BASE_URL/v1/checklist-registrations?ModifiedSince=2026-11-01T00:00:00Z&ChunkLimit=500" \
  -H "Authorization: Bearer $REPORTING_TOKEN"
```

Keep the records with `statusText` `Approved`. Follow the top-level `continuationToken` until it is `null`; `meta.continuationToken` only echoes the token you sent.

## 2. Mark as reported

```bash
curl -X POST "$BASE_URL/api/integration/checklist-registrations/reported" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "ids": ["6a8fca2d63223ae8ad106164", "6a8fca2d63223ae8ad106165"] }'
```

`ids`: 1 to 100 distinct registration ids, the `id` from step 1 (or from `v1/machine-checklist-registrations`). A missing, empty, blank or repeated id, or more than 100 ids, returns **400** and changes nothing.

| Status | When | Changed |
|---|---|---|
| **401** | No valid `ditioapiv3` bearer token. | no |
| **403** | The client is not an administrator, or `x-company-id` is not allowed for it. A read-only client also gets 403 when any id in the batch would be reported; if none would be, it gets 200 with the per-id results. | no |
| **413** | The body is larger than 64 KB (100 ids need about 3 KB). | no |

**200**, one result per id in the order sent:

```json
[
  { "id": "6a8fca2d63223ae8ad106164", "result": "reported" },
  { "id": "6a8fca2d63223ae8ad106165", "result": "alreadyReported" }
]
```

| `result` | Meaning | Changed |
|---|---|---|
| `reported` | It was Approved and is now Reported. | yes |
| `alreadyReported` | It was already Reported. Retrying is safe. | no |
| `notApproved` | It is pending, sent for approval or rejected. Only Approved can become Reported. Also returned when someone else edited it repeatedly while the request ran: read it again and resend. | no |
| `notPermitted` | Your client can read it but has no approver rights on it. | no |
| `notFound` | It does not exist, was deleted, or is not visible to your client. A subsidiary's registration usually returns this; see below. | no |

Each id is handled on its own: one id that cannot be reported does not stop the others.

## Good to know

- **Approver rights follow the Ditio app, per registration.** Your client can report a registration when it has approver rights on it, and every company the checklist is shared with counts, not only your own. A subcontractor's client can report a registration on a main contractor's project when the subcontractor's company is on the checklist. The company that owns a machine checklist's template can report machine registrations made in other companies when it is on the checklist's company list. A parent company with project companies can report only the registrations on its own projects; a project company's registrations return `notPermitted`.
- **Subsidiaries are not included.** Data Extraction returns a parent company's subsidiaries' registrations, but this call acts for your client's own company, so a subsidiary's registrations normally return `notFound` and stay `Approved` (unless your company is on the checklist, as above). To report them, call again with the `x-company-id` header set to the subsidiary, once per subsidiary. This works only for a client that does not emulate a user; a client that emulates a user keeps that user's company.
- **After a 5xx, resend the same request.** A server error part-way through can leave earlier ids `Reported` without returning their results. Resending is safe: those ids return `alreadyReported` and the rest are processed.
- **It comes back once in extraction.** Reporting adds a new version, so your next incremental pull returns the registration again with status `Reported`. Pick up `Approved` only; another company's client may also have reported it already.
- Only Approved → Reported is offered. Approving, rejecting and reverting stay in the Ditio app.

Full guide: https://docs.ditio.app/guides/safety-reporting/
