namespace Ditio.Samples.Examples;

/// <summary>
/// Create-or-update user certificates via api/v4/integration/certificates. Certificates are matched
/// to an existing user by employeeNumber + certificateType.
/// </summary>
public static class CertificatesExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // employeeNumber must already exist in Ditio (see the users / employees examples).
        await api.PostAsync("api/v4/integration/certificates", new object[]
        {
            new
            {
                employeeNumber = "SAMPLE-1042",
                certificateType = "Byggekort",
                certificateNumber = "BK-2025-12345",
                issuedDateTime = "2025-01-15T00:00:00Z",
                validUntilDateTime = "2027-01-15T00:00:00Z",
                notes = "Issued by Byggenæringens Landsforening",
            },
            new
            {
                employeeNumber = "SAMPLE-1042",
                certificateType = "Kranførerbevis",
                certificateNumber = "KF-2025-67890",
                issuedDateTime = "2024-06-01T00:00:00Z",
                validUntilDateTime = "2026-06-01T00:00:00Z",
            },
        });

        // Re-posting the same employeeNumber + certificateType updates the existing certificate.
    }
}
