using System.Net;

namespace Ditio.Samples.Examples;

/// <summary>Create → look up → update machines and equipment via api/v4/integration/machines.</summary>
public static class MachinesExample
{
    public static async Task RunAsync(DitioConfig cfg)
    {
        var api = new DitioApiClient(cfg.BaseUrl, cfg.Scope, new DitioTokenProvider(cfg));

        // typeId must match an existing machine type in your company.
        // Look up valid types via: GET {BaseUrl}/api/MachineType
        var created = await api.PostAsync("api/v4/integration/machines", new
        {
            companyId = cfg.CompanyId,
            machineNumber = "SAMPLE-M-001",
            name = "Volvo EC220E",
            typeId = "beltemaskin", // tracked excavator (example)
            active = true,
            buildYear = 2022,
            weight = 22000,
            registrationNumber = "AB 12345",
        });
        string? machineId = created?.id;

        // Look up by machine number.
        await api.GetAsync("api/v4/integration/machines/by-machine-number/SAMPLE-M-001");

        // Partial update (e.g. periodic hour-meter / service updates).
        if (machineId is not null)
            await UpdateMachineAsync(api, HttpMethod.Patch, $"api/v4/integration/machines/{machineId}",
                new { hourMeter = 3500, serviceDate = "2025-02-15T00:00:00Z" });

        // Price sync. A request that sets prices to 0 can be held for review and answered with 202;
        // UpdateMachineAsync handles that answer.
        if (machineId is not null)
            await UpdateMachineAsync(api, HttpMethod.Patch, $"api/v4/integration/machines/{machineId}",
                new { costPrice = 450.0, price = 650.0 });

        // Block check-in on a machine (e.g. while under service). Machines are preserve-on-omit on
        // both PUT and PATCH, so any field left out of the body keeps its stored value; JSON null is
        // treated as omitted, so send "" to clear a stored string. Machines only.
        if (machineId is not null)
            await UpdateMachineAsync(api, HttpMethod.Patch, $"api/v4/integration/machines/{machineId}", new
            {
                blockCheckIn = true,
                blockCheckInMessage = "Under service",
            });

        // ESG fuel fields have a dedicated endpoint.
        if (machineId is not null)
            await api.PatchAsync($"api/v4/integration/machines/{machineId}/esg", new
            {
                fuelConsumptionRate = 12.5,
                fuelConsumptionUnit = 0, // 0 = L/hour
                fuelType = 1,            // 1 = diesel
            });

        // Equipment is the same endpoint with isEquipment = true (typeId is not validated for equipment).
    }

    /// <summary>
    /// Sends a machine update (<c>PUT {id}</c>, <c>PATCH {id}</c> or <c>PUT update/array</c>) and handles both
    /// success answers. 200: the update was applied and the body is the machine (or the array of machines).
    /// 202: the company holds risky price changes for review, and this request set prices to 0 while the API
    /// client is over the limit. Its other fields were applied, its prices wait for an administrator to approve
    /// or reject them in Ditio, and the body is <c>{ pendingPriceChangeId, summary }</c>.
    /// A 202 is success, pending review: log the id and do not retry. While the client is over the limit, every
    /// further request that sets a price to 0 is held too.
    /// </summary>
    /// <returns>The updated machine (or array); null when the prices were held or the request failed.</returns>
    private static async Task<dynamic?> UpdateMachineAsync(DitioApiClient api, HttpMethod method, string path, object body)
    {
        var (status, response) = await api.SendWithStatusAsync(method, path, body);
        if (status == HttpStatusCode.Accepted)
        {
            Console.WriteLine($"  Prices held for review: pendingPriceChangeId {response?.pendingPriceChangeId} ({response?.summary})");
            Console.WriteLine("  The other fields were applied. Not retrying: an administrator approves or rejects the prices in Ditio.");
            return null;
        }

        return response;
    }
}
