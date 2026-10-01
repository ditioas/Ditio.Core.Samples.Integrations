using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ditio.Samples.Examples;

using Ditio.Samples.Examples.DataExtractionSync;

/// <summary>
/// Keeps a local database in step with Ditio through the Data Extraction API (<c>v1/*</c>), so
/// Power BI (or anything else) reads a local copy instead of pulling every record on every
/// refresh.
///
/// The first run of an endpoint is a full sync. Every later run asks only for what changed since
/// the previous run started (<c>ModifiedSince</c>), follows <c>continuationToken</c> to the last
/// page, upserts changed records and flags deleted ones. Each endpoint is synced in one
/// transaction, and its watermark only moves when the endpoint completed — a failed run is simply
/// retried from the old watermark next time.
///
/// Run it on a schedule (cron, Windows Task Scheduler). See <c>data-extraction-sync/README.md</c>.
/// </summary>
public static class IncrementalSyncExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        if (string.IsNullOrEmpty(cfg.ClientId) || string.IsNullOrEmpty(cfg.ClientSecret))
        {
            Console.WriteLine("⚠ Nothing synced: no API client is configured.");
            Console.WriteLine("  Set Ditio:ClientId and Ditio:ClientSecret in appsettings.json (or DITIO_Ditio__ClientId /");
            Console.WriteLine("  DITIO_Ditio__ClientSecret). The client needs the reportingapiv1 scope.");
            Environment.ExitCode = 1;
            return;
        }

        var settings = IncrementalSyncConfig.Load();
        var endpoints = settings.SelectEndpoints(out var unknown);
        if (unknown.Count > 0)
        {
            Console.WriteLine($"⚠ Unknown endpoint(s) in DataExtractionSync:Endpoints: {string.Join(", ", unknown)}");
            Console.WriteLine($"  Known: {string.Join(", ", SyncEndpoint.All.Select(e => e.Path))}");
            Environment.ExitCode = 1;
            return;
        }

        var api = new DitioApiClient(cfg.ReportingBaseUrl, cfg.ReportingScope, new DitioTokenProvider(cfg));
        await using var store = await SyncStore.Open(settings.DatabasePath);

        Console.WriteLine($"Reporting API : {cfg.ReportingBaseUrl}");
        Console.WriteLine($"Database      : {Path.GetFullPath(settings.DatabasePath)}");
        Console.WriteLine($"Endpoints     : {endpoints.Count}");

        var failed = new List<string>();
        foreach (var endpoint in endpoints)
        {
            try
            {
                await SyncOneEndpoint(api, store, endpoint, settings);
            }
            catch (Exception ex)
            {
                // One endpoint failing must not stop the others. Its transaction was rolled back
                // and its watermark left alone, so the next run retries the same window.
                failed.Add(endpoint.Path);
                Console.WriteLine($"✗ {endpoint.Path} failed, will retry from the previous watermark next run: {ex.Message}");
            }
        }

        Console.WriteLine();
        if (failed.Count > 0)
        {
            Console.WriteLine($"Done with errors: {failed.Count} of {endpoints.Count} endpoint(s) failed ({string.Join(", ", failed)}).");
            Environment.ExitCode = 1; // so a scheduler can alert on it
            return;
        }

        Console.WriteLine($"Done: {endpoints.Count} endpoint(s) in sync.");
    }

    private static async Task SyncOneEndpoint(DitioApiClient api, SyncStore store, SyncEndpoint endpoint, IncrementalSyncConfig settings)
    {
        await store.EnsureTable(endpoint);

        // Take the start time BEFORE the first request. Anything changed while we page through is
        // either in this run's pages or after this timestamp, so the next run catches it.
        var startedUtc = DateTime.UtcNow;

        var state = await store.GetState(endpoint);
        var fullSync = state is null || settings.ForceFullSync;

        // The overlap re-reads the last few minutes of the previous run. It absorbs clock skew
        // between this machine and Ditio and any write that landed while the previous run was
        // reading. Upserts make the re-read records harmless.
        string? modifiedSince = fullSync
            ? null
            : SyncStore.FormatUtc(state!.Value.LastSyncStartedUtc.AddMinutes(-settings.OverlapMinutes));

        Console.WriteLine();
        Console.WriteLine(fullSync
            ? $"--- {endpoint.Path}: full sync ---"
            : $"--- {endpoint.Path}: changes since {modifiedSince} ---");

        await using var transaction = await store.BeginTransaction();
        if (fullSync)
            await store.Clear(transaction, endpoint);

        int upserted = 0, deleted = 0, pages = 0;
        string? continuationToken = null;
        do
        {
            var page = await GetPage(api, endpoint, modifiedSince, continuationToken, settings.ChunkLimit);
            pages++;

            foreach (var record in page["data"] as JArray ?? [])
            {
                if (record is not JObject item || item.Value<string>("id") is not { Length: > 0 } id)
                    continue;

                var modified = item.Value<string>("modifiedDateTime");
                var json = item.ToString(Formatting.None);

                if (item.Value<bool?>("isDeleted") == true)
                {
                    // Deleted projects, work orders, resources and users carry their last version,
                    // whose modifiedDateTime predates the deletion; deletedDateTime is the real "when".
                    await store.MarkDeleted(transaction, endpoint, id, item.Value<string>("deletedDateTime") ?? modified, json);
                    deleted++;
                }
                else
                {
                    await store.Upsert(transaction, endpoint, id, modified, json);
                    upserted++;
                }
            }

            continuationToken = page.Value<string>("continuationToken");
        } while (!string.IsNullOrEmpty(continuationToken));

        // The watermark is the last write of the transaction: it moves only if every page landed.
        await store.SaveState(transaction, endpoint, startedUtc, fullSync);
        await transaction.CommitAsync();

        Console.WriteLine($"✓ {endpoint.Path}: {upserted} upserted, {deleted} deleted, {pages} page(s)");
    }

    /// <summary>
    /// One page. In incremental mode EVERY page must repeat <c>ModifiedSince</c>: the API decides
    /// full vs incremental from it, and reads the continuation token accordingly (a modified-time
    /// watermark when incremental; a work date, load date or id when full).
    /// </summary>
    private static async Task<JObject> GetPage(DitioApiClient api, SyncEndpoint endpoint, string? modifiedSince, string? continuationToken, int chunkLimit)
    {
        var query = new List<string> { $"ChunkLimit={chunkLimit}" };
        if (modifiedSince is not null)
            query.Add($"ModifiedSince={Uri.EscapeDataString(modifiedSince)}");
        if (!string.IsNullOrEmpty(continuationToken))
            query.Add($"ContinuationToken={Uri.EscapeDataString(continuationToken)}");

        var raw = await api.GetRawAsync($"{endpoint.Path}?{string.Join("&", query)}");

        // Keep timestamps as the strings the API sent: Newtonsoft would otherwise parse them into
        // DateTime and write them back in a different format.
        return JsonConvert.DeserializeObject<JObject>(raw, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None })
               ?? throw new InvalidOperationException($"{endpoint.Path} returned an empty body.");
    }
}
