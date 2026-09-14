# 17 — Absences

Push absences that have already been granted in your payroll or HR system into Ditio, **in one call**.

`api/v5/integration/absences` · scope `ditioapiv3`. Set `$BASE_URL` / `$TOKEN` first — see [`../authentication`](../authentication). Runnable C# example: [`AbsencesExample.cs`](AbsencesExample.cs).

You send the identifiers you already hold — an employee number, a payroll code, a project number, a date range. Ditio resolves its own internal ids, expands the period into days, and runs the same validation an absence created in the app goes through. **You never look up a Ditio id.**

> **Auth:** every request needs a Bearer token with the `ditioapiv3` scope. Your API client must have **Administrator** access. A token with the wrong scope returns **401**, not 403 — the scope is validated as the token's audience.

## Before you start

Two ordering dependencies, not code problems:

1. **The employees must exist in Ditio**, with the same employee numbers your payroll system uses — see [`../employees-v5`](../employees-v5).
2. **The projects must exist**, if your absences are booked against projects — see [`../projects`](../projects).

Absence types need no setup: `absenceTypeCode` matches the payroll code they already carry.

## Create or update

```bash
curl -X POST "$BASE_URL/api/v5/integration/absences" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "externalId": "884213/1/7",
    "employeeNumber": "1042",
    "absenceTypeCode": "505",
    "startDate": "2026-09-14",
    "endDate": "2026-09-18",
    "projectNumber": "6040-110",
    "hoursPerDay": 7.5,
    "comment": "Innvilget i lønnssystemet"
  }'
```

`201 Created` on the first push, `200 OK` when an existing absence was updated.

The response echoes back names as well as ids — `userName`, `absenceTypeName`, `projectName` — so you can confirm the absence landed on the person and project you meant without a second lookup.

## `externalId` is the whole contract

It is what makes a repeated push **update** rather than duplicate, and what lets you change or cancel the absence later.

> **Use an id that survives an edit.** Do not build it from the absence's own dates, employee and type. That dedupes a replayed push, but the moment the period changes in your system the key changes too — the update arrives looking like a brand new absence, and you end up with two overlapping absences instead of one corrected one.
>
> Use whatever stable id your system assigns the absence itself: a record id, a case id, the id of the leave application.

Surrounding whitespace is trimmed. Nothing else is changed — including **case**, so `ABC-1` and `abc-1` are two different absences.

## Absence types

`absenceTypeCode` matches the **payroll code the absence type already carries** — the wage type (*lønnsart*) your payroll system already uses. No translation table on your side, and nothing extra for a Ditio administrator to configure.

> **A payroll code is a rollup, not an identifier.** Ditio models finer absence types than payroll pays on — "blood donation", "dentist" and "funeral" may all export as one welfare-leave code. Where a code does not identify exactly one active type the request is **refused**, and the error lists the candidate types with their ids. Send `absenceTypeId` for those.

Sending both `absenceTypeCode` and `absenceTypeId` is fine; the id wins.

## Projects

Send one `projectNumber` — whatever number you use. Which of Ditio's fields it corresponds to (project number, external project number, external dimension) is a Ditio-side setup detail you have no way of knowing, so we match against all of them at once. If it identifies more than one project the request is refused rather than resolved, and the error says which projects and which of those numbers were involved.

In a parent / project-company structure the same number is commonly reused across project companies. If it matches in more than one, the request is rejected naming the companies — send `projectCompanyId` to say which you mean.

## Adjusting individual days

The period defines the absence: `startDate`..`endDate` is expanded to **work days**, each at `hoursPerDay`. Send `days` only for days that differ.

| Entry | Effect |
|-------|--------|
| A date the period already books | Sets that day's hours |
| A date the period skipped — a weekend inside the range | Adds that day |
| `"qty": 0` | Removes that day (the person worked) |
| A date outside `startDate`..`endDate` | Ignored |

A day is `{ "date", "qty" }` in both directions — the same shape you send comes back in `days`.

> **`days` does not replace the period.** Listing one day adjusts one day; it does not narrow the absence to that day. If the absence really is a single day, say so with `startDate` and `endDate`.

**Always check `daysQty` in the response against what you expected** — a Monday-to-Sunday absence is five days, not seven.

## Read back and cancel

```bash
curl "$BASE_URL/api/v5/integration/absences/884213/1/7" -H "Authorization: Bearer $TOKEN"

curl -X DELETE "$BASE_URL/api/v5/integration/absences/884213/1/7" -H "Authorization: Bearer $TOKEN"
```

`DELETE` returns `204 No Content`, or `404` if no absence is held under that id.

Only absences created through this endpoint are reachable. One a person registered in the app carries no `externalId`, so it cannot be found, changed or deleted here — that falls out of the key rather than a permission check.

## Approval

`approved: true` marks the absence approved at the moment it is **created**.

On a later push the flag is ignored **in both directions**: it cannot approve an existing absence, and omitting it cannot revoke an approval granted in Ditio. Approval belongs to Ditio once the absence exists.

## What else can refuse a backdated push

Clearing the employment check does not on its own mean a backdated absence is written. The same rules apply as to an absence registered in Ditio, and two bite on the past:

- **Payroll already approved or locked** for any day in the period. Retroactive pushes hit this most often, because a leaver's final month is usually the month payroll has closed.
- **The absence itself is locked.**

Absence-type thresholds (quota per year, maximum consecutive days) are evaluated on every push.

## Full reference

The complete field list, error table, and the parent/project-company rules are in the Absences page of the integration documentation.
