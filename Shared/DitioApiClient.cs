using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Ditio.Samples;

/// <summary>
/// Minimal HTTP helper for the Ditio APIs: attaches a Bearer token, sends JSON, and returns the
/// parsed response as <c>dynamic</c>. Create one per API surface — the integration API and the
/// reporting API use different base URLs and scopes.
/// </summary>
public sealed class DitioApiClient
{
    private readonly HttpClient _http;
    private readonly DitioTokenProvider _tokens;
    private readonly string _scope;

    public DitioApiClient(string baseUrl, string scope, DitioTokenProvider tokens)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        _scope = scope;
        _tokens = tokens;
    }

    public Task<dynamic?> GetAsync(string path) => SendAsync(HttpMethod.Get, path);
    public Task<dynamic?> PostAsync(string path, object body) => SendAsync(HttpMethod.Post, path, body);
    public Task<dynamic?> PutAsync(string path, object body) => SendAsync(HttpMethod.Put, path, body);
    public Task<dynamic?> PatchAsync(string path, object body) => SendAsync(HttpMethod.Patch, path, body);
    public Task<dynamic?> DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path);

    private async Task<dynamic?> SendAsync(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetTokenAsync(_scope));
        if (body is not null)
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request);
        return await ReadResponse(method, path, response);
    }

    /// <summary>
    /// Downloads a binary response body to a local file (used by the Documents API's
    /// GET .../documents/{fileReferenceId}, which streams the document bytes as
    /// application/octet-stream). Returns the path written, or null on a non-success status.
    /// </summary>
    public async Task<string?> DownloadFileAsync(string path, string destinationPath)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetTokenAsync(_scope));

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        Console.WriteLine($"GET {path} -> {(int)response.StatusCode} {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"  Error: {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);
        Console.WriteLine($"  Saved {response.Content.Headers.ContentLength ?? 0} bytes to {destinationPath}");
        return destinationPath;
    }

    /// <summary>Uploads one or more files as multipart/form-data (used by the Documents API).</summary>
    public async Task<dynamic?> UploadFilesAsync(string path, params string[] filePaths)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in filePaths)
        {
            var content = new StreamContent(File.OpenRead(file));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(content, "file", Path.GetFileName(file));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, path.TrimStart('/')) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetTokenAsync(_scope));

        using var response = await _http.SendAsync(request);
        return await ReadResponse(HttpMethod.Post, path, response);
    }

    private static async Task<dynamic?> ReadResponse(HttpMethod method, string path, HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"{method} {path} -> {(int)response.StatusCode} {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"  Error: {raw}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        Console.WriteLine($"  Response: {raw}");
        return JsonConvert.DeserializeObject<dynamic>(raw);
    }
}
