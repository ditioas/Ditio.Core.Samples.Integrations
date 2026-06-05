# 04 — Documents

Attach documents (PDF, Word, images, …) to **projects** and **work orders**. `api/v4/integration/projects|tasks/{id}/files` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` — see [`../01-authentication`](../01-authentication).

## Upload (multipart)

```bash
curl -X POST "$BASE_URL/api/v4/integration/projects/{projectId}/files?replaceExistingFilesWithSameName=true" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@/path/to/Drawing-A1.pdf" \
  -F "file=@/path/to/Site-plan.docx"
```

`replaceExistingFilesWithSameName=true` replaces an already-attached file with the same original filename (use on re-sync to avoid duplicates). The response's `files[].fileReference.id` is the handle you store — you need it to download, replace, or delete the document.

## List / download / delete

```bash
curl    $BASE_URL/api/v4/integration/projects/{projectId}/files                    -H "Authorization: Bearer $TOKEN"   # list
curl -L $BASE_URL/api/file/{fileReferenceId} -o out.pdf                             -H "Authorization: Bearer $TOKEN"   # download
curl -X DELETE $BASE_URL/api/v4/integration/projects/{projectId}/files/{fileRefId} -H "Authorization: Bearer $TOKEN"   # remove
```

## Work orders

Identical — swap `projects` for `tasks` and use the work order id:

```bash
curl -X POST "$BASE_URL/api/v4/integration/tasks/{taskId}/files" -H "Authorization: Bearer $TOKEN" -F "file=@/path/to/Method-statement.pdf"
```

## Notes

- Replace matches by **filename only** — keep your own mapping of `fileReference.id` if you need stable matching.
- You only see/act on projects and work orders in your own company.

**C#:** [`DocumentsExample.cs`](DocumentsExample.cs).
