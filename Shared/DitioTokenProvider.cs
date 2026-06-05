using IdentityModel.Client; // https://github.com/IdentityModel

namespace Ditio.Samples;

/// <summary>
/// Fetches and caches OAuth2 client-credentials tokens per scope. Ditio tokens a client fetches
/// itself are short-lived (~30 min by default), so cache them and refresh ~1 minute before expiry
/// rather than requesting a new token per call.
/// </summary>
public sealed class DitioTokenProvider(DitioConfig config)
{
    private readonly Dictionary<string, (string Token, DateTime ExpiresUtc)> _cache = new();

    public async Task<string> GetTokenAsync(string scope)
    {
        if (_cache.TryGetValue(scope, out var cached) && DateTime.UtcNow < cached.ExpiresUtc.AddMinutes(-1))
            return cached.Token;

        using var client = new HttpClient();
        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = $"{config.IdentityUrl.TrimEnd('/')}/connect/token",
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret,
            Scope = scope,
        });

        if (response.IsError || string.IsNullOrEmpty(response.AccessToken))
            throw new Exception($"Authentication failed for scope '{scope}': {response.Error} {response.ErrorDescription} {response.Raw}");

        _cache[scope] = (response.AccessToken, DateTime.UtcNow.AddSeconds(response.ExpiresIn));
        return response.AccessToken;
    }
}
