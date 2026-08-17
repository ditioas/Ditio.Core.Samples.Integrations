using Microsoft.Extensions.Configuration;

namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>Maps one Ditio project onto one ChkBox project. There is no reliable automatic mapping.</summary>
public sealed class ProjectMapping
{
    /// <summary>Ditio project id (the GUID-like id, not the project number).</summary>
    public string DitioProjectId { get; set; } = "";

    /// <summary>Optional — only used to make log output readable.</summary>
    public string? DitioProjectNumber { get; set; }

    /// <summary>ChkBox project resource id, e.g. <c>SKK180039</c>. Ask the project owner for this.</summary>
    public string ChkBoxProjectId { get; set; } = "";
}

/// <summary>
/// Settings for the crew-list → ChkBox bridge. Loaded from the <c>ChkBox</c> section of
/// <c>appsettings.json</c> (git-ignored) or <c>DITIO_ChkBox__*</c> environment variables.
///
/// The ChkBox API key is a secret belonging to the project owner — never commit it.
/// </summary>
public sealed class ChkBoxBridgeConfig
{
    /// <summary>Production: https://api.byggekortleser.no · Development/testing: https://devapi.byggekortleser.no</summary>
    public string BaseUrl { get; set; } = "https://devapi.byggekortleser.no";

    /// <summary>API key issued by Infotech. Test and production use separate keys.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// When true (the default) nothing is written to ChkBox — the bridge prints the passages it
    /// would post. Run this way first and check the output before enabling live posting.
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>How often to poll. Ditio's crew data is not real-time enough to warrant less than this.</summary>
    public int PollIntervalMinutes { get; set; } = 15;

    /// <summary>Run one cycle and exit, instead of looping. Useful under an external scheduler (cron, k8s CronJob).</summary>
    public bool RunOnce { get; set; }

    /// <summary>
    /// Which crew source to read. <c>crew-list-registrations</c> is the public data-extraction
    /// endpoint and the one to use; <c>online-users</c> is the older backoffice crew list, kept for
    /// anyone already on it. See the README.
    /// </summary>
    public string Source { get; set; } = "crew-list-registrations";

    /// <summary>
    /// Time zone the online-users source's naive timestamps are in — that endpoint returns times in
    /// the calling user's configured zone with no offset. Ignored by the crew-list-registrations
    /// source, which returns UTC.
    /// </summary>
    public string TimeZone { get; set; } = "Europe/Oslo";

    /// <summary>Where the delta cursor and posted-passage set live. Must be durable across restarts.</summary>
    public string StateFilePath { get; set; } = "chkbox-bridge-state.json";

    /// <summary>
    /// How far back the first run reads when there is no stored cursor. 0 means "today only",
    /// matching the crew list's own default. Raise it once, for a backfill, then set it back.
    /// </summary>
    public int BackfillHours { get; set; }

    /// <summary>
    /// How long a posted passage is remembered. Must comfortably exceed how late the source can
    /// restate a registration, or an edited record could be posted a second time.
    /// </summary>
    public int PostedPassageRetentionDays { get; set; } = 30;

    /// <summary>Ditio project -> ChkBox project. Only mapped projects are bridged.</summary>
    public List<ProjectMapping> Projects { get; set; } = [];

    public static ChkBoxBridgeConfig Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables("DITIO_")
            .Build();

        var settings = new ChkBoxBridgeConfig();
        configuration.GetSection("ChkBox").Bind(settings);
        return settings;
    }

    /// <summary>Returns the problems that make this config unusable, so we can report them all at once.</summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(ApiKey))
            problems.Add("ChkBox:ApiKey is not set — request one from support@infotech.no (test and production keys differ).");

        if (Projects.Count == 0)
            problems.Add("ChkBox:Projects is empty — map at least one Ditio project to a ChkBox project id.");

        foreach (var mapping in Projects)
        {
            if (string.IsNullOrWhiteSpace(mapping.DitioProjectId) || string.IsNullOrWhiteSpace(mapping.ChkBoxProjectId))
                problems.Add("Every entry in ChkBox:Projects needs both DitioProjectId and ChkBoxProjectId.");
        }

        if (PollIntervalMinutes < 1)
            problems.Add("ChkBox:PollIntervalMinutes must be at least 1.");

        if (!TryGetTimeZone(out _))
            problems.Add($"ChkBox:TimeZone '{TimeZone}' was not recognised on this machine.");

        return problems;
    }

    /// <summary>
    /// Resolves the configured zone. Windows and Linux disagree on time zone ids, so fall back to
    /// the Windows name if the IANA one is not found.
    /// </summary>
    public bool TryGetTimeZone(out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                return true;
            }
            catch (Exception fallbackFailure) when (fallbackFailure is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                timeZone = TimeZoneInfo.Utc;
                return false;
            }
        }
    }
}
