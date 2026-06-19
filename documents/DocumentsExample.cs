namespace Ditio.Samples.Examples;

/// <summary>
/// Push a document to a project (and the same works for a work order), list, then remove it, via
/// api/v4/integration/projects|tasks/{id}/documents. Pushed documents surface in the Ditio Info Center,
/// visible to the project's members in the mobile app.
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

        // Download a single document by id (gated to the project):
        //   GET api/v4/integration/projects/{projectId}/documents/{fileReferenceId}

        // Remove the document we just uploaded.
        if (fileReferenceId is not null)
            await api.DeleteAsync($"api/v4/integration/projects/{projectId}/documents/{fileReferenceId}");

        File.Delete(tempFile);

        // The work-order endpoints are identical — swap 'projects' for 'tasks' and use the task id (its
        // documents nest under the parent project's folder; section defaults to the work order's number):
        //   POST   api/v4/integration/tasks/{taskId}/documents
        //   GET    api/v4/integration/tasks/{taskId}/documents
        //   GET    api/v4/integration/tasks/{taskId}/documents/{fileReferenceId}
        //   DELETE api/v4/integration/tasks/{taskId}/documents/{fileReferenceId}
    }
}
