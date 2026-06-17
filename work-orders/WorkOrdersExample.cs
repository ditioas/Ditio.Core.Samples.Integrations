namespace Ditio.Samples.Examples;

/// <summary>Create a work order (task) under a project and look it up via api/v4/integration/tasks.</summary>
public static class WorkOrdersExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // A work order belongs to a project, so make sure one exists first.
        var project = await api.PostAsync("api/v4/integration/projects", new
        {
            companyId = cfg.CompanyId,
            projectNumber = "SAMPLE-P-001",
            name = "Sample project",
            active = true,
        });
        string? projectId = project?.id;

        // Create a work order.
        //
        // Settings like safeJobAnalysisApprovalRequired, measureUnitQty, unitId, costPrice,
        // price and fixedResourcePrice are optional. Omit them to leave them "not provided":
        // if the project has a template work order (marked as template in the Ditio backoffice),
        // omitted settings are inherited from it. Any value you send wins over the template.
        var created = await api.PostAsync("api/v4/integration/tasks", new
        {
            companyId = cfg.CompanyId,
            projectId,
            externalId = "SAMPLE-WO-100",
            name = "Foundation work",
            active = true,
            // Provided explicitly -> wins over the template:
            safeJobAnalysisApprovalRequired = true,
            costPrice = 1200.0,
            // measureUnitQty / unitId / price / fixedResourcePrice omitted -> inherited from the
            // project's template work order if one exists.
        });
        string? taskId = created?.id;

        // List the project's work orders.
        if (projectId is not null)
            await api.GetAsync($"api/v4/integration/tasks/project/{projectId}");

        // Partial update.
        if (taskId is not null)
            await api.PatchAsync($"api/v4/integration/tasks/{taskId}", new { name = "Foundation work (phase 2)" });

        // Delete (destructive) — uncomment to try:
        // if (taskId is not null) await api.DeleteAsync($"api/v4/integration/tasks/{taskId}");
    }
}
