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
curl $BASE_URL/api/v4/integration/machines/{id}                            -H "Authorization: Bearer $TOKEN"
```

`GET .../machines/{id}` reaches machines and equipment owned by a company in your company structure (with prices) and ones another company shares with you, such as a subcontractor's machine on your projects (`price`, `costPrice` and `costPriceMachine` are `null`). Any other id returns `404`, the same as an unknown id.

## Update

Machine updates require a relationship to the stored machine before applying request fields: the effective caller's company owns it, it is already shared with that company, or the caller has the applicable administration access through the stored owner's company hierarchy. Existing write permissions must also pass; sharing alone does not grant permission to edit the machine. Supplying a different `companyId`, or knowing the ID of a machine with no transactions, does not grant access.

`PUT`, `PATCH` and ESG updates return the same generic HTTP 404 for an absent machine and for one you can't reach. Array updates check each item separately; earlier items can already have succeeded if a later item fails. Supported ownership transfers remain available after the stored target passes the check.

On a machine another company shares with you, prices in the request are ignored (the stored prices stay, the response returns `null`), and a `companyId` other than the owner's returns 404: sharing never moves a machine to your company.

Equipment (`isEquipment: true`) can be updated only when it belongs to your company structure. `PUT` and array updates answer equipment outside it like an unknown equipment id (HTTP 400). `PATCH` answers 404, or 400 for equipment another company shares with you.

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

### Price changes held for review (`202`)

A company can choose to hold risky price changes from integrations until an administrator has reviewed them. For such a company, `PUT /{id}`, `PATCH /{id}` and `PUT update/array` can answer **`202 Accepted`** instead of `200`. That happens when the request sets `price`, `costPrice` or `costPriceMachine` from a non-zero value to `0`, and, counting this request, your API client has set more than 5 prices to `0` in the last 24 hours, or prices on more than 10 % of the company's priced machines and equipment. Omitted prices and prices that are already `0` never count.

When a request is held, its other fields are applied, but none of its prices are (on every item of an array update). The body is not the machine:

```json
{
  "pendingPriceChangeId": "68b2c3d4e5f6a7b8c9d0e5f1",
  "summary": "1 price(s) on 1 resource(s) set to 0 or not charged; 6 price(s) on 6 resource(s) from this client in 24 hours"
}
```

An administrator approves the prices (they are applied as you sent them) or rejects them in Ditio. The Integration API has no endpoint to look up or decide a held change; read the machine with `GET .../machines/{id}` later to see its prices.

Treat `202` as **success, pending review**: log `pendingPriceChangeId` and **do not retry**. While your client is over the limit, every further request that sets a price to `0` is held too. Full reference: [Price changes held for review](https://docs.ditio.app/api-reference/integration/machines/#price-changes-held-for-review).

**C#:** [`MachinesExample.cs`](MachinesExample.cs). It creates its own machine in your company and updates only that one, so the rules above for other companies' machines don't change what it does. Its `UpdateMachineAsync` helper sends the `PATCH` machine updates and handles `202`: it logs `pendingPriceChangeId` and the summary, and does not retry. Use it the same way for `PUT /{id}` and `PUT update/array`.
