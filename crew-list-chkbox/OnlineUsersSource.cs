using Newtonsoft.Json.Linq;

namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>
/// Reads the crew list from <c>GET api/v3/onlineusers/activeonly</c> — the same data behind the
/// "Mannskapsliste" screen and Excel export in the Ditio backoffice.
///
/// This source works against Ditio today, but understand its limits before relying on it:
///
/// <list type="bullet">
/// <item>It is a <b>snapshot, not a delta</b> — there is no cursor, no pagination and no
/// changed-since filter, so every poll re-reads the whole day window and the bridge diffs
/// client-side.</item>
/// <item>It is <b>aggregated per person per day</b>: <c>startTime</c> is the first check-in of the
/// day and <c>stopTime</c> the last check-out. If someone leaves and returns, the middle passages
/// are not visible here and will not reach ChkBox.</item>
/// <item>Times come back <b>without a UTC offset</b>, in the calling user's configured time zone —
/// hence the <c>ChkBox:TimeZone</c> setting.</item>
/// </list>
///
/// Use <see cref="CrewListRegistrationsSource"/> instead once the public data-extraction endpoint
/// is available; it is passage-level, delta-capable and returns UTC.
/// </summary>
internal sealed class OnlineUsersSource(
    DitioReadClient client,
    IReadOnlyList<ProjectMapping> projects,
    TimeZoneInfo timeZone,
    int backfillHours) : ICrewSource
{
    public string Name => "online-users (api/v3/onlineusers/activeonly)";

    public async Task<CrewFetchResult> FetchAsync(string? cursor, CancellationToken cancellationToken)
    {
        // No delta support on this endpoint, so the cursor is ignored: we always re-read a date
        // window. Default is today, matching the crew list's own default when dates are omitted.
        var today = DateTime.UtcNow.Date;
        var from = backfillHours > 0 ? today.AddHours(-backfillHours).Date : today;

        // `toDateTime` is the START of that day, so the end date itself is excluded — pass
        // tomorrow to include today. (The Excel endpoint is end-inclusive instead. Yes, really.)
        var to = today.AddDays(1);

        var projectIds = string.Join(",", projects.Select(p => p.DitioProjectId));
        var path = $"api/v3/onlineusers/activeonly?fromDateTime={from:yyyy-MM-dd}&toDateTime={to:yyyy-MM-dd}"
                   + $"&projectIds={Uri.EscapeDataString(projectIds)}";

        var response = await client.GetAsync(path, cancellationToken);
        if (response is not JArray rows)
            return new CrewFetchResult([], cursor);

        var registrations = new List<CrewRegistration>();
        foreach (var row in rows)
        {
            var registration = MapRow(row);
            if (registration is not null)
                registrations.Add(registration);
        }

        return new CrewFetchResult(registrations, cursor);
    }

    private CrewRegistration? MapRow(JToken row)
    {
        var userId = row["userId"]?.Value<string>();
        var projectId = row["projectId"]?.Value<string>();

        // Roster entries for people who never checked in come back with an empty project and
        // zeroed times. /activeonly should not return them, but guard anyway.
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(projectId))
            return null;

        var checkIn = ToOffset(row["startTime"]?.Value<DateTime?>());
        if (checkIn is null)
            return null;

        // hasTransaction == true means the check-in is still open (person is on site).
        var stillOnSite = row["hasTransaction"]?.Value<bool?>() ?? false;
        var checkOut = stillOnSite ? null : ToOffset(row["stopTime"]?.Value<DateTime?>());

        return new CrewRegistration
        {
            // There is no transaction id on this endpoint, so synthesise a stable one. It must not
            // change between polls or the bridge would re-post the passage as new.
            Id = $"{userId}:{projectId}:{checkIn.Value:yyyy-MM-dd}",
            DitioProjectId = projectId,
            DitioProjectNumber = row["projectNumber"]?.Value<string>(),
            HseCardId = HseCard.Normalize(row["cardId"]?.Value<string>()),
            CheckInDateTime = checkIn.Value,
            CheckOutDateTime = checkOut,
        };
    }

    /// <summary>
    /// Attaches the correct offset to a naive timestamp. Ditio returns these in the calling user's
    /// time zone with no offset, and ChkBox would otherwise assume Central European time — which
    /// silently shifts every passage for anyone running outside Norway.
    /// </summary>
    private DateTimeOffset? ToOffset(DateTime? value)
    {
        // Unset times come back as DateTime.MinValue (0001-01-01).
        if (value is null || value.Value.Year <= 1)
            return null;

        var naive = DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);
        return new DateTimeOffset(naive, timeZone.GetUtcOffset(naive));
    }
}
