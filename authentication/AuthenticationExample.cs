namespace Ditio.Samples.Examples;

/// <summary>Fetches an OAuth2 client-credentials token and shows that it is cached/reused.</summary>
public static class AuthenticationExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var tokens = new DitioTokenProvider(cfg);

        var token = await tokens.GetTokenAsync(cfg.Scope);
        Console.WriteLine($"Got a '{cfg.Scope}' token ({token.Length} chars) from {cfg.IdentityUrl}.");

        // Cached — the second call does not hit the token endpoint again.
        _ = await tokens.GetTokenAsync(cfg.Scope);
        Console.WriteLine("Second call reused the cached token (tokens are short-lived; refresh near expiry).");

        // The reporting / data-extraction API uses a different scope:
        // var reportingToken = await tokens.GetTokenAsync(cfg.ReportingScope);
    }
}
