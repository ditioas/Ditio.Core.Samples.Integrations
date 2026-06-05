# 06 — Employees (v5)

`api/v5/integration/employees` · scope `ditioapiv3`. The modern replacement for the v4 Users API — consistent `employeeNumber` identifier, true PATCH, dedicated employment operations. **Use this for new integrations.** Set `$BASE_URL` / `$TOKEN` — see [`../01-authentication`](../01-authentication).

## Create (profile + first employment together)

```bash
curl -X POST $BASE_URL/api/v5/integration/employees \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{
    "employeeNumber": "1042",
    "firstName": "Ola", "lastName": "Nordmann",
    "phone": "+4798765432",
    "birthDate": "1990-05-15",
    "email": "ola.nordmann@example.com",
    "workTitle": "Machine Operator",
    "employment": { "startDate": "2025-03-01", "department": "Construction", "payroll": 1 }
  }'
```

`payroll`: `0` = Disabled, `1` = Enabled, `2` = Variable.

## Look up / search

```bash
curl $BASE_URL/api/v5/integration/employees                                  -H "Authorization: Bearer $TOKEN"   # all
curl "$BASE_URL/api/v5/integration/employees/1042?include=tags,employment-history" -H "Authorization: Bearer $TOKEN"
curl "$BASE_URL/api/v5/integration/employees/search?query=Ola"               -H "Authorization: Bearer $TOKEN"
```

## Update

```bash
# True PATCH — omitted fields unchanged; explicit null clears
curl -X PATCH $BASE_URL/api/v5/integration/employees/1042 -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "workTitle": "Project Manager", "email": null }'
```

## Employment operations

```bash
# Update current employment
curl -X PATCH $BASE_URL/api/v5/integration/employees/1042/update-employment -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "department": "Management" }'
# End employment (offboarding)
curl -X PATCH $BASE_URL/api/v5/integration/employees/1042/end-employment    -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "endDate": "2025-06-30" }'
# Rehire (new employment)
curl -X POST  $BASE_URL/api/v5/integration/employees/1042/create-employment -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{ "startDate": "2025-09-01", "payroll": 1 }'
```

Also: disable/enable, change-employee-number, change-phone-number, tags, project-company operations — see Swagger.

**C#:** [`EmployeesV5Example.cs`](EmployeesV5Example.cs).
