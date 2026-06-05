# 08 — Certificates

`api/v4/integration/certificates` · scope `ditioapiv3`. Manage user certificates/qualifications (safety courses, builder cards, crane licenses). Matched to a user by `employeeNumber` + `certificateType`. Set `$BASE_URL` / `$TOKEN` — see [`../01-authentication`](../01-authentication).

## Create or update (array)

```bash
curl -X POST $BASE_URL/api/v4/integration/certificates \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '[
    {
      "employeeNumber": "1042",
      "certificateType": "Byggekort",
      "certificateNumber": "BK-2025-12345",
      "issuedDateTime": "2025-01-15T00:00:00Z",
      "validUntilDateTime": "2027-01-15T00:00:00Z",
      "notes": "Issued by Byggenæringens Landsforening"
    },
    {
      "employeeNumber": "1042",
      "certificateType": "Kranførerbevis",
      "certificateNumber": "KF-2025-67890",
      "issuedDateTime": "2024-06-01T00:00:00Z",
      "validUntilDateTime": "2026-06-01T00:00:00Z"
    }
  ]'
```

Required per item: `employeeNumber`, `certificateType`, `issuedDateTime`, `validUntilDateTime`. The `employeeNumber` must match an existing user; `issuedDateTime` must be before `validUntilDateTime`. Re-posting the same `employeeNumber` + `certificateType` **updates** the existing certificate.

**C#:** [`CertificatesExample.cs`](CertificatesExample.cs).
