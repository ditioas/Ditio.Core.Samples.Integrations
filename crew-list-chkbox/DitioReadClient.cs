using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ditio.Samples.Examples.CrewListChkBox;

/// <summary>
/// A read-only Ditio client for this sample.
///
/// It deliberately does NOT reuse <see cref="DitioApiClient"/>, for two reasons that matter here:
/// that client prints every full response body to the console — which for the crew list means
/// dumping names, phone numbers and birth dates into the terminal — and it swallows non-2xx
/// responses by returning null, which in a poll loop would look like "no crew today" and let the
/// bridge advance its cursor past registrations it never read.
///
/// This one is quiet and fails loudly instead.
/// </summary>
internal sealed class DitioReadClient(string baseUrl, string scope, DitioTokenProvider tokens)
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };

    public async Task<JToken> GetAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path.TrimStart('/'));
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(scope));

        using var response = await _http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new DitioReadException(
                $"GET {PathOnly(path)} failed: {(int)response.StatusCode} {response.StatusCode}. Body: {Truncate(raw)}",
                response.StatusCode);
        }

        return string.IsNullOrWhiteSpace(raw) ? new JArray() : ParseJson(raw);
    }

    /// <summary>
    /// Parses with <see cref="DateParseHandling.DateTimeOffset"/> so UTC timestamps from the
    /// data-extraction API keep their offset. Newtonsoft's default turns them into
    /// <see cref="DateTime"/>, which throws on a <c>Value&lt;DateTimeOffset&gt;()</c> read.
    /// </summary>
    private static JToken ParseJson(string raw)
    {
        using var reader = new JsonTextReader(new StringReader(raw))
        {
            DateParseHandling = DateParseHandling.DateTimeOffset,
        };
        return JToken.Load(reader);
    }

    /// <summary>Query strings can carry project ids; keep them out of exception messages and logs.</summary>
    private static string PathOnly(string path)
    {
        var queryStart = path.IndexOf('?');
        return queryStart < 0 ? path : path[..queryStart];
    }

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500] + "…";
}

internal sealed class DitioReadException(string message, System.Net.HttpStatusCode statusCode) : Exception(message)
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
}
