namespace Ditio.Samples.Examples;

/// <summary>
/// Push a document to a project (and the same works for a work order), list, then remove it, via
/// api/v4/integration/projects|tasks/{id}/documents. Then the same by project number
/// (api/v4/integration/projects/by-project-number/{projectNumber}/documents): a section path that builds
/// folders, delete by name, and folders, pages and documents addressed by your own ids. Pushed documents
/// surface in the Ditio Info Center, visible to the project's members in the mobile app.
/// </summary>
public static class DocumentsExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // Ensure a project to attach to.
        var project = await api.PostAsync("api/v4/integration/projects", new
        {
            companyId = cfg.CompanyId,
            projectNumber = "SAMPLE-P-001",
            name = "Sample project",
            active = true,
        });
        string? projectId = project?.id;
        if (projectId is null)
        {
            Console.WriteLine("Could not resolve a project id; aborting.");
            return;
        }

        // Create a small file to upload (replace with a real PDF/Word/image in practice).
        var tempFile = Path.Combine(Path.GetTempPath(), "ditio-sample-document.txt");
        await File.WriteAllTextAsync(tempFile, "Hello from the Ditio documents sample.");

        // Upload + attach. Optional 'section' groups documents onto a named page under the project's
        // document folder (re-use the same section to add more). replaceExistingFilesWithSameName=true
        // replaces a same-named file on re-sync.
        var upload = await api.UploadFilesAsync(
            $"api/v4/integration/projects/{projectId}/documents?section=Drawings&replaceExistingFilesWithSameName=true", tempFile);
        string? fileReferenceId = upload?.files?[0]?.fileReference?.id;
        Console.WriteLine($"Uploaded file reference id: {fileReferenceId}");
        Console.WriteLine($"Landed on Info Center page: {upload?.section} ({upload?.pageExternalId})");

        // List the document pages (sections) on the project and their files.
        await api.GetAsync($"api/v4/integration/projects/{projectId}/documents");

        // Download a single document by id (gated to the project). The endpoint streams the bytes back
        // as the response body (200, application/octet-stream + Content-Disposition), so use the binary
        // download helper rather than GetAsync (which parses JSON).
        if (fileReferenceId is not null)
        {
            var downloadPath = Path.Combine(Path.GetTempPath(), "ditio-sample-document-downloaded.txt");
            await api.DownloadFileAsync(
                $"api/v4/integration/projects/{projectId}/documents/{fileReferenceId}", downloadPath);
            if (File.Exists(downloadPath))
                File.Delete(downloadPath);
        }

        // Remove the document we just uploaded.
        if (fileReferenceId is not null)
            await api.DeleteAsync($"api/v4/integration/projects/{projectId}/documents/{fileReferenceId}");

        // --- By project number, with folders: section paths --------------------------------------------
        // Every level of 'section' but the last is a folder; the last is the page that holds the documents
        // (at most 10 levels). Missing levels are created, existing ones reused. A one-level section, like
        // 'Drawings' above, is unchanged. Escape the path: it is a query value.
        const string projectNumber = "SAMPLE-P-001";
        const string sectionPath = "Dokumenter/Tillegg og endringer/Endringsmeldinger";
        var byNumber = $"api/v4/integration/projects/by-project-number/{Uri.EscapeDataString(projectNumber)}/documents";

        var pathUpload = await api.UploadFilesAsync(
            $"{byNumber}?section={Uri.EscapeDataString(sectionPath)}&replaceExistingFilesWithSameName=true", tempFile);
        Console.WriteLine($"Landed on: {pathUpload?.section} ({pathUpload?.pageExternalId})");

        // Describe a folder on the path — any level can be described, not only the page.
        await api.PatchAsync(
            $"{byNumber}?section={Uri.EscapeDataString("Dokumenter/Tillegg og endringer")}",
            new { description = "Change orders and change notices for this project." });

        // Lists every page at every depth; 'section' is the full path, plus 'sectionId' and 'files[].fileId'.
        await api.GetAsync(byNumber);

        // Delete by section and file name — no Ditio id needed (204, or 404 when it is not there).
        await api.DeleteAsync(
            $"{byNumber}?section={Uri.EscapeDataString(sectionPath)}&fileName={Uri.EscapeDataString(Path.GetFileName(tempFile))}");

        // --- Your own ids: mirror a folder tree (e.g. SharePoint item ids) -----------------------------
        // Sending the same sectionId again with a new name or parentSectionId renames or moves the section.
        // Sections by id and sections by path are separate trees; a sync uses one or the other.
        await api.PutAsync($"{byNumber}/sections/sp-dokumenter", new
        {
            name = "Dokumenter",
            isFolder = true,
            description = "Project documents mirrored from the document system.",
        });
        await api.PutAsync($"{byNumber}/sections/sp-meldinger", new
        {
            name = "Endringsmeldinger",
            parentSectionId = "sp-dokumenter",
            isFolder = false,
        });

        // Upload exactly one file with your fileId; uploading the same fileId again replaces the document
        // wherever it is on the project.
        await api.UploadFilesAsync($"{byNumber}?sectionId=sp-meldinger&fileId=sp-file-0042", tempFile);

        // Delete the document by your file id, then the folder with everything in it (204, or 404).
        await api.DeleteAsync($"{byNumber}/files/sp-file-0042");
        await api.DeleteAsync($"{byNumber}/sections/sp-dokumenter");

        File.Delete(tempFile);

        // The work-order endpoints are identical — swap 'projects' for 'tasks' and use the task id (its
        // documents nest under the parent project's folder; section defaults to the work order's number):
        //   POST   api/v4/integration/tasks/{taskId}/documents
        //   GET    api/v4/integration/tasks/{taskId}/documents
        //   GET    api/v4/integration/tasks/{taskId}/documents/{fileReferenceId}
        //   DELETE api/v4/integration/tasks/{taskId}/documents/{fileReferenceId}
    }
}
