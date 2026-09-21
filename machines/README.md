# 07 — Machines & equipment

`api/v4/integration/machines` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` — see [`../authentication`](../authentication).

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

# Block / unblock check-in (machines only; ignored for equipment)
curl -X PATCH $BASE_URL/api/v4/integration/machines/{id} -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "blockCheckIn": true, "blockCheckInMessage": "Under service" }'

# ESG fuel fields have a dedicated endpoint
curl -X PATCH $BASE_URL/api/v4/integration/machines/{id}/esg -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "fuelConsumptionRate": 12.5, "fuelConsumptionUnit": 0, "fuelType": 1 }'
```

`fuelConsumptionUnit`: 0 L/h, 1 L/km, 2 kWh/h, 3 kWh/km, 4 kg/h, 5 kg/km. `fuelType`: 0 none, 1 diesel, 2 biodiesel, 3 gasoline, 4 electric, 5 gas, 6 natural gas, 7 dyed diesel.

### Omitted fields are preserved

Unlike the other entities in this repo, machines are **preserve-on-omit** on both `PUT` and `PATCH`:
a field you leave out of the body keeps its stored value rather than being reset. A sync that
refreshes only one field cannot clear the rest.

```bash
# Only the serial number changes. active, prices and registration number keep their stored values.
curl -X PUT $BASE_URL/api/v4/integration/machines/update/array -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '[{ "id": "{id}", "companyId": "YOUR_COMPANY_ID", "machineNumber": "M-001", "serialNumber": "VCEC220EL00012345" }]'
```

To change a value, send it. To deactivate a machine, send `"active": false` explicitly — omitting
`active` does **not** deactivate it. JSON `null` counts as omitted; send `""` to clear a stored
string such as `blockCheckInMessage`. The response echoes the machine's stored state after the
update, not the fields you sent.

On **create**, omitted fields still take their defaults — `active` defaults to `false`, so send
`"active": true` for a machine that should be usable straight away.

Deactivate retired machines (`active:false`) rather than deleting.

**C#:** [`MachinesExample.cs`](MachinesExample.cs).
