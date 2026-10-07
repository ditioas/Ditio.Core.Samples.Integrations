namespace Ditio.Samples.Examples;

/// <summary>
/// Pull approved payroll data out of Ditio via the payroll export API — the recommended way to move
/// payroll data into an accounting/payroll system (the alternative is a flat-file export configured in
/// Ditio Web). Integration API host, <c>ditioapiv3</c> scope, same as the write endpoints.
/// <para>
/// Each summary carries <c>overtimeLines</c>: one entry per overtime payroll type booked on the day, so
/// an integration does not have to assume the three fixed overtime fields (<c>overtime50Qty</c>,
/// <c>overtime100Qty</c>). A company can have any number of overtime types — see the loop below.
/// </para>
/// </summary>
public static class PayrollExportExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // fromWorkDate/toWorkDate default to the last 14 days when omitted; the window is capped at 45
        // days. dataFilter=0 (default) returns only approved data.
        var summaries = await api.GetAsync(
            "api/payroll-export/?fromWorkDate=2026-09-01&toWorkDate=2026-09-28&dataFilter=0");

        // `/readonly` is an alias for the same JSON export — some integrations prefer it because it
        // documents intent (it never locks data, same as the default dataFilter=0 above).
        // await api.GetAsync("api/payroll-export/readonly?fromWorkDate=2026-09-01&toWorkDate=2026-09-28");

        if (summaries is null)
            return;

        foreach (var summary in summaries)
        {
            Console.WriteLine($"{summary.userName}  {summary.transDateTime}  qty={summary.qty}");

            // Existing fixed fields are unchanged — still present alongside overtimeLines.
            Console.WriteLine($"  overtime50Qty={summary.overtime50Qty}  overtime100Qty={summary.overtime100Qty}");

            // overtimeLines: ordered by lineType, then sorting, then name. Iterate it instead of reading
            // overtime50Qty/overtime100Qty directly so a manual overtime type (lineType 41) — or a fourth
            // level, should the company ever add one — is not silently dropped.
            foreach (var line in summary.overtimeLines ?? Enumerable.Empty<dynamic>())
            {
                // lineType: 20/30/40 = overtime levels 1/2/3 (the automatic calculation), 41 = a manually
                // registered overtime type (a company can have any number of these, freely named).
                //
                // overtimeType: 0 = Overtime — deducted from ordinary hours (summary.qty already nets
                //                    this out).
                //               1 = OvertimeAddition — paid ON TOP of ordinary hours, not deducted.
                // Two companies can configure the same lineType with different overtimeType values, so
                // always read overtimeType per line rather than assuming it from lineType.
                string kind = line.overtimeType == 1 ? "addition (paid on top)" : "overtime (deducted)";
                Console.WriteLine($"    [{line.lineType}] {line.name} ({kind}): {line.qty}");
            }
        }
    }
}
