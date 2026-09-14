namespace Ditio.Samples.Examples;

/// <summary>Create → look up → update → list projects via api/v4/integration/projects.</summary>
public static class ProjectsExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // Create (idempotent on projectNumber — returns the existing project if it already exists).
        var created = await api.PostAsync("api/v4/integration/projects", new
        {
            companyId = cfg.CompanyId,
            projectNumber = "SAMPLE-P-001",
            name = "Sample project",
            active = true,
        });
        string? projectId = created?.id;

        // Look up by project number.
        await api.GetAsync("api/v4/integration/projects/by-project-number/SAMPLE-P-001");

        // Partial update (PATCH only changes the fields you send).
        if (projectId is not null)
            await api.PatchAsync($"api/v4/integration/projects/{projectId}", new { name = "Sample project (updated)" });

        // List all projects in the company.
        await api.GetAsync("api/v4/integration/projects");

        // Missing and cross-company project IDs return the same 404 response. PATCH never creates
        // an unavailable project; DitioApiClient logs both expected responses and returns null.
        const string unavailableProjectId = "000000000000000000000000";
        await api.GetAsync($"api/v4/integration/projects/{unavailableProjectId}");
        await api.PatchAsync(
            $"api/v4/integration/projects/{unavailableProjectId}",
            new { name = "This project will not be created" });

        // Delete is destructive and cascades — uncomment to try it:
        // if (projectId is not null) await api.DeleteAsync($"api/v4/integration/projects/{projectId}");
    }
}
