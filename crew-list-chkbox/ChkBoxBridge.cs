namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>What one poll cycle did. Returned so a host can log or alert on it.</summary>
public sealed record CycleResult
{
    public int RegistrationsRead { get; init; }
    public int Posted { get; init; }
    public int SkippedAlreadyInChkBox { get; init; }
    public int SkippedNoCardInDitio { get; init; }
    public int SkippedUnknownToChkBox { get; init; }
    public int SkippedUnmappedProject { get; init; }
    public int Failed { get; init; }

    /// <summary>False when the source could not be read at all — nothing was processed.</summary>
    public bool SourceRead { get; init; } = true;
}

/// <summary>
/// The bridge itself: one poll cycle of Ditio crew list → ChkBox registrations.
///
/// Kept separate from the console example so it can be driven by whatever host you deploy it in
/// (a BackgroundService, a cron job, an Azure Function) and exercised in tests.
/// </summary>
public sealed class ChkBoxBridge(
    ICrewSource source,
    ChkBoxClient chkBox,
    ChkBoxBridgeConfig config,
    BridgeState state)
{
    private readonly Dictionary<string, ProjectMapping> _mappingsByDitioProject =
        config.Projects.ToDictionary(p => p.DitioProjectId, StringComparer.Ordinal);

    public async Task<CycleResult> RunCycleAsync(CancellationToken cancellationToken)
    {
        CrewFetchResult fetched;
        try
        {
            fetched = await source.FetchAsync(state.Cursor, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Leave the cursor untouched so the next cycle re-reads the same window.
            Console.WriteLine($"  ✗ Could not read from Ditio: {ex.Message}");
            return new CycleResult { SourceRead = false };
        }

        var pending = new List<Passage>();
        var noCardInDitio = 0;
        var unmappedProject = 0;

        foreach (var registration in fetched.Registrations)
        {
            if (!_mappingsByDitioProject.ContainsKey(registration.DitioProjectId))
            {
                unmappedProject++;
                continue;
            }

            if (string.IsNullOrEmpty(registration.HseCardId))
            {
                // No byggekort on file in Ditio — nothing to match against in ChkBox.
                noCardInDitio++;
                continue;
            }

            foreach (var passage in ToPassages(registration))
            {
                if (!state.PostedPassages.ContainsKey(passage.DedupeKey))
                    pending.Add(passage);
            }
        }

        Console.WriteLine($"  Read {fetched.Registrations.Count} registration(s); {pending.Count} new passage(s) to consider.");
        if (noCardInDitio > 0)
            Console.WriteLine($"  ⓘ {noCardInDitio} skipped — no HSE card number registered in Ditio.");
        if (unmappedProject > 0)
            Console.WriteLine($"  ⓘ {unmappedProject} skipped — project not in ChkBox:Projects.");

        if (pending.Count == 0)
        {
            Commit(fetched.NextCursor);
            return new CycleResult
            {
                RegistrationsRead = fetched.Registrations.Count,
                SkippedNoCardInDitio = noCardInDitio,
                SkippedUnmappedProject = unmappedProject,
            };
        }

        // Safety net: ChkBox has no idempotency key on POST, so check what is already there before
        // writing. This is what stops a lost state file from producing duplicate registrations.
        var alreadyInChkBox = await LoadExistingAsync(pending, cancellationToken);

        var posted = 0;
        var duplicates = 0;
        var unknownCard = 0;
        var failed = 0;

        foreach (var passage in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mapping = _mappingsByDitioProject[passage.DitioProjectId];

            if (IsAlreadyRegistered(alreadyInChkBox, mapping.ChkBoxProjectId, passage))
            {
                // Remember it so we stop re-checking this passage on every future cycle.
                state.PostedPassages[passage.DedupeKey] = DateTimeOffset.UtcNow;
                duplicates++;
                continue;
            }

            string? cardResourceId;
            try
            {
                cardResourceId = await ResolveCardAsync(passage.HseCardId, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Card lookup failed for {Redact.Card(passage.HseCardId)}: {ex.Message}");
                failed++;
                continue;
            }

            if (cardResourceId is null)
            {
                // ChkBox only knows cards registered or pre-approved somewhere in the tenant, so
                // the project owner has to add this worker before their passages can be bridged.
                Console.WriteLine($"  ⓘ Card {Redact.Card(passage.HseCardId)} is unknown to ChkBox — ask the project owner to add it.");
                unknownCard++;
                continue;
            }

            if (config.DryRun)
            {
                Console.WriteLine($"  [dry run] {passage.ActionValue,-3} {ChkBoxClient.FormatTime(passage.Time)} "
                                  + $"card {Redact.Card(passage.HseCardId)} → {mapping.ChkBoxProjectId}");
                posted++;
                continue;
            }

            try
            {
                await chkBox.PostRegistrationAsync(
                    mapping.ChkBoxProjectId, cardResourceId, passage.ActionValue, passage.Time, cancellationToken);

                // Record immediately — if a later passage fails, this one must not be re-posted.
                state.PostedPassages[passage.DedupeKey] = DateTimeOffset.UtcNow;
                posted++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed to post {passage.ActionValue} for card {Redact.Card(passage.HseCardId)}: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine(config.DryRun
            ? $"  Would post {posted}; {duplicates} already in ChkBox; {unknownCard} unknown card(s); {failed} error(s)."
            : $"  Posted {posted}; {duplicates} already in ChkBox; {unknownCard} unknown card(s); {failed} error(s).");

        if (failed == 0)
        {
            Commit(fetched.NextCursor);
        }
        else
        {
            Console.WriteLine("  ⓘ Cursor held back because of errors — the next cycle will retry.");
            // Still persist, so passages that DID succeed are never posted twice.
            Commit(cursor: null);
        }

        return new CycleResult
        {
            RegistrationsRead = fetched.Registrations.Count,
            Posted = posted,
            SkippedAlreadyInChkBox = duplicates,
            SkippedNoCardInDitio = noCardInDitio,
            SkippedUnknownToChkBox = unknownCard,
            SkippedUnmappedProject = unmappedProject,
            Failed = failed,
        };
    }

    /// <summary>Persists state, advancing the cursor only when a cursor is supplied.</summary>
    private void Commit(string? cursor)
    {
        if (cursor is not null)
            state.Cursor = cursor;

        state.PrunePostedPassages(TimeSpan.FromDays(config.PostedPassageRetentionDays));
        state.Save(config.StateFilePath);
    }

    private static IEnumerable<Passage> ToPassages(CrewRegistration registration)
    {
        yield return new Passage
        {
            RegistrationId = registration.Id,
            Action = PassageAction.In,
            Time = registration.CheckInDateTime,
            HseCardId = registration.HseCardId!,
            DitioProjectId = registration.DitioProjectId,
        };

        // The check-out only exists once the person has left; while the stay is open we
        // deliberately post only the `in`, and pick up the `out` on a later cycle.
        if (registration.CheckOutDateTime is { } checkOut)
        {
            yield return new Passage
            {
                RegistrationId = registration.Id,
                Action = PassageAction.Out,
                Time = checkOut,
                HseCardId = registration.HseCardId!,
                DitioProjectId = registration.DitioProjectId,
            };
        }
    }

    private async Task<Dictionary<string, IReadOnlyList<ExistingRegistration>>> LoadExistingAsync(
        IReadOnlyList<Passage> pending,
        CancellationToken cancellationToken)
    {
        var existing = new Dictionary<string, IReadOnlyList<ExistingRegistration>>(StringComparer.Ordinal);

        var byChkBoxProject = pending.GroupBy(p => _mappingsByDitioProject[p.DitioProjectId].ChkBoxProjectId, StringComparer.Ordinal);

        foreach (var group in byChkBoxProject)
        {
            // Look back from the earliest passage we are about to post for this project.
            var earliest = group.Min(p => p.Time);

            try
            {
                existing[group.Key] = await chkBox.GetRegistrationsAsync(group.Key, earliest.AddMinutes(-5), cancellationToken);
            }
            catch (Exception ex)
            {
                // Reconciliation is the safety net, not the primary guard — local state still
                // protects us. Warn and continue rather than stalling the cycle.
                Console.WriteLine($"  ⚠ Could not read existing registrations for {group.Key}: {ex.Message}");
                existing[group.Key] = [];
            }
        }

        return existing;
    }

    /// <summary>
    /// Matches on card + action + time within a minute. Exact-second equality is too brittle:
    /// project admins can adjust passage times in ChkBox after the fact.
    /// </summary>
    private static bool IsAlreadyRegistered(
        Dictionary<string, IReadOnlyList<ExistingRegistration>> existing,
        string chkBoxProjectId,
        Passage passage)
    {
        if (!existing.TryGetValue(chkBoxProjectId, out var registrations))
            return false;

        return registrations.Any(r =>
            string.Equals(r.CardId, passage.HseCardId, StringComparison.Ordinal)
            && string.Equals(r.Action, passage.ActionValue, StringComparison.OrdinalIgnoreCase)
            && Math.Abs((r.Time - passage.Time).TotalSeconds) < 60);
    }

    private async Task<string?> ResolveCardAsync(string hseCardId, CancellationToken cancellationToken)
    {
        if (state.CardResourceIds.TryGetValue(hseCardId, out var cached))
            return cached;

        var resourceId = await chkBox.ResolveCardResourceIdAsync(hseCardId, cancellationToken);
        if (resourceId is not null)
            state.CardResourceIds[hseCardId] = resourceId;

        return resourceId;
    }
}
