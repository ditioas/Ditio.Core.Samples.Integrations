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

`ids`: 1 to 100 distinct registration ids, the `id` from step 1. Anything else returns **400** and changes nothing.

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
| `notApproved` | Not submitted, sent for approval or rejected. Only Approved can become Reported. | no |
| `notPermitted` | Your client can read it but has no approver rights on it. | no |
| `notFound` | It does not exist, was deleted, or is not visible to your client. | no |

## Good to know

- **It comes back once in extraction.** Reporting adds a new version, so your next incremental pull returns the registration again with status `Reported`. Pick up `Approved` only.
- **Approver rights follow the Ditio app.** An administrator client reports its own company's registrations. On a parent company with project companies, that is the registrations on the parent company's own projects; a project company's registrations return `notPermitted`.
- Only Approved → Reported is offered. Approving, rejecting and reverting stay in the Ditio app.

Full guide: https://docs.ditio.app/guides/safety-reporting/
