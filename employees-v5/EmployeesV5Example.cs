namespace Ditio.Samples.Examples;

/// <summary>
/// The modern v5 Employees API (api/v5/integration/employees): create with an initial employment,
/// look up by employee number, partial-update, and run an employment operation.
/// </summary>
public static class EmployeesV5Example
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // Create an employee (profile + first employment are created together).
        await api.PostAsync("api/v5/integration/employees", new
        {
            employeeNumber = "SAMPLE-2042",
            firstName = "Kari",
            lastName = "Nordmann",
            phone = "+4791234567",
            birthDate = "1992-08-20",
            email = "kari.nordmann@example.com",
            workTitle = "Surveyor",
            employment = new
            {
                startDate = "2025-03-01",
                department = "Survey",
                payroll = 1, // 0 = Disabled, 1 = Enabled, 2 = Variable
            },
        });

        // Look up, optionally including related data.
        await api.GetAsync("api/v5/integration/employees/SAMPLE-2042?include=tags,employment-history");

        // True PATCH semantics: only the fields you send change; explicit null clears a value.
        await api.PatchAsync("api/v5/integration/employees/SAMPLE-2042", new { workTitle = "Lead Surveyor" });

        // Update the current employment (dedicated employment endpoint).
        await api.PatchAsync("api/v5/integration/employees/SAMPLE-2042/update-employment", new { department = "Geomatics" });

        // Offboarding: end the current employment.
        // await api.PatchAsync("api/v5/integration/employees/SAMPLE-2042/end-employment", new { endDate = "2025-06-30" });
    }
}
