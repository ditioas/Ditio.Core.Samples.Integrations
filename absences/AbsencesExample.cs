namespace Ditio.Samples.Examples;

/// <summary>
/// Push an absence that has already been granted in your payroll or HR system into Ditio, in one call,
/// via api/v5/integration/absences. Then read it back and cancel it.
/// <para>
/// The point of this endpoint is that you send the identifiers you already hold — an employee number, a
/// payroll code, a project number — and Ditio resolves its own ids, expands the period into days, and
/// runs the same validation an absence created in the app goes through. You never look up a Ditio id.
/// </para>
/// </summary>
public static class AbsencesExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // Your own id for this absence. It is the whole dedup contract: pushing the same one again
        // UPDATES the absence rather than creating a second one, so replaying a feed is harmless.
        //
        // Use whatever stable id your system already assigns the absence itself — a record id, a case id,
        // the id of the leave application. Do NOT build it out of the dates: that dedupes a replay, but
        // the moment someone edits the period in your system the key changes too, the update arrives
        // looking like a brand new absence, and you end up with two overlapping absences.
        const string externalId = "SAMPLE-ABS-001";

        // 1. Create. Everything here is a business identifier you already have.
        var created = await api.PostAsync("api/v5/integration/absences", new
        {
            externalId,
            employeeNumber = "1042",

            // The payroll code the absence type already carries in Ditio — the same wage type
            // (lønnsart) your payroll system uses. Nothing to configure on either side.
            absenceTypeCode = "505",

            // Plain dates. A value carrying a time or an offset is rejected on purpose:
            // "2026-09-14T00:00:00+02:00" arrives as the 13th in UTC and would book the wrong day.
            startDate = "2026-09-14",
            endDate = "2026-09-18",

            // Your project number, whatever form it takes. Ditio matches it against every project key it
            // holds. If it identifies more than one project the request is refused rather than resolved,
            // and the error names the projects and which of those numbers were involved.
            projectNumber = "SAMPLE-P-001",

            hoursPerDay = 7.5,
            comment = "Innvilget i lønnssystemet",
        });

        Console.WriteLine($"Absence id: {created?.absenceRegistrationId}");
        Console.WriteLine($"Employee:   {created?.userName} ({created?.employeeNumber})");
        Console.WriteLine($"Type:       {created?.absenceTypeName}");
        Console.WriteLine($"Project:    {created?.projectName} ({created?.projectNumber})");
        Console.WriteLine($"Days:       {created?.daysQty}");

        // ALWAYS check daysQty against what you expected. The period is expanded to WORK days, so a
        // Monday-to-Sunday absence is five days, not seven.

        // 2. Push the same externalId again — this UPDATES, it does not duplicate. Here the period is
        //    corrected to end a day earlier, and one day is adjusted rather than re-listed in full.
        await api.PostAsync("api/v5/integration/absences", new
        {
            externalId,
            employeeNumber = "1042",
            absenceTypeCode = "505",
            startDate = "2026-09-14",
            endDate = "2026-09-17",
            projectNumber = "SAMPLE-P-001",
            hoursPerDay = 7.5,

            // days ADJUSTS the period; it does not replace it. Only list what differs:
            //   qty > 0   sets that day's hours (and adds a weekend day inside the period)
            //   qty = 0   removes that day — the person worked
            //   a date outside startDate..endDate is ignored
            // So this books the 14th, 15th and 17th at 7.5, and the 16th at 4.
            //
            // A day is { date, qty } in both directions — the same shape comes back in the response's
            // days array, so there is nothing to translate.
            days = new[]
            {
                new { date = "2026-09-16", qty = 4.0 },
            },

            // Note what is NOT here: approved. On a later push the flag is ignored in both directions —
            // it cannot approve an absence, and omitting it cannot revoke an approval granted in Ditio.
            // Approval belongs to Ditio once the absence exists.
        });

        // 3. Read it back by your own id. No Ditio id needed, ever.
        await api.GetAsync($"api/v5/integration/absences/{externalId}");

        // 4. Cancel it. Only absences created through this endpoint are reachable — an absence a person
        //    registered in the app carries no externalId, so it cannot be found or deleted here. That is
        //    a property of the key rather than a permission check.
        await api.DeleteAsync($"api/v5/integration/absences/{externalId}");

        // A payroll code is a ROLLUP: Ditio models finer absence types than payroll pays on, so one code
        // can cover several types ("blood donation", "dentist" and "funeral" may all export as one
        // welfare-leave code). When a code does not identify exactly one active type the request is
        // refused and the error lists the candidates with their ids — send absenceTypeId for those:
        //
        //   { "externalId": "...", "employeeNumber": "1042",
        //     "absenceTypeId": "65f1a2b3c4d5e6f7a8b9c0d1",
        //     "startDate": "2026-09-14", "endDate": "2026-09-18" }
        //
        // Sending both absenceTypeCode and absenceTypeId is fine; the id wins.
    }
}
