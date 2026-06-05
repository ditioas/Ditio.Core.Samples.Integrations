namespace Ditio.Samples.Examples;

/// <summary>
/// Attach a document to a project (and the same works for a work order), list, then remove it,
/// via api/v4/integration/projects|tasks/{id}/files.
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

        // Upload + attach. replaceExistingFilesWithSameName=true replaces a same-named file on re-sync.
        var upload = await api.UploadFilesAsync(
            $"api/v4/integration/projects/{projectId}/files?replaceExistingFilesWithSameName=true", tempFile);
        string? fileReferenceId = upload?.files?[0]?.fileReference?.id;
        Console.WriteLine($"Uploaded file reference id: {fileReferenceId}");

        // List the documents on the project.
        await api.GetAsync($"api/v4/integration/projects/{projectId}/files");

        // Download is a plain GET on the file id: GET {BaseUrl}/api/file/{fileReferenceId}

        // Remove the document we just uploaded.
        if (fileReferenceId is not null)
            await api.DeleteAsync($"api/v4/integration/projects/{projectId}/files/{fileReferenceId}");

        File.Delete(tempFile);

        // The work-order endpoints are identical — swap 'projects' for 'tasks' and use the task id:
        //   POST   api/v4/integration/tasks/{taskId}/files
        //   GET    api/v4/integration/tasks/{taskId}/files
        //   DELETE api/v4/integration/tasks/{taskId}/files/{fileReferenceId}
    }
}
