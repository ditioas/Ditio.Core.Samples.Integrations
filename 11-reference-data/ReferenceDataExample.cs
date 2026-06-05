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
        // payroll export filters (payrollTypeIds / absenceTypeIds — see 09-payroll-export).
    }
}
