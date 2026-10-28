namespace Ditio.Samples.Examples;

/// <summary>
/// Create-or-update user certificates via api/v4/integration/certificates. Certificates are matched
/// to an existing user by employeeNumber + certificateType. Then attach an image of the card to a certificate
/// via api/v4/integration/certificates/attachments, addressed by the same employeeNumber + certificateType.
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
        // certificateNumber is stored on the certificate; on an update, leaving it out keeps the stored number.

        // Attach an image (or PDF scan) of the card to the certificate. The certificate must exist first (the
        // upsert above), otherwise the upload returns 404. It is addressed by the same employeeNumber +
        // certificateType, so you never need a Ditio id. Send one or more files, each as a 'file' part.
        // Replace this generated 1x1 PNG with the real scan in practice.
        var cardImage = Path.Combine(Path.GetTempPath(), "byggekort-front.png");
        await File.WriteAllBytesAsync(cardImage, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg=="));

        // replaceExistingFilesWithSameName=true replaces a file this API client uploaded earlier to this certificate
        // with the same file name (use it on re-sync to avoid duplicates). Attachments added by others, for
        // example the employee in the app, are never replaced. certificateNumber picks the card when the employee
        // holds more than one current builder card; without it, the one valid longest is used.
        var upload = await api.UploadFilesAsync(
            "api/v4/integration/certificates/attachments?employeeNumber=SAMPLE-1042&certificateType=Byggekort" +
            "&certificateNumber=BK-2025-12345&replaceExistingFilesWithSameName=true",
            cardImage);

        string? certificateId = upload?.certificateId;
        string? fileReferenceId = upload?.files?[0]?.fileReference?.id;
        Console.WriteLine($"Attached to certificate {certificateId}; file reference id: {fileReferenceId}");
    }
}
