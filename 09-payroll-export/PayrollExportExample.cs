namespace Ditio.Samples.Examples;

/// <summary>
/// Read payroll data out of Ditio via GET /api/payroll-export/ (JSON). Convert the response into
/// whatever format your accounting system needs. Date ranges are limited to 45 days.
/// </summary>
public static class PayrollExportExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // dataFilter: 0 = only approved (default), 5 = only locked, 10 = all data.
        await api.GetAsync("api/payroll-export/?fromWorkDate=2025-01-01&toWorkDate=2025-01-31&dataFilter=0");

        // Summary lines:
        // await api.GetAsync("api/payroll-export/summary-as-lines?fromWorkDate=2025-01-01&toWorkDate=2025-01-31");

        // A formatted export file (format from the company payroll setup) is at /api/payroll-export/file.
    }
}
