namespace Ditio.Samples.Examples;

/// <summary>
/// Reference / lookup data you read before creating other entities — e.g. machine types (needed to
/// create machines) and alert (notification) types. Core API, scope <c>ditioapiv3</c>.
/// </summary>
public static class ReferenceDataExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // Machine types — each has a typeId used when creating machines (see 07-machines).
        await api.GetAsync("api/MachineType");

        // Alert / notification types — used when working with project alerts.
        await api.GetAsync("api/ProjNotificationTypeSetup");

        // Payroll types and absence types are configured in Ditio and referenced by id in the
        // payroll export filters (payrollTypeIds / absenceTypeIds — see data-extraction (v1/payroll-lines, v1/absence-registrations).

        // Project external references — the lists of values Ditio validates project and work
        // order writes against. Read these BEFORE writing externalProjectNumber on a project:
        // if the company has values configured, anything not in the list is rejected with 400.
        await api.GetAsync("api/project-external-reference/type/ExternalProjectNumber");

        // Sub project numbers are scoped per project, and the path segment is the project's
        // externalProjectNumber -- not its Ditio projectNumber. Those are often the same value,
        // which makes it easy to get wrong when they differ.
        await api.GetAsync("api/project-external-reference/type/ExternalSubProjectNumber/project/40001");

        // Add a value when yours is missing. projNumber is required for ExternalSubProjectNumber
        // and ProcessCode; omit it for the other types. Prefer disable/{id} over delete when a
        // code goes out of use -- disabled values stay readable so historical data still resolves.
        await api.PostAsync("api/project-external-reference", new
        {
            externalRefType = "ExternalProjectNumber",
            externalRefValue = "40001",
            name = "Example project",
        });
    }
}
