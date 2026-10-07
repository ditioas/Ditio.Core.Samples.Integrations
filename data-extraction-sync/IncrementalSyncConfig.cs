using Microsoft.Extensions.Configuration;

namespace Ditio.Samples.Examples.DataExtractionSync;

/// <summary>
/// Settings for the sync. Loaded from the <c>DataExtractionSync</c> section of
/// <c>appsettings.json</c> or <c>DITIO_DataExtractionSync__*</c> environment variables. All optional.
/// </summary>
internal sealed class IncrementalSyncConfig
{
    /// <summary>SQLite file to sync into. Created on the first run.</summary>
    public string DatabasePath { get; set; } = "ditio-sync.db";

    /// <summary>Endpoint paths to sync (e.g. <c>v1/time-registrations</c>). Empty = all of <see cref="SyncEndpoint.All"/>.</summary>
    public List<string> Endpoints { get; set; } = [];

    /// <summary>How far before the previous run's start the next run reads from. See the README.</summary>
    public int OverlapMinutes { get; set; } = 10;

    /// <summary>Records per page. The API caps it at 10000, and may return a few more to keep equal timestamps on one page.</summary>
    public int ChunkLimit { get; set; } = 5000;

    /// <summary>Ignore the stored watermarks once and re-read everything (each table is replaced).</summary>
    public bool ForceFullSync { get; set; }

    public static IncrementalSyncConfig Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables("DITIO_")
            .Build();

        var settings = new IncrementalSyncConfig();
        configuration.GetSection("DataExtractionSync").Bind(settings);
        return settings;
    }

    public List<SyncEndpoint> SelectEndpoints(out List<string> unknown)
    {
        unknown = [];
        if (Endpoints.Count == 0)
            return [.. SyncEndpoint.All];

        var selected = new List<SyncEndpoint>();
        foreach (var path in Endpoints)
        {
            var endpoint = SyncEndpoint.All.FirstOrDefault(e => e.Path.Equals(path.Trim().TrimStart('/'), StringComparison.OrdinalIgnoreCase));
            if (endpoint is null)
                unknown.Add(path);
            else
                selected.Add(endpoint);
        }

        return selected;
    }
}
