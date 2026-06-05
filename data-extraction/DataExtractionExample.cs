namespace Ditio.Samples.Examples;

/// <summary>
/// The paginated Data Extraction API (<c>v1/*</c>) on the reporting host — the recommended way to
/// pull data OUT of Ditio in bulk. Uses the <c>reportingapiv1</c> scope and a continuation token.
/// </summary>
public static class DataExtractionExample
{
    // Every v1 extraction endpoint is a GET, paginated with a continuationToken.
    private static readonly (string Name, string Path)[] Endpoints =
    {
        ("Projects",                  "v1/project"),
        ("Work orders (WBS)",         "v1/project/work-breakdown-structure"),
        ("Users",                     "v1/user"),
        ("Resources (machines)",      "v1/resource"),
        ("Checklists",                "v1/checklist-registrations"),
        ("Alerts (incidents)",        "v1/incident-registrations"),
        ("Project transactions",      "v1/time-registrations"),
        ("Machine registrations",     "v1/machine-registrations"),
        ("Absences",                  "v1/absence-registrations"),
        ("Payroll lines",             "v1/payroll-lines"),
        ("Images",                    "v1/images"),
    };

    public static async Task RunAsync(DitioConfig cfg)
    {
        // Reporting API uses a different base URL AND scope than the integration API.
        var api = new DitioApiClient(cfg.ReportingBaseUrl, cfg.ReportingScope, new DitioTokenProvider(cfg));

        // Fetch the first page of each extraction endpoint.
        foreach (var (name, path) in Endpoints)
        {
            Console.WriteLine($"\n--- {name} ({path}) ---");
            await api.GetAsync(path);
        }

        // Pagination: follow continuationToken until it is empty.
        Console.WriteLine("\n--- Paging through ALL projects ---");
        string? token = null;
        do
        {
            var path = token is null ? "v1/project" : $"v1/project?continuationToken={Uri.EscapeDataString(token)}";
            var page = await api.GetAsync(path);
            token = page?.continuationToken;
        } while (!string.IsNullOrEmpty(token));
    }
}
