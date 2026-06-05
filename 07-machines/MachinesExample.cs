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
            await api.PatchAsync($"api/v4/integration/machines/{machineId}", new { hourMeter = 3500, serviceDate = "2025-02-15T00:00:00Z" });

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
}
