# 04 — Documents

Push documents (PDF, Word, images, …) to **projects** and **work orders**. `api/v4/integration/projects|tasks/{id}/documents` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

Pushed documents surface in the Ditio **Info Center**, so the project's members see them in the **mobile app**, grouped under a per-project folder and (optionally) into named **sections**.

## Upload (multipart)

```bash
curl -X POST "$BASE_URL/api/v4/integration/projects/{projectId}/documents?section=Drawings&replaceExistingFilesWithSameName=true" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@/path/to/Drawing-A1.pdf" \
  -F "file=@/path/to/Site-plan.docx"
```

- `section` (optional) groups the documents onto a named page; re-use the same value to add more. Omit it → a default **"Documents"** page.
- `replaceExistingFilesWithSameName=true` replaces a document on the page with the same original filename (use on re-sync to avoid duplicates).

The response confirms the page (`section`, `pageExternalId`) and returns `files[].fileReference.id` — the handle you store to download or delete a document.

## List / download / delete

```bash
curl    $BASE_URL/api/v4/integration/projects/{projectId}/documents                       -H "Authorization: Bearer $TOKEN"   # list pages + files
curl -L $BASE_URL/api/v4/integration/projects/{projectId}/documents/{fileRefId} -o out.pdf -H "Authorization: Bearer $TOKEN"   # download one
curl -X DELETE $BASE_URL/api/v4/integration/projects/{projectId}/documents/{fileRefId}    -H "Authorization: Bearer $TOKEN"   # remove
```

## Work orders

Identical — swap `projects` for `tasks` and use the work order id. Its documents nest under the parent project's folder; `section` defaults to the work order's number/short description:

```bash
curl -X POST "$BASE_URL/api/v4/integration/tasks/{taskId}/documents" -H "Authorization: Bearer $TOKEN" -F "file=@/path/to/Method-statement.pdf"
```

## Notes

- The durable handle is `(project, section)` — re-using it adds to the same page. Replace matches by **filename only**; keep your own mapping of `fileReference.id` if you need stable matching.
- Documents are visible to active employees assigned to the project.
- You only see/act on projects and work orders in your own company.

**C#:** [`DocumentsExample.cs`](DocumentsExample.cs).
