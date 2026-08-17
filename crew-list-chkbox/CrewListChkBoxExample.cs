namespace Ditio.Samples.Examples;

using Ditio.Samples.Examples.CrewListChkBox;

/// <summary>
/// Bridges Ditio's crew list (mannskapsliste) into Infotech's ChkBox / byggekortleser API, so a
/// site's check-in and check-out passages reach the project owner's HSE register without anyone
/// having to register twice.
///
/// It polls Ditio on an interval, works out which passages are new, resolves each worker's HSE
/// card (byggekort) to a ChkBox card, and posts <c>in</c>/<c>out</c> registrations.
///
/// Ditio does not run this for you — it is a starting point to deploy and extend.
/// See <c>crew-list-chkbox/README.md</c>.
/// </summary>
public static class CrewListChkBoxExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var config = ChkBoxBridgeConfig.Load();

        var problems = config.Validate();
        if (problems.Count > 0)
        {
            Console.WriteLine("⚠ The ChkBox bridge is not configured yet:");
            foreach (var problem in problems)
                Console.WriteLine($"  • {problem}");
            Console.WriteLine("\n  Add a \"ChkBox\" section to appsettings.json — see crew-list-chkbox/README.md.");
            return;
        }

        config.TryGetTimeZone(out var timeZone);

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Console.WriteLine("\nStopping after the current cycle…");
            cancellation.Cancel();
        };

        var tokens = new DitioTokenProvider(cfg);
        var chkBox = new ChkBoxClient(config.BaseUrl, config.ApiKey);

        // The two crew sources live on different hosts, with different scopes.
        var useExtractionApi = config.Source.Equals("crew-list-registrations", StringComparison.OrdinalIgnoreCase);
        var ditio = useExtractionApi
            ? new DitioReadClient(cfg.ReportingBaseUrl, cfg.ReportingScope, tokens)
            : new DitioReadClient(cfg.BaseUrl, cfg.Scope, tokens);

        ICrewSource source = useExtractionApi
            ? new CrewListRegistrationsSource(ditio, config.Projects, config.BackfillHours)
            : new OnlineUsersSource(ditio, config.Projects, timeZone, config.BackfillHours);

        Console.WriteLine($"Source     : {source.Name}");
        Console.WriteLine($"ChkBox     : {config.BaseUrl}");
        Console.WriteLine($"Mode       : {(config.DryRun ? "DRY RUN — nothing will be written to ChkBox" : "LIVE — passages will be posted")}");
        Console.WriteLine($"Projects   : {config.Projects.Count} mapped");
        Console.WriteLine($"State file : {Path.GetFullPath(config.StateFilePath)}");
        Console.WriteLine();

        if (!await VerifyProjectMappingsAsync(chkBox, config.Projects, cancellation.Token))
            return;

        var state = BridgeState.Load(config.StateFilePath);
        var bridge = new ChkBoxBridge(source, chkBox, config, state);

        if (config.RunOnce)
        {
            Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Polling…");
            await bridge.RunCycleAsync(cancellation.Token);
            return;
        }

        Console.WriteLine($"Polling every {config.PollIntervalMinutes} min. Press Ctrl+C to stop.\n");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(config.PollIntervalMinutes));

        try
        {
            do
            {
                Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Polling…");
                await bridge.RunCycleAsync(cancellation.Token);
            } while (await timer.WaitForNextTickAsync(cancellation.Token));
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Stopped.");
        }
    }

    /// <summary>
    /// Checks every configured ChkBox project id resolves. A typo here would otherwise surface as a
    /// confusing failure on the first POST, after the bridge has already done a day's work.
    /// </summary>
    private static async Task<bool> VerifyProjectMappingsAsync(
        ChkBoxClient chkBox,
        IReadOnlyList<ProjectMapping> projects,
        CancellationToken cancellationToken)
    {
        var allResolved = true;

        foreach (var mapping in projects)
        {
            var description = await chkBox.GetProjectDescriptionAsync(mapping.ChkBoxProjectId, cancellationToken);
            if (description is null)
            {
                Console.WriteLine($"✗ ChkBox project '{mapping.ChkBoxProjectId}' not found — check ChkBox:Projects.");
                allResolved = false;
            }
            else
            {
                var ditioLabel = mapping.DitioProjectNumber ?? mapping.DitioProjectId;
                Console.WriteLine($"✓ Ditio {ditioLabel} → ChkBox {mapping.ChkBoxProjectId} ({description})");
            }
        }

        Console.WriteLine();
        return allResolved;
    }
}
