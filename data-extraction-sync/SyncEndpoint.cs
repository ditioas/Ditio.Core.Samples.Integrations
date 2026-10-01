namespace Ditio.Samples.Examples.DataExtractionSync;

/// <summary>
/// One Data Extraction endpoint, the table its raw records go into, and the columns its view
/// pulls out of the JSON for reporting. Add fields to <see cref="ViewColumns"/> as you need
/// them; every field of every record is in the table's <c>json</c> column already.
/// </summary>
internal sealed record SyncEndpoint(string Path, string Table, string[] ViewColumns)
{
    public string View => $"vw_{Table}";

    // Registrations first, then the dimensions they refer to.
    public static readonly SyncEndpoint[] All =
    {
        new("v1/time-registrations", "time_registrations",
        [
            "workDate", "startDateTime", "stopDateTime",
            "projectId", "projectNumber", "projectName",
            "taskId", "taskWbsNumber", "taskName",
            "resourceId", "resourceNumber", "resourceName",
            "userId", "userName", "userEmployeeNumber",
            "qty", "unitName", "amount", "costAmount",
            "approved", "payrollApproved", "locked", "invoiced",
            "description", "modifiedDateTime",
        ]),
        new("v1/absence-registrations", "absence_registrations",
        [
            "date", "startTime", "userId", "employeeNumber", "projectId",
            "absenceTypeId", "absenceTypeCode", "absenceTypeName", "qty",
            "approved", "payrollApproved", "locked", "modifiedDateTime",
        ]),
        new("v1/machine-registrations", "machine_registrations",
        [
            "startDateTime", "stopDateTime",
            "machineId", "resourceNumber", "machineName", "machineTypeName",
            "projectId", "projectNumber", "projectName",
            "driverId", "driverName", "qty", "approved", "modifiedDateTime",
        ]),
        new("v1/incident-registrations", "incident_registrations",
        [
            "serialNumber", "title", "typeName", "baseType", "status", "risk",
            "projectId", "projectNumber", "projectName", "taskId",
            "createdAt", "modifiedDateTime",
        ]),
        new("v1/checklist-registrations", "checklist_registrations",
        [
            "documentNumber", "serialNumber", "templateName", "baseTypeName", "statusText", "hasDeviation",
            "projectId", "projectNumber", "projectName", "activityId", "activityNumber", "activityName",
            "createdDateTime", "submittedDateTime", "modifiedDateTime",
        ]),
        new("v1/flow-trip-registrations", "flow_trip_registrations",
        [
            "loadDateTime", "dumpDateTime",
            "projectId", "projectNumber", "projectName", "taskId",
            "massType", "quantity", "unitOfMeasureText",
            "dumperId", "dumperName", "loaderId", "loaderName", "distance",
            "modifiedDateTime",
        ]),
        new("v1/project", "projects",
        [
            "number", "name", "externalNumber", "companyId", "companyName", "active", "modifiedDateTime",
        ]),
        new("v1/project/work-breakdown-structure", "work_orders",
        [
            "projectId", "number", "externalNumber", "name", "nameWithNumber", "fullPathName", "modifiedDateTime",
        ]),
        new("v1/resource", "resources",
        [
            "number", "name", "typeName", "typeBaseName", "department", "active", "modifiedDateTime",
        ]),
        new("v1/user", "users",
        [
            "identityUserId", "firstName", "lastName", "email", "workTitle", "isDisabled",
            "companyId", "companyName", "modifiedDateTime",
        ]),
    };
}
