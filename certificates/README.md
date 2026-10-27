# 08 — Certificates

`api/v4/integration/certificates` · scope `ditioapiv3`. Manage user certificates/qualifications (safety courses, builder cards, crane licenses) and attach scans of the card. Matched to a user by `employeeNumber` + `certificateType`. Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

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

Required per item: `employeeNumber`, `certificateType`, `issuedDateTime`, `validUntilDateTime`. Optional: `certificateNumber`, `notes`. The `employeeNumber` must match an existing user; `issuedDateTime` must be before `validUntilDateTime`. Re-posting the same `employeeNumber` + `certificateType` **updates** the existing certificate. `certificateNumber` is stored on the certificate; on an update, leaving it out keeps the stored number.

## Upload attachments to a certificate

`POST api/v4/integration/certificates/attachments` — attach images or PDF scans of the card (front and back, for example) to a certificate. The certificate is found by the same key as the upsert, `employeeNumber` + `certificateType` (type matching ignores case and spaces), so you never need a Ditio id. **Create the certificate first** (above), then upload the files.

Send one or more files as `multipart/form-data`, each in a part named `file`. Query parameters:

| Param | Required | Effect |
|-------|----------|--------|
| `employeeNumber` | yes | The employee's number, as in the upsert |
| `certificateType` | yes | The certificate type, as in the upsert |
| `replaceExistingFilesWithSameName` | no | Default `false`. When `true`, a file your company uploaded earlier to this certificate with the same original file name is replaced instead of added again. Attachments added by others (for example the employee in the app) are never replaced |

```bash
curl -X POST "$BASE_URL/api/v4/integration/certificates/attachments?employeeNumber=1042&certificateType=Byggekort&replaceExistingFilesWithSameName=true" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@/path/to/byggekort-front.jpg" \
  -F "file=@/path/to/byggekort-back.jpg"
```

**Response** (`200`):

```json
{
  "successful": true,
  "message": "Files uploaded and attached ok",
  "certificateId": "66c1a2b3c4d5e6f7a8b9c0d4",
  "files": [
    {
      "name": "byggekort-front.jpg",
      "created": "2026-06-05T08:10:25Z",
      "modified": "2026-06-05T08:10:25Z",
      "size": 182113,
      "url": "/api/file/66b1a2c3d4e5f6a7b8c9d0e2",
      "fileReference": { "id": "66b1a2c3d4e5f6a7b8c9d0e2" }
    }
  ],
  "attachments": ["66b1a2c3d4e5f6a7b8c9d0e2"]
}
```

`certificateId` is the certificate the files were attached to; `files[].fileReference.id` and `attachments` are the ids of the stored files.

| Status | When |
|--------|------|
| `404` | The employee has no current certificate of that type — create it first |
| `400` | `employeeNumber` or `certificateType` is blank, or the body is not `multipart/form-data` |

**C#:** [`CertificatesExample.cs`](CertificatesExample.cs).
