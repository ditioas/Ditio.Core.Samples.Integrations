using Newtonsoft.Json;

namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>
/// Everything the bridge must remember between polls, persisted as JSON next to the executable.
///
/// This file is what keeps the integration idempotent. ChkBox's <c>POST /registrations</c> has no
/// idempotency key, so if you lose this file the bridge falls back to reconciling against
/// <c>GET /registrations</c> before posting — slower, but it will not duplicate.
///
/// In production, put this somewhere durable (a mounted volume, not a container's ephemeral disk).
/// </summary>
public sealed class BridgeState
{
    /// <summary>
    /// Delta cursor for the crew source. For the crew-list-registrations source this is the
    /// ModifiedSince timestamp; the online-users source ignores it and re-reads a day window.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Passages already posted to ChkBox, keyed by <see cref="Passage.DedupeKey"/>, with the time
    /// we posted them so old entries can be pruned. This is the primary duplicate guard.
    /// </summary>
    public Dictionary<string, DateTimeOffset> PostedPassages { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Cache of HSE card number -> ChkBox card resource id. Card lookups are the chattiest part of
    /// a poll cycle and the mapping is stable, so caching it keeps the 15-minute cycle cheap.
    /// </summary>
    public Dictionary<string, string> CardResourceIds { get; set; } = new(StringComparer.Ordinal);

    public static BridgeState Load(string path)
    {
        if (!File.Exists(path))
            return new BridgeState();

        try
        {
            return JsonConvert.DeserializeObject<BridgeState>(File.ReadAllText(path)) ?? new BridgeState();
        }
        catch (JsonException ex)
        {
            // Never start from a half-written file with a cursor we cannot trust — that would skip
            // registrations silently. Start clean and let reconciliation prevent duplicates.
            Console.WriteLine($"⚠ State file at {path} is unreadable ({ex.Message}). Starting from a clean state.");
            return new BridgeState();
        }
    }

    /// <summary>Writes via a temp file + move so a crash mid-write cannot corrupt the state.</summary>
    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>
    /// Drops posted-passage entries older than the retention window so the file does not grow
    /// without bound. Keep this comfortably longer than how far back the source can restate a
    /// registration, or a late edit could be posted twice.
    /// </summary>
    public void PrunePostedPassages(TimeSpan retention)
    {
        var cutoff = DateTimeOffset.UtcNow - retention;
        var expired = PostedPassages.Where(entry => entry.Value < cutoff).Select(entry => entry.Key).ToList();
        foreach (var key in expired)
            PostedPassages.Remove(key);
    }
}
