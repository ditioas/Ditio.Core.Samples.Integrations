using Newtonsoft.Json;

namespace Ditio.Samples.Examples;

/// <summary>
/// Pull payroll data out of Ditio into an accounting/payroll system.
/// <para>
/// <b>Recommended:</b> <c>v1/payroll-lines</c> on the Reporting host (Data Extraction API,
/// <c>reportingapiv1</c> scope). It is paginated, supports full and incremental sync, and returns
/// each payroll summary with <c>payrollValuesAsExportLines</c> — the lines (type, quantity, rate,
/// project, work order) ready to map to your payroll codes. <c>v1/payroll-lines-extended</c> takes the
/// same parameters and adds approval, verification and lock status plus the full breakdown.
/// </para>
/// <para>
/// <b>Alternative:</b> <c>api/payroll-export</c> on the Integration host (<c>ditioapiv3</c> scope) — the
/// older, non-paginated JSON export (date range capped at 45 days), and <c>api/payroll-export/file</c>,
/// which returns the file in the format configured in Ditio's payroll setup.
/// </para>
/// <para>
/// <b>Locking:</b> none of these calls lock data. A period is locked by a payroll administrator in
/// Ditio Web, not through the API. Until then <c>ExportFilter=Approved</c> returns the same lines on
/// every call, so make your import idempotent (upsert on the line <c>id</c>). Once a period is locked its
/// lines leave <c>Approved</c>; read them with <c>ExportFilter=Locked</c> or <c>All</c>.
/// </para>
/// Both APIs need an API client whose user is an administrator (otherwise 403).
/// </summary>
public static class PayrollExportExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var tokens = new DitioTokenProvider(cfg);

        // Reporting API: different base URL AND scope than the Integration API.
        var reporting = new DitioApiClient(cfg.ReportingBaseUrl, cfg.ReportingScope, tokens);
        await PullApprovedPayrollLinesAsync(reporting);
        await PullPayrollChangesAsync(reporting);

        // Alternative: the Integration API's payroll export.
        var integration = new DitioApiClient(cfg.BaseUrl, cfg.Scope, tokens);
        await PrintPayrollExportOvertimeAsync(integration);
    }

    /// <summary>
    /// Full sync of one period: approved, not yet locked payroll data, page by page.
    /// </summary>
    private static async Task PullApprovedPayrollLinesAsync(DitioApiClient api)
    {
        Console.WriteLine("\n--- v1/payroll-lines, approved data for a period (full sync) ---");

        // ExportFilter defaults to All, which includes data that is not yet approved — always send
        // Approved for a payroll run (Approved = approved and not yet locked).
        // UserPayrollTypeFilter defaults to PaidByHour; use AllUsers to include fixed-pay employees.
        // FromDateTime/ToDateTime bound the work dates. There is no 45-day limit here: follow the
        // continuation token until it is empty and you have the whole range. Chunk a large backfill
        // month by month.
        const string path = "v1/payroll-lines?ExportFilter=Approved&FromDateTime=2026-09-01&ToDateTime=2026-09-30";

        var summaries = 0;
        var exportLines = 0;
        await foreach (var summary in FetchAllAsync(api, path))
        {
            summaries++;

            // One payroll summary per employee per day; each carries the lines to export.
            foreach (var line in summary.payrollValuesAsExportLines ?? Enumerable.Empty<dynamic>())
            {
                exportLines++;
                Console.WriteLine($"  employee {summary.employeeNumber}  {summary.transDateTime}  " +
                                  $"{line.typeName}: qty={line.qty} rate={line.rate} " +
                                  $"project={line.projectNumber} workOrder={line.taskNumber}");
            }
        }

        Console.WriteLine($"Pulled {summaries} payroll summaries, {exportLines} export lines.");
    }

    /// <summary>
    /// Incremental sync: only what changed since your last successful sync. Deleted lines come back as
    /// tombstones (<c>isDeleted: true</c>) so you can remove them on your side.
    /// </summary>
    private static async Task PullPayrollChangesAsync(DitioApiClient api)
    {
        Console.WriteLine("\n--- v1/payroll-lines, changes since your last sync (incremental) ---");

        // ModifiedSince is the instant of your last successful sync (ISO-8601, UTC) and must be within
        // the last 45 days; for older data run a full sync. Store the new watermark only after every
        // page has been processed.
        var modifiedSince = DateTime.UtcNow.AddDays(-1).ToString("o");
        var path = $"v1/payroll-lines?ExportFilter=Approved&ModifiedSince={Uri.EscapeDataString(modifiedSince)}";

        var changed = 0;
        var deleted = 0;
        await foreach (var summary in FetchAllAsync(api, path))
        {
            if ((bool?)summary.isDeleted == true)
            {
                deleted++;
                Console.WriteLine($"  remove {summary.id}");
                continue;
            }

            changed++;
            Console.WriteLine($"  upsert {summary.id}");
        }

        Console.WriteLine($"{changed} changed, {deleted} deleted since {modifiedSince}.");
    }

    /// <summary>
    /// The Integration API's payroll export. Each summary carries <c>overtimeLines</c>: one entry per
    /// overtime payroll type booked on the day, so an integration does not have to assume the fixed
    /// <c>overtime50Qty</c>/<c>overtime100Qty</c> fields — a company can have any number of overtime types.
    /// (<c>v1/payroll-lines-extended</c> carries the same field; see ../data-extraction.)
    /// </summary>
    private static async Task PrintPayrollExportOvertimeAsync(DitioApiClient api)
    {
        Console.WriteLine("\n--- api/payroll-export (Integration API alternative) ---");

        // fromWorkDate/toWorkDate default to the last 14 days when omitted; the window is capped at 45
        // days (a longer range is silently cut to 45 days — chunk it). dataFilter=0 (default) returns
        // only approved, not yet locked data.
        var summaries = await api.GetAsync(
            "api/payroll-export/?fromWorkDate=2026-09-01&toWorkDate=2026-09-28&dataFilter=0");

        // `/readonly` is an alias for the same JSON export.
        // await api.GetAsync("api/payroll-export/readonly?fromWorkDate=2026-09-01&toWorkDate=2026-09-28");

        // The payroll FILE in the format configured in Ditio's payroll setup (dataExportType=10 =
        // payroll file) is a download, not JSON — see README.md:
        // GET api/payroll-export/file?fromWorkDate=2026-09-01&toWorkDate=2026-09-28&dataExportType=10

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

    /// <summary>
    /// Follows <c>continuationToken</c> until it is empty and yields every record. Uses
    /// <see cref="DitioApiClient.GetRawAsync"/>, which throws on a non-success status — a failed page can
    /// never be mistaken for "no more data" and push your sync watermark past it.
    /// </summary>
    private static async IAsyncEnumerable<dynamic> FetchAllAsync(DitioApiClient api, string path)
    {
        string? token = null;
        do
        {
            var url = token is null ? path : $"{path}&ContinuationToken={Uri.EscapeDataString(token)}";
            dynamic? page = JsonConvert.DeserializeObject<dynamic>(await api.GetRawAsync(url));
            if (page is null)
                yield break;

            foreach (var record in page.data)
                yield return record;

            token = page.continuationToken;
        } while (!string.IsNullOrEmpty(token));
    }
}
