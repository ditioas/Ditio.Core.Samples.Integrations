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

    // Extraction endpoints whose records carry a `pdfUrl` link to a Ditio-generated PDF.
    private static readonly (string Name, string Path)[] PdfEndpoints =
    {
        ("Checklist PDFs", "v1/checklist-registrations"),
        ("Alert PDFs",     "v1/incident-registrations"),
        ("Absence PDFs",   "v1/absence-registrations"),
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

        // Pull the PDFs Ditio generates (checklist, alert, absence).
        await PullGeneratedPdfsAsync(api);
    }

    /// <summary>
    /// Demonstrates pulling Ditio-generated PDFs. Checklist, alert (incident) and absence extraction
    /// records each carry a <c>pdfUrl</c> — an absolute link to the rendered PDF. Query incrementally
    /// with <c>ModifiedSince</c>, then download each non-null <c>pdfUrl</c>.
    /// <c>pdfUrl</c> is null until the PDF has been generated (on submit/report), so skip nulls and
    /// pick them up on a later sync.
    /// </summary>
    private static async Task PullGeneratedPdfsAsync(DitioApiClient api)
    {
        // Only fetch records changed since your last sync (ISO-8601, UTC).
        var modifiedSince = "2026-06-01T00:00:00Z";
        var downloadDir = Directory.CreateDirectory("pdf-downloads").FullName;

        foreach (var (name, path) in PdfEndpoints)
        {
            Console.WriteLine($"\n--- {name} ({path}?ModifiedSince={modifiedSince}) ---");

            var page = await api.GetAsync($"{path}?ModifiedSince={Uri.EscapeDataString(modifiedSince)}");
            if (page?.data == null)
                continue;

            foreach (var record in page.data)
            {
                string? pdfUrl = record.pdfUrl;
                if (string.IsNullOrEmpty(pdfUrl))
                    continue; // PDF not generated yet — it will appear on a later sync.

                string id = record.id;
                var destination = Path.Combine(downloadDir, $"{name.Replace(" ", "-")}-{id}.pdf");
                await api.DownloadUrlAsync(pdfUrl, destination);
            }
        }
    }
}
