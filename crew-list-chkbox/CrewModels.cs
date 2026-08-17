namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>
/// One person's stay on one project — a check-in, plus a check-out once they leave.
/// Both crew sources normalise to this shape, and the bridge turns it into ChkBox passages.
/// </summary>
public sealed record CrewRegistration
{
    /// <summary>
    /// Stable id for this stay. Used as the dedupe key, so it MUST NOT change between polls —
    /// if it does, the bridge will post duplicate registrations to ChkBox.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Ditio project the person checked in on.</summary>
    public required string DitioProjectId { get; init; }

    public string? DitioProjectNumber { get; init; }

    /// <summary>
    /// HSE card (byggekort) number, normalised the same way Ditio normalises it before pushing to
    /// HMSReg. Without one we cannot resolve a ChkBox card, so the stay is skipped.
    /// </summary>
    public string? HseCardId { get; init; }

    public required DateTimeOffset CheckInDateTime { get; init; }

    /// <summary>Null while the person is still on site.</summary>
    public DateTimeOffset? CheckOutDateTime { get; init; }

    public bool IsOnSite => CheckOutDateTime is null;
}

public enum PassageAction
{
    In,
    Out,
}

/// <summary>A single ChkBox registration to post: one person, one project, in or out, at a time.</summary>
public sealed record Passage
{
    public required string RegistrationId { get; init; }
    public required PassageAction Action { get; init; }
    public required DateTimeOffset Time { get; init; }
    public required string HseCardId { get; init; }
    public required string DitioProjectId { get; init; }

    /// <summary>What the bridge remembers so it never posts the same passage twice.</summary>
    public string DedupeKey => $"{RegistrationId}:{ActionValue}";

    /// <summary>The literal value ChkBox expects for <c>registration.attributes.action</c>.</summary>
    public string ActionValue => Action == PassageAction.In ? "in" : "out";
}

public sealed record CrewFetchResult(IReadOnlyList<CrewRegistration> Registrations, string? NextCursor);

/// <summary>
/// Where crew data comes from. Two implementations exist so this sample keeps working as Ditio's
/// public API surface changes — see the README for which to use when.
/// </summary>
public interface ICrewSource
{
    /// <summary>Name shown in log output.</summary>
    string Name { get; }

    /// <summary>
    /// Fetches everything that changed since <paramref name="cursor"/>. Sources that have no delta
    /// support ignore the cursor and return a full window; the bridge dedupes either way.
    /// </summary>
    Task<CrewFetchResult> FetchAsync(string? cursor, CancellationToken cancellationToken);
}

internal static class HseCard
{
    /// <summary>
    /// Normalises an HSE card (byggekort) number the same way Ditio does before pushing to HMSReg
    /// (see <c>WorkCardStatus.NormalizeCardId</c> in the Ditio core): strip spaces, and drop a
    /// single leading zero on numbers longer than 7 characters.
    ///
    /// Both sides must agree on this, otherwise <c>GET /cards?filter[cardId]=</c> silently finds
    /// nothing and every passage for that worker is skipped.
    /// </summary>
    public static string? Normalize(string? cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return null;

        cardId = cardId.Replace(" ", "");

        if (cardId.Length > 7 && cardId.StartsWith('0'))
            cardId = cardId[1..];

        return cardId.Length == 0 ? null : cardId;
    }
}

internal static class Redact
{
    /// <summary>
    /// The crew list is personal data. Never log a full card number, name, phone or birth date —
    /// log this instead, which is enough to correlate against Ditio without spilling a roster.
    /// </summary>
    public static string Card(string? cardId) => cardId switch
    {
        null or "" => "(none)",
        { Length: <= 4 } => "****",
        _ => $"{cardId[..2]}****{cardId[^2..]}",
    };
}
