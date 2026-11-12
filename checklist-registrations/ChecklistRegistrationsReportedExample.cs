namespace Ditio.Samples.Examples;

/// <summary>
/// Read approved checklist registrations from the Data Extraction API, process them in your own system, and
/// mark them as reported in Ditio via api/integration/checklist-registrations/reported, the same step an
/// approver takes in the Ditio app.
/// </summary>
public static class ChecklistRegistrationsReportedExample
{
    private const int MaxIdsPerCall = 100;

    public static async Task RunAsync(DitioConfig cfg)
    {
        // Two APIs, two base URLs, two scopes: extraction reads from the reporting API, the status change
        // goes to the Core integration API.
        var reporting = new DitioApiClient(cfg.ReportingBaseUrl, cfg.ReportingScope, new DitioTokenProvider(cfg));
        var core = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // 1. Read registrations changed in the last week. In a real sync, store the time each run started and
        //    pass it as ModifiedSince on the next run (see ../data-extraction-sync).
        var modifiedSince = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var approvedIds = new List<string>();
        string? token = null;
        do
        {
            var path = $"v1/checklist-registrations?ModifiedSince={Uri.EscapeDataString(modifiedSince)}&ChunkLimit=500";
            if (!string.IsNullOrEmpty(token))
                path += $"&ContinuationToken={Uri.EscapeDataString(token)}";

            var page = await reporting.GetAsync(path);
            foreach (var registration in page?.data ?? Enumerable.Empty<dynamic>())
            {
                // Pick up Approved only. A registration you mark as reported comes back once more in the next
                // incremental pull, with status Reported, so filtering here keeps it from being processed twice.
                if ((string?)registration.statusText == "Approved" && registration.isDeleted != true)
                    approvedIds.Add((string)registration.id);
            }

            // Take the token from the top level of the response. meta.continuationToken only echoes the
            // token you sent, so paging on it repeats the last page forever.
            token = (string?)page?.continuationToken;
        } while (!string.IsNullOrEmpty(token));

        Console.WriteLine($"Approved registrations to process: {approvedIds.Count}");

        // 2. Process them in your system here. Mark only the ones you actually stored.

        // 3. Mark them as reported, up to 100 ids per call. Each id gets its own result, so one id that cannot
        //    be reported never blocks the rest. Resending a call is safe: ids already done return
        //    alreadyReported, so after a 5xx send the same batch again.
        foreach (var batch in approvedIds.Chunk(MaxIdsPerCall))
        {
            var results = await core.PostAsync("api/integration/checklist-registrations/reported", new { ids = batch });
            foreach (var result in results ?? Enumerable.Empty<dynamic>())
            {
                // reported | alreadyReported | notApproved | notPermitted | notFound
                Console.WriteLine($"{result.id}: {result.result}");
            }
        }
    }
}
