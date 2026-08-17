using System.Net;
using Newtonsoft.Json.Linq;

namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>
/// Reads the crew list from the public data-extraction endpoint
/// <c>GET v1/crew-list-registrations</c> on the reporting host (scope <c>reportingapiv1</c>).
///
/// This is the source to prefer. Unlike the backoffice crew-list endpoint it is passage-level
/// (one record per check-in/check-out, so nothing is lost when someone leaves and returns),
/// returns UTC, and supports proper incremental sync via <c>ModifiedSince</c> +
/// <c>ContinuationToken</c> with <c>isDeleted</c> tombstones for retroactive edits.
///
/// Date defaulting matches the crew list: with no parameters you get today; supply
/// <c>FromDateTime</c>/<c>ToDateTime</c> for history; supply <c>ModifiedSince</c> to sync deltas.
/// </summary>
internal sealed class CrewListRegistrationsSource(
    DitioReadClient client,
    IReadOnlyList<ProjectMapping> projects,
    int backfillHours) : ICrewSource
{
    public string Name => "crew-list-registrations (v1/crew-list-registrations)";

    public async Task<CrewFetchResult> FetchAsync(string? cursor, CancellationToken cancellationToken)
    {
        var registrations = new List<CrewRegistration>();
        var projectIds = string.Join(",", projects.Select(p => p.DitioProjectId));

        var query = $"ProjectIds={Uri.EscapeDataString(projectIds)}&IncludeOpen=true";

        if (!string.IsNullOrEmpty(cursor))
        {
            // Delta mode: everything changed since the last successful poll. No date window — a
            // registration edited today may well have a check-in from last week.
            query += $"&ModifiedSince={Uri.EscapeDataString(cursor)}";
        }
        else if (backfillHours > 0)
        {
            var from = DateTime.UtcNow.AddHours(-backfillHours);
            query += $"&FromDateTime={Uri.EscapeDataString(from.ToString("yyyy-MM-ddTHH:mm:ssZ"))}";
        }
        // else: no cursor and no backfill -> the endpoint defaults to today, which is what we want.

        // Record the time BEFORE reading, so anything modified mid-poll is picked up next cycle
        // rather than skipped.
        var nextCursor = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string? continuationToken = null;

        do
        {
            var path = continuationToken is null
                ? $"v1/crew-list-registrations?{query}"
                : $"v1/crew-list-registrations?{query}&ContinuationToken={Uri.EscapeDataString(continuationToken)}";

            JToken response;
            try
            {
                response = await client.GetAsync(path, cancellationToken);
            }
            catch (DitioReadException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new DitioReadException(
                    "v1/crew-list-registrations is not available on this environment yet. "
                    + "Set ChkBox:Source to \"online-users\" to use the existing crew-list endpoint instead.",
                    HttpStatusCode.NotFound);
            }

            if (response["data"] is JArray data)
            {
                foreach (var record in data)
                {
                    var registration = MapRecord(record);
                    if (registration is not null)
                        registrations.Add(registration);
                }
            }

            continuationToken = response["continuationToken"]?.Value<string>();
        } while (!string.IsNullOrEmpty(continuationToken));

        return new CrewFetchResult(registrations, nextCursor);
    }

    private static CrewRegistration? MapRecord(JToken record)
    {
        // Tombstone: the registration was deleted or corrected away in Ditio. ChkBox has no delete
        // on registrations, so we cannot retract what we already sent — just don't send more.
        if (record["isDeleted"]?.Value<bool?>() == true)
            return null;

        var id = record["id"]?.Value<string>();
        var projectId = record["projectId"]?.Value<string>();
        var checkIn = record["checkInDateTime"]?.Value<DateTimeOffset?>();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(projectId) || checkIn is null)
            return null;

        return new CrewRegistration
        {
            Id = id,
            DitioProjectId = projectId,
            DitioProjectNumber = record["projectNumber"]?.Value<string>(),
            HseCardId = HseCard.Normalize(record["hseCardId"]?.Value<string>()),
            CheckInDateTime = checkIn.Value,
            CheckOutDateTime = record["checkOutDateTime"]?.Value<DateTimeOffset?>(),
        };
    }
}
