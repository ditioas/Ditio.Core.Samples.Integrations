namespace Ditio.Samples.Examples;

/// <summary>Create → look up → update → disable a user via api/v4/integration/users (the v4 Users API).</summary>
public static class UsersExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // Create a user. employeeNumber is the primary match key and must be unique in the company.
        var created = await api.PostAsync("api/v4/integration/users", new
        {
            companyId = cfg.CompanyId,
            employeeNumber = "SAMPLE-1042",
            firstName = "Ola",
            lastName = "Nordmann",
            mobileWork = "+4798765432",
            birthDate = "1990-05-15",
            employmentStartDate = "2025-03-01",
            workTitle = "Machine Operator",
            department = "Construction",
        });
        string? companyProfileId = created?.companyProfileId;

        // Look up by employee number.
        await api.GetAsync("api/v4/integration/users/by-employee-number/SAMPLE-1042");

        // Partial update uses companyProfileId in the URL.
        if (companyProfileId is not null)
            await api.PatchAsync($"api/v4/integration/users/{companyProfileId}", new { workTitle = "Senior Machine Operator" });

        // Prefer disabling over deleting when someone leaves.
        // if (companyProfileId is not null) await api.PatchAsync($"api/v4/integration/users/disable/{companyProfileId}", new { });

        // New integrations should prefer the v5 Employees API (see 06-employees-v5).
    }
}
