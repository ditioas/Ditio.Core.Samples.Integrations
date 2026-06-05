# 07 — Machines & equipment

`api/v4/integration/machines` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` — see [`../01-authentication`](../01-authentication).

> A machine's `typeId` must match an existing machine type in your company (e.g. `beltemaskin`, `hjullaster`, `dumper`, `lastebil`). List valid types with `GET $BASE_URL/api/MachineType`. **Equipment** (`isEquipment: true`) does not validate `typeId`.

## Create

```bash
curl -X POST $BASE_URL/api/v4/integration/machines \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "companyId": "YOUR_COMPANY_ID", "machineNumber": "M-001", "name": "Volvo EC220E", "typeId": "beltemaskin", "active": true, "buildYear": 2022 }'
```

Required: `companyId`, `machineNumber`. Batch create: `POST .../machines/create/array` with an array.

## Look up

```bash
curl $BASE_URL/api/v4/integration/machines                                 -H "Authorization: Bearer $TOKEN"
curl $BASE_URL/api/v4/integration/machines/by-machine-number/M-001         -H "Authorization: Bearer $TOKEN"
curl "$BASE_URL/api/v4/integration/machines?includeEquipmentDetails=true"  -H "Authorization: Bearer $TOKEN"
```

## Update

```bash
# Partial update (e.g. hour meter / service)
curl -X PATCH $BASE_URL/api/v4/integration/machines/{id} -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "hourMeter": 3500, "serviceDate": "2025-02-15T00:00:00Z" }'

# ESG fuel fields have a dedicated endpoint
curl -X PATCH $BASE_URL/api/v4/integration/machines/{id}/esg -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "fuelConsumptionRate": 12.5, "fuelConsumptionUnit": 0, "fuelType": 1 }'
```

`fuelConsumptionUnit`: 0 L/h, 1 L/km, 2 kWh/h, 3 kWh/km, 4 kg/h, 5 kg/km. `fuelType`: 0 none, 1 diesel, 2 biodiesel, 3 gasoline, 4 electric, 5 gas, 6 natural gas, 7 dyed diesel.

Deactivate retired machines (`active:false`) rather than deleting.

**C#:** [`MachinesExample.cs`](MachinesExample.cs).
