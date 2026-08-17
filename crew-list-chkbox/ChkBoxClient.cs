using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>A registration that already exists in ChkBox, used to avoid posting it twice.</summary>
public sealed record ExistingRegistration(string CardId, string Action, DateTimeOffset Time);

/// <summary>
/// Minimal client for Infotech's ChkBox API (byggekortleser). The API is
/// <see href="https://jsonapi.org/format/1.0/">JSON:API v1.0</see>, not plain REST — resources are
/// wrapped in <c>data</c>, relationships are resource-linkage objects, and related resources come
/// back in a top-level <c>included</c> array when you ask for them with <c>include</c>.
///
/// Auth is an API key in the Authorization header: <c>Authorization: ApiKey &lt;key&gt;</c>.
/// Base URLs: <c>https://api.byggekortleser.no</c> (production),
/// <c>https://devapi.byggekortleser.no</c> (development/testing, separate keys).
/// </summary>
public sealed class ChkBoxClient
{
    private const string JsonApiMediaType = "application/vnd.api+json";

    private static readonly HttpStatusCode[] RetryableStatuses =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
    ];

    private readonly HttpClient _http;
    private readonly int _maxAttempts;

    public ChkBoxClient(string baseUrl, string apiKey, int maxAttempts = 4)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        // The scheme really is the literal word "ApiKey", not Bearer/Basic.
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonApiMediaType));
        _maxAttempts = maxAttempts;
    }

    /// <summary>Verifies a ChkBox project id exists and returns its description, or null if not found.</summary>
    public async Task<string?> GetProjectDescriptionAsync(string projectId, CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Get, $"projects/{Uri.EscapeDataString(projectId)}", null, cancellationToken);
        if (response is null)
            return null;

        return response["data"]?["attributes"]?["description"]?.Value<string>() ?? "(no description)";
    }

    /// <summary>
    /// Resolves an HSE card number (the number printed on the byggekort, e.g. <c>4388261</c>) to the
    /// opaque ChkBox card resource id that <c>POST /registrations</c> requires.
    ///
    /// Returns null when the card is unknown to ChkBox. <c>GET /cards</c> only returns cards that
    /// have already been registered on a project, or pre-approved for one — anywhere in the tenant,
    /// not necessarily on the project you are posting to. A brand new worker who has never swiped
    /// and has never been added to an access group therefore cannot be resolved, and the project
    /// owner has to add them in ChkBox before their passages can be bridged.
    /// </summary>
    public async Task<string?> ResolveCardResourceIdAsync(string hseCardId, CancellationToken cancellationToken)
    {
        var path = $"cards?filter%5BcardId%5D={Uri.EscapeDataString(hseCardId)}";
        var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);

        // Resource ids are opaque strings — never parse or construct them, always resolve.
        return (response?["data"] as JArray)?.FirstOrDefault()?["id"]?.Value<string>();
    }

    /// <summary>
    /// Reads registrations already on a project since a given time, so the bridge can skip
    /// passages that are already there. This is the safety net behind the local dedupe state:
    /// ChkBox has no idempotency key on POST, so a lost state file would otherwise mean duplicates.
    /// </summary>
    public async Task<IReadOnlyList<ExistingRegistration>> GetRegistrationsAsync(
        string projectId,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        var results = new List<ExistingRegistration>();
        const int pageSize = 500;
        var offset = 0;

        while (true)
        {
            var path = $"registrations?filter%5Bproject%5D={Uri.EscapeDataString(projectId)}"
                       + $"&filter%5Btime%5D%5Bge%5D={Uri.EscapeDataString(FormatTime(since))}"
                       + $"&include=card&page%5Blimit%5D={pageSize}&page%5Boffset%5D={offset}";

            var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
            if (response?["data"] is not JArray data || data.Count == 0)
                break;

            // Related cards arrive in the top-level `included` array — map id -> printed cardId.
            var cardsById = new Dictionary<string, string>(StringComparer.Ordinal);
            if (response["included"] is JArray included)
            {
                foreach (var item in included.Where(i => i["type"]?.Value<string>() == "card"))
                {
                    var id = item["id"]?.Value<string>();
                    var cardId = item["attributes"]?["cardId"]?.Value<string>();
                    if (id is not null && cardId is not null)
                        cardsById[id] = cardId;
                }
            }

            foreach (var registration in data)
            {
                var cardResourceId = registration["relationships"]?["card"]?["data"]?["id"]?.Value<string>();
                var action = registration["attributes"]?["action"]?.Value<string>();
                var time = registration["attributes"]?["time"]?.Value<DateTimeOffset?>();

                if (cardResourceId is null || action is null || time is null)
                    continue;

                if (cardsById.TryGetValue(cardResourceId, out var printedCardId))
                    results.Add(new ExistingRegistration(printedCardId, action, time.Value));
            }

            if (data.Count < pageSize)
                break;

            offset += pageSize;
        }

        return results;
    }

    /// <summary>
    /// Creates a registration (a passage). Required fields are <c>action</c>, <c>project</c> and
    /// <c>card</c>; <c>time</c> is optional and defaults to now, but we always send it explicitly
    /// because we are replaying passages that happened earlier.
    /// </summary>
    public async Task PostRegistrationAsync(
        string chkBoxProjectId,
        string cardResourceId,
        string action,
        DateTimeOffset time,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            data = new
            {
                type = "registration",
                attributes = new
                {
                    action,
                    time = FormatTime(time),
                },
                relationships = new
                {
                    project = new { data = new { type = "project", id = chkBoxProjectId } },
                    card = new { data = new { type = "card", id = cardResourceId } },
                },
            },
        };

        // Observed behaviour: this returns 202 Accepted with an EMPTY body — the registration is
        // processed asynchronously and is not echoed back. So "no exception" is the success signal;
        // do not expect a resource id here. SendAsync throws on any non-2xx.
        //
        // Because acceptance is asynchronous, a freshly posted passage may not be visible to
        // GET /registrations for a moment. That is why the local posted-passage state is the
        // primary duplicate guard and the read-back is only a safety net.
        await SendAsync(HttpMethod.Post, "registrations", body, cancellationToken);
    }

    /// <summary>
    /// ChkBox assumes Central European time when an offset is missing, which is a trap for anyone
    /// running outside Norway or across the DST boundary. Always send an explicit offset.
    /// </summary>
    public static string FormatTime(DateTimeOffset time) => time.ToString("yyyy-MM-ddTHH:mm:sszzz");

    private async Task<JObject?> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(JsonApiMediaType);
            }

            using var response = await _http.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return string.IsNullOrWhiteSpace(raw) ? null : ParseJson(raw);

            // A 404 on a lookup is a legitimate "not found", not a failure worth throwing over.
            if (response.StatusCode == HttpStatusCode.NotFound && method == HttpMethod.Get)
                return null;

            var retryable = RetryableStatuses.Contains(response.StatusCode);
            if (!retryable || attempt >= _maxAttempts)
            {
                throw new ChkBoxException(
                    $"{method} {path} failed: {(int)response.StatusCode} {response.StatusCode}. Body: {Truncate(raw)}");
            }

            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            Console.WriteLine($"  ChkBox {(int)response.StatusCode} on {method} {PathOnly(path)} — retrying in {delay.TotalSeconds:0}s (attempt {attempt}/{_maxAttempts - 1})");
            await Task.Delay(delay, cancellationToken);
        }
    }

    /// <summary>
    /// Parses with <see cref="DateParseHandling.DateTimeOffset"/>. Newtonsoft otherwise materialises
    /// timestamps as <see cref="DateTime"/> and drops the offset — which both throws on a
    /// <c>Value&lt;DateTimeOffset&gt;()</c> read and would silently shift passage times if it didn't.
    /// </summary>
    private static JObject ParseJson(string raw)
    {
        using var reader = new JsonTextReader(new StringReader(raw))
        {
            DateParseHandling = DateParseHandling.DateTimeOffset,
        };
        return JObject.Load(reader);
    }

    /// <summary>Strips the query string so we never log a card number that was used as a filter.</summary>
    private static string PathOnly(string path)
    {
        var queryStart = path.IndexOf('?');
        return queryStart < 0 ? path : path[..queryStart];
    }

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500] + "…";
}

public sealed class ChkBoxException(string message) : Exception(message);
