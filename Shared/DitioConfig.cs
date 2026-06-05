using Microsoft.Extensions.Configuration;

namespace Ditio.Samples;

/// <summary>
/// Connection settings for the samples. Defaults point at <b>production</b>; override them in
/// <c>appsettings.json</c> (copy <c>appsettings.example.json</c>) or via <c>DITIO_Ditio__*</c>
/// environment variables.
/// </summary>
public sealed class DitioConfig
{
    /// <summary>Identity Server. Production: identity.ditio.app · Test: identity.ditio.dev</summary>
    public string IdentityUrl { get; set; } = "https://identity.ditio.app";

    /// <summary>Integration API base (api/v4, api/v5). Production: integration.ditio.no · Test: core-api.ditio.dev/core</summary>
    public string BaseUrl { get; set; } = "https://integration.ditio.no";

    /// <summary>Reporting / data-extraction API base (v1/*). Production: core-api.ditio.app/core · Test: core-api.ditio.dev/core</summary>
    public string ReportingBaseUrl { get; set; } = "https://core-api.ditio.app/core";

    /// <summary>Scope for the integration + core API.</summary>
    public string Scope { get; set; } = "ditioapiv3";

    /// <summary>Scope for the reporting / data-extraction API.</summary>
    public string ReportingScope { get; set; } = "reportingapiv1";

    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string CompanyId { get; set; } = "";

    public static DitioConfig Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables("DITIO_")
            .Build();

        var settings = new DitioConfig();
        configuration.GetSection("Ditio").Bind(settings);
        return settings;
    }
}
