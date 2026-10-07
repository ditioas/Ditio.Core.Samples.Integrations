"""Tables, columns, labels and relationships of the Ditio Power BI template.

Each column is (api field, type, label):
  - api field: the field name in the Data Extraction response; the Power Query keeps it.
  - type: a key of COLUMN_TYPES.
  - label: what the customer sees in Power BI. "hdr:<Key>" takes the label Ditio's own
    Excel export uses (translations/export-headers.<lang>.json), so a column reads the
    same in Power BI as in the Excel file. Anything else is the label itself.

Columns whose api field is "id", ends in "Id" or is "isDeleted" are hidden: they are keys
or plumbing, not something to put on a report. VISIBLE_ID_FIELDS lists the exceptions that
hold business values (chapter, piece-work id).
"""

PRODUCTION_CORE_URL = "https://integration.ditio.no"
TEST_CORE_URL = "https://core-api.ditio.dev/core"
PRODUCTION_REPORTING_URL = "https://core-api.ditio.app/reporting"
TEST_REPORTING_URL = "https://core-api.ditio.dev/reporting"
PRODUCTION_IDENTITY_URL = "https://identity.ditio.app"
TEST_IDENTITY_URL = "https://identity.ditio.dev"

# Column type -> (Power Query type, tabular model dataType)
COLUMN_TYPES = {
    "text": ("type text", "string"),
    "number": ("type number", "double"),
    "int": ("Int64.Type", "int64"),
    "bool": ("type logical", "boolean"),
    "datetime": ("type datetime", "dateTime"),
    "date": ("type date", "dateTime"),
}

# Power BI incremental refresh for the registration tables (applies once published to the Power BI
# service): keep this many months, re-read only this many most recent months on each refresh.
# Month partitions keep the number of API calls low (one partition = one paged extraction per table);
# three months covers late approvals and payroll locks. Customers can change both in Power BI Desktop
# (table > Incremental refresh). The Dato table spans three years back, so keep STORE_MONTHS <= 36.
STORE_MONTHS = 24
REFRESH_MONTHS = 3

# The date-only column every registration table gets, related to the Dato table.
DATE_KEY_COLUMN = "dateKey"
DATE_KEY_LABEL = "Dato"

RESOURCE_GROUPS = {
    "Human": "Person", "Machine": "Maskin", "Vehicle": "Kjøretøy", "SeaVessel": "Fartøy",
    "AirVessel": "Luftfartøy", "StaticObject": "Fast installasjon", "Other": "Annet",
}
INCIDENT_BASE_TYPES = {"HSE": "HMS", "QA": "Kvalitet", "Environmental": "Miljø", "Machine": "Maskin", "Other": "Annet"}
INCIDENT_STATUSES = {"Open": "Åpen", "InProgress": "Pågår", "Closed": "Lukket"}
CHECKLIST_STATUSES = {
    "Pending": "Ikke sendt", "SentForApproval": "Sendt til godkjenning", "Approved": "Godkjent",
    "Rejected": "Avvist", "Reported": "Rapportert",
}
MASS_UNITS = {"Ton": "tonn", "CubicMeter": "m³"}
ITEM_TRANS_TYPES = {"Consumption": "Forbruk", "Sale": "Salg", "Purchase": "Innkjøp", "Adjustment": "Justering"}

TABLES = [
    {
        "name": "Prosjekter",
        "path": "v1/project",
        "window": False,
        "description": "Prosjekter API-klienten har tilgang til (v1/project).",
        "columns": [
            ("id", "text", "Prosjekt-id"),
            ("number", "text", "hdr:ProjectNo"),
            ("name", "text", "Prosjektnavn"),
            ("externalNumber", "text", "hdr:ExternalProjectNo"),
            ("externalDim01", "text", "hdr:ExternalDim1"),
            ("externalDim02", "text", "hdr:ExternalDim2"),
            ("companyId", "text", "Firma-id"),
            ("companyName", "text", "hdr:Company"),
            ("active", "bool", "Aktiv"),
            ("isExternal", "bool", "Eksternt prosjekt"),
            ("createdDateTime", "datetime", "Opprettet"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
    },
    {
        "name": "Arbeidsordrer",
        "path": "v1/project/work-breakdown-structure",
        "window": False,
        "description": "Arbeidsordrer (v1/project/work-breakdown-structure).",
        "columns": [
            ("id", "text", "Arbeidsordre-id"),
            ("projectId", "text", "Prosjekt-id"),
            ("number", "text", "Arbeidsordrenr"),
            ("name", "text", "Arbeidsordre"),
            ("nameWithNumber", "text", "Arbeidsordre med nr"),
            ("chapterId", "text", "Kapittel"),
            ("externalNumber", "text", "Produksjonskode"),
            ("externalDim01", "text", "hdr:ExternalDim1"),
            ("externalDim02", "text", "hdr:ExternalDim2"),
            ("externalPieceWorkId", "text", "hdr:PieceworkId"),
            ("parentName", "text", "Overordnet arbeidsordre"),
            ("fullPathName", "text", "hdr:Breadcrumb"),
            ("companyId", "text", "Firma-id"),
            ("companyName", "text", "hdr:Company"),
            ("isExternal", "bool", "Ekstern"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
    },
    {
        "name": "Ressurser",
        "path": "v1/resource",
        "window": False,
        "description": "Ressurser: personer, maskiner, kjøretøy m.m. (v1/resource).",
        "columns": [
            ("id", "text", "Ressurs-id"),
            ("number", "text", "hdr:ResourceNo"),
            ("name", "text", "hdr:ResourceName"),
            ("typeName", "text", "hdr:ResourceType"),
            ("typeBaseName", "text", "Grunntype"),
            ("registrationNumber", "text", "Registreringsnummer"),
            ("serialNumber", "text", "Serienummer"),
            ("department", "text", "Avdeling"),
            ("emissionClass", "text", "Utslippsklasse"),
            ("buildYear", "int", "Byggeår"),
            ("weight", "number", "Vekt"),
            ("capacity", "number", "Kapasitet"),
            ("costPrice", "number", "hdr:CostPrice"),
            ("price", "number", "Pris"),
            ("companyId", "text", "Firma-id"),
            ("companyName", "text", "hdr:Company"),
            ("active", "bool", "Aktiv"),
            ("isExternal", "bool", "Ekstern ressurs"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
        "mapped": [("resourceGroup", "typeBaseName", RESOURCE_GROUPS, "Ressursgruppe")],
        "hidden": ["typeBaseName"],
    },
    {
        "name": "Brukere",
        "path": "v1/user",
        "window": False,
        "description": ("Brukere, én rad per person (v1/user). En person med profiler i flere firma (prosjektfirma, "
                        "datterselskap) vises med profilen i arbeidsgiverfirmaet. Personopplysninger som fødselsdato "
                        "og adresse er utelatt."),
        # Registrations carry the person's identity id; v1/user has one row per company profile.
        "per_person": {"key": "identityUserId", "count_field": "profileCount", "count_label": "Antall profiler"},
        "columns": [
            ("id", "text", "Profil-id"),
            ("identityUserId", "text", "Bruker-id"),
            ("employeeNumber", "text", "hdr:EmployeeNumber"),
            ("name", "text", "Navn"),
            ("firstName", "text", "Fornavn"),
            ("lastName", "text", "Etternavn"),
            ("email", "text", "E-post"),
            ("workTitle", "text", "hdr:Position"),
            ("department", "text", "Avdeling"),
            ("companyId", "text", "Firma-id"),
            ("companyName", "text", "hdr:Company"),
            ("employmentCompanyId", "text", "Arbeidsgiver-id"),
            ("employmentCompanyName", "text", "Ansatt i firma"),
            ("isActiveEmployment", "bool", "Aktivt ansatt"),
            ("isDisabled", "bool", "Deaktivert"),
            ("startDate", "datetime", "Ansatt fra"),
            ("endDate", "datetime", "Ansatt til"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
    },
    {
        "name": "Timeføringer",
        "path": "v1/time-registrations",
        "window": True,
        "date_key": "workDate",
        "description": "Prosjekttransaksjoner: timer for personer, maskiner og kjøretøy (v1/time-registrations).",
        "columns": [
            ("id", "text", "Transaksjons-id"),
            ("workDate", "datetime", "Arbeidsdato"),
            ("startDateTime", "datetime", "Start"),
            ("stopDateTime", "datetime", "Slutt"),
            ("qty", "number", "hdr:Hours"),
            ("unitName", "text", "hdr:Unit"),
            ("unitQty", "number", "hdr:UnitCount"),
            ("unitPrice", "number", "Enhetspris"),
            ("price", "number", "Pris"),
            ("amount", "number", "hdr:Amount"),
            ("costPriceBase", "number", "Kostpris grunnlag"),
            ("costPriceMan", "number", "hdr:CostPricePersonnel"),
            ("costPriceMachine", "number", "hdr:CostPriceMachine"),
            ("costAmount", "number", "hdr:Cost"),
            ("costAmountMan", "number", "hdr:CostPersonnel"),
            ("costAmountMachine", "number", "hdr:CostMachine"),
            ("breakQty", "number", "hdr:BreaksTotal"),
            ("description", "text", "hdr:ExternalComment"),
            ("descriptionInternal", "text", "hdr:InternalComment"),
            ("approved", "bool", "Godkjent"),
            ("approvedByName", "text", "hdr:ApprovedBy"),
            ("approvedDateTime", "datetime", "hdr:ApprovedDate"),
            ("payrollApproved", "bool", "Lønnsgodkjent"),
            ("payrollApprovedDateTime", "datetime", "Lønnsgodkjent dato"),
            ("locked", "bool", "Låst"),
            ("lockedByName", "text", "hdr:LockedBy"),
            ("lockedDateTime", "datetime", "hdr:LockedDate"),
            ("invoiced", "bool", "hdr:Invoiced"),
            ("projectId", "text", "Prosjekt-id"),
            ("projectNumber", "text", "hdr:ProjectNo"),
            ("projectName", "text", "Prosjektnavn"),
            ("projectExternalNumber", "text", "hdr:ExternalProjectNo"),
            ("projectCompanyId", "text", "Prosjektfirma-id"),
            ("projectCompanyName", "text", "hdr:ProjectCompany"),
            ("projectLeaderName", "text", "Prosjektleder"),
            ("projectMainForemanName", "text", "Anleggsleder"),
            ("taskId", "text", "Arbeidsordre-id"),
            ("taskName", "text", "Arbeidsordre"),
            ("taskNameLong", "text", "hdr:Breadcrumb"),
            ("taskWbsNumber", "text", "Arbeidsordrenr"),
            ("taskChapterId", "text", "Kapittel"),
            ("taskPieceWorkId", "text", "hdr:PieceworkId"),
            ("resourceId", "text", "Ressurs-id"),
            ("resourceNumber", "text", "hdr:ResourceNo"),
            ("resourceName", "text", "hdr:ResourceName"),
            ("resourceTypeName", "text", "hdr:ResourceType"),
            ("primaryResourceName", "text", "Primærressurs"),
            ("machineEmissionClass", "text", "Utslippsklasse"),
            ("userId", "text", "Bruker-id"),
            ("userName", "text", "Navn"),
            ("userEmployeeNumber", "text", "hdr:EmployeeNumber"),
            ("userCompanyName", "text", "hdr:Company"),
            ("userCompanyOrgNr", "text", "hdr:CompanyNo"),
            ("userWorkTitle", "text", "hdr:Position"),
            ("userWorkShiftSettingName", "text", "Arbeidstidsordning"),
            ("createdDateTime", "datetime", "Opprettet"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
    },
    {
        "name": "Maskinregistreringer",
        "path": "v1/machine-registrations",
        "window": True,
        # The endpoint filters on the stop time, so partitions must be keyed on it too: a
        # registration from 22:00 to 02:00 would otherwise fall into no partition.
        "date_key": "stopDateTime",
        "description": "Maskinregistreringer (v1/machine-registrations).",
        "columns": [
            ("id", "text", "Registrerings-id"),
            ("startDateTime", "datetime", "Start"),
            ("stopDateTime", "datetime", "Slutt"),
            ("qty", "number", "Timer"),
            ("description", "text", "Kommentar"),
            ("approved", "bool", "Godkjent"),
            ("machineId", "text", "Maskin-id"),
            ("machineName", "text", "Maskin"),
            ("resourceNumber", "text", "hdr:ResourceNo"),
            ("machineTypeName", "text", "hdr:ResourceType"),
            ("driverId", "text", "Fører-id"),
            ("driverName", "text", "Fører"),
            ("driverCompanyName", "text", "Førers firma"),
            ("projectId", "text", "Prosjekt-id"),
            ("projectNumber", "text", "hdr:ProjectNo"),
            ("projectName", "text", "Prosjektnavn"),
            ("projectExternalNumber", "text", "hdr:ExternalProjectNo"),
            ("projectCompanyName", "text", "hdr:ProjectCompany"),
            ("createdDateTime", "datetime", "Opprettet"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
    },
    {
        "name": "Fravær",
        "path": "v1/absence-registrations",
        "window": True,
        "date_key": "date",
        "description": "Fravær, én rad per fraværsdag (v1/absence-registrations). Fritekst og sykemeldingsdetaljer er utelatt.",
        "columns": [
            ("id", "text", "Fraværsdag-id"),
            ("registrationId", "text", "Registrerings-id"),
            ("date", "datetime", "Fraværsdato"),
            ("startTime", "datetime", "Start"),
            ("qty", "number", "Timer"),
            ("absenceTypeId", "text", "Fraværstype-id"),
            ("absenceTypeCode", "text", "Fraværskode"),
            ("absenceTypeName", "text", "Fraværstype"),
            ("employeeNumber", "text", "hdr:EmployeeNumber"),
            ("userId", "text", "Bruker-id"),
            ("projectId", "text", "Prosjekt-id"),
            ("approved", "bool", "Godkjent"),
            ("approvedByName", "text", "hdr:ApprovedBy"),
            ("approvedDateTime", "datetime", "hdr:ApprovedDate"),
            ("payrollApproved", "bool", "Lønnsgodkjent"),
            ("payrollApprovedDateTime", "datetime", "Lønnsgodkjent dato"),
            ("locked", "bool", "Låst"),
            ("lockedDateTime", "datetime", "hdr:LockedDate"),
            ("pdfUrl", "text", "PDF"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
    },
    {
        "name": "Varsler",
        "path": "v1/incident-registrations",
        "window": True,
        # The endpoint ignores FromDateTime/ToDateTime; ModifiedSince narrows the load instead.
        "window_by_modified": True,
        "date_key": "createdAt",
        "description": "Varsler: HMS, kvalitet, miljø, maskin m.m. (v1/incident-registrations). Risiko og skadetype er utelatt fordi de sjelden fylles ut.",
        "columns": [
            ("id", "text", "Varsel-id"),
            ("serialNumberWithPrefix", "text", "Varselnr"),
            ("title", "text", "Tittel"),
            ("description", "text", "Beskrivelse"),
            ("typeName", "text", "Varselstype"),
            ("baseTypeText", "text", "Hovedtype (API)"),
            ("statusText", "text", "Status (API)"),
            ("measures", "text", "Tiltak"),
            ("requiresFurtherAction", "bool", "Krever oppfølging"),
            ("resolvedOnLocation", "bool", "Løst på stedet"),
            ("companyName", "text", "hdr:Company"),
            ("projectId", "text", "Prosjekt-id"),
            ("projectNumber", "text", "hdr:ProjectNo"),
            ("projectName", "text", "Prosjektnavn"),
            ("projectCompanyName", "text", "hdr:ProjectCompany"),
            ("taskId", "text", "Arbeidsordre-id"),
            ("taskName", "text", "Arbeidsordre"),
            ("machineId", "text", "Maskin-id"),
            ("machineName", "text", "Maskin"),
            ("latitude", "number", "Breddegrad"),
            ("longitude", "number", "Lengdegrad"),
            ("createdAt", "datetime", "Opprettet"),
            ("statusUpdatedAt", "datetime", "Status endret"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("pdfUrl", "text", "PDF"),
            ("isDeleted", "bool", "Slettet"),
        ],
        "mapped": [
            ("baseTypeName", "baseTypeText", INCIDENT_BASE_TYPES, "Hovedtype"),
            ("statusName", "statusText", INCIDENT_STATUSES, "Status"),
        ],
        "hidden": ["baseTypeText", "statusText"],
    },
    {
        "name": "Sjekklister",
        "path": "v1/checklist-registrations",
        "window": True,
        # The endpoint ignores FromDateTime/ToDateTime; ModifiedSince narrows the load instead.
        "window_by_modified": True,
        "date_key": "createdDateTime",
        "description": "Sjekklister og skjema (v1/checklist-registrations).",
        "columns": [
            ("id", "text", "Sjekkliste-id"),
            ("documentNumber", "text", "Dokumentnr"),
            ("serialNumber", "text", "Løpenr"),
            ("templateName", "text", "Mal"),
            ("templateVersion", "int", "Malversjon"),
            ("baseTypeName", "text", "Maltype"),
            ("statusText", "text", "Status (API)"),
            ("hasDeviation", "bool", "Har avvik"),
            ("hasComment", "bool", "Har kommentar"),
            ("hasImages", "bool", "Har bilder"),
            ("projectId", "text", "Prosjekt-id"),
            ("projectNumber", "text", "hdr:ProjectNo"),
            ("projectName", "text", "Prosjektnavn"),
            ("activityNumber", "text", "Arbeidsordrenr"),
            ("activityName", "text", "Arbeidsordre"),
            ("machineId", "text", "Maskin-id"),
            ("resourceNumber", "text", "hdr:ResourceNo"),
            ("createdByUserName", "text", "Opprettet av"),
            ("createdDateTime", "datetime", "Opprettet"),
            ("submittedByUserName", "text", "Sendt inn av"),
            ("submittedDateTime", "datetime", "Sendt inn"),
            ("approvedByUserName", "text", "hdr:ApprovedBy"),
            ("latitude", "number", "Breddegrad"),
            ("longitude", "number", "Lengdegrad"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("pdfUrl", "text", "PDF"),
            ("isDeleted", "bool", "Slettet"),
        ],
        "mapped": [("statusName", "statusText", CHECKLIST_STATUSES, "Status")],
        "hidden": ["statusText"],
    },
    {
        "name": "Massetransport",
        "path": "v1/flow-trip-registrations",
        "window": True,
        "date_key": "loadDateTime",
        "description": "Massetransport: turer fra Ditio Flow og eldre massetransport (v1/flow-trip-registrations).",
        "columns": [
            ("id", "text", "Tur-id"),
            ("loadDateTime", "datetime", "Lastet"),
            ("dumpDateTime", "datetime", "Tippet"),
            ("costStartDateTime", "datetime", "Kost start"),
            ("costEndDateTime", "datetime", "Kost slutt"),
            ("quantity", "number", "Mengde"),
            ("quantityM3", "number", "Volum m³ (omregnet)"),
            ("registeredQuantity", "number", "Registrert mengde"),
            ("verifiedQuantity", "number", "Verifisert mengde"),
            ("unitOfMeasureText", "text", "Enhet (API)"),
            ("massTypeId", "text", "Massetype-id"),
            ("massType", "text", "Massetype"),
            ("m3PerHour", "number", "m³ per time"),
            ("totalCycleTimeSeconds", "int", "Syklustid (sek)"),
            ("totalCycleTimeHours", "number", "Syklustid (timer)"),
            ("utilizationRate", "number", "Utnyttelsesgrad"),
            ("distance", "number", "Tur avstand"),
            ("averageSpeedKmh", "number", "Tur fart (km/t)"),
            ("netAltitudeChange", "number", "Netto høydeendring"),
            ("totalAscent", "number", "Total stigning"),
            ("maxGradient", "number", "Maks stigningsgrad"),
            ("averageGradient", "number", "Snitt stigningsgrad"),
            ("loadLocationName", "text", "Lasteområde"),
            ("dumpLocationName", "text", "Dumpeområde"),
            ("loadLongitude", "number", "Last lengdegrad"),
            ("loadLatitude", "number", "Last breddegrad"),
            ("dumpLongitude", "number", "Dump lengdegrad"),
            ("dumpLatitude", "number", "Dump breddegrad"),
            ("loaderId", "text", "Laster-id"),
            ("loaderName", "text", "Laster"),
            ("loaderNumber", "text", "Lasternr"),
            ("loaderDriverName", "text", "Lasterfører"),
            ("dumperId", "text", "Dumper-id"),
            ("dumperName", "text", "Dumper"),
            ("dumperNumber", "text", "Dumpernr"),
            ("dumperRegistrationNumber", "text", "Dumper reg.nr"),
            ("dumperDriverName", "text", "Dumperfører"),
            ("dumperDriverCompanyName", "text", "Dumperførers firma"),
            ("trailer", "bool", "Tilhenger"),
            ("verified", "bool", "Verifisert"),
            ("approved", "bool", "Godkjent"),
            ("invoiced", "bool", "hdr:Invoiced"),
            ("receiptNumber", "text", "Kvitteringsnr"),
            ("customerName", "text", "Kunde"),
            ("comment", "text", "Kommentar"),
            ("projectId", "text", "Prosjekt-id"),
            ("projectNumber", "text", "hdr:ProjectNo"),
            ("projectName", "text", "Prosjektnavn"),
            ("projectExternalNumber", "text", "hdr:ExternalProjectNo"),
            ("projectCompanyName", "text", "hdr:ProjectCompany"),
            ("taskId", "text", "Arbeidsordre-id"),
            ("taskName", "text", "Arbeidsordre"),
            ("isFlowData", "bool", "Fra Ditio Flow"),
            ("modifiedDateTime", "datetime", "Sist endret"),
            ("isDeleted", "bool", "Slettet"),
        ],
        # GeoJSON points are [longitude, latitude]; split them into number columns.
        "coordinates": {"loadCoordinates": "load", "dumpCoordinates": "dump"},
        "mapped": [("unitName", "unitOfMeasureText", MASS_UNITS, "Enhet")],
        "hidden": ["unitOfMeasureText"],
    },
]

# v1/item-registrations/web-query returns semicolon-separated text, not JSON, and is
# not paginated. Its header row is localised and does not line up with the data rows,
# so the query skips it and names the columns by position.
ITEM_TABLE = {
    "name": "Varetransaksjoner",
    "path": "v1/item-registrations/web-query",
    "date_key": "transDate",
    "description": "Varetransaksjoner: forbruk, salg, innkjøp og justering (v1/item-registrations/web-query).",
    "columns": [
        ("itemName", "text", "Vare"),
        ("projectName", "text", "Prosjektnavn"),
        ("projectNumber", "text", "hdr:ProjectNo"),
        ("taskName", "text", "Arbeidsordre"),
        ("transType", "text", "Transaksjonstype (API)"),
        ("userName", "text", "Navn"),
        ("transDate", "datetime", "Transaksjonsdato"),
        ("machineName", "text", "Maskin"),
        ("qty", "number", "Antall"),
        ("costPrice", "number", "hdr:CostPrice"),
        ("price", "number", "Pris"),
        ("costAmount", "number", "hdr:Cost"),
        ("amount", "number", "hdr:Amount"),
        ("warehouseName", "text", "Lager"),
        ("warehouseQty", "number", "Lagerantall"),
        ("description", "text", "Kommentar"),
    ],
    "mapped": [("transTypeName", "transType", ITEM_TRANS_TYPES, "Transaksjonstype")],
    "hidden": ["transType"],
}

# Many-to-one, fact -> dimension, by api field. Only keys proven unique on the dimension
# side and populated from the same id space on the fact side. Arbeidsordrer -> Prosjekter
# is deliberately not related: it would give Timeføringer two paths to Prosjekter.
# Every registration table is also related to Dato on its date key (added by the builder).
RELATIONSHIPS = [
    ("Timeføringer", "projectId", "Prosjekter", "id"),
    ("Timeføringer", "taskId", "Arbeidsordrer", "id"),
    ("Timeføringer", "resourceId", "Ressurser", "id"),
    ("Timeføringer", "userId", "Brukere", "identityUserId"),
    ("Fravær", "userId", "Brukere", "identityUserId"),
    ("Maskinregistreringer", "projectId", "Prosjekter", "id"),
    ("Maskinregistreringer", "machineId", "Ressurser", "id"),
    ("Fravær", "projectId", "Prosjekter", "id"),
    ("Varsler", "projectId", "Prosjekter", "id"),
    ("Varsler", "taskId", "Arbeidsordrer", "id"),
    ("Sjekklister", "projectId", "Prosjekter", "id"),
    ("Massetransport", "projectId", "Prosjekter", "id"),
    ("Massetransport", "taskId", "Arbeidsordrer", "id"),
]


# Fields named like ids that hold business values customers report on.
VISIBLE_ID_FIELDS = {"chapterId", "taskChapterId", "taskPieceWorkId", "externalPieceWorkId"}


def is_hidden(table, api):
    if api in VISIBLE_ID_FIELDS:
        return False
    return (api == "id" or api.endswith("Id") or api in ("isDeleted", DATE_KEY_COLUMN)
            or api in table.get("hidden", []))
