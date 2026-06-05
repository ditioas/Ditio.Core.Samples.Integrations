using Ditio.Samples;
using Ditio.Samples.Examples;

var config = DitioConfig.Load();

if (string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
{
    Console.WriteLine("⚠ No credentials configured.");
    Console.WriteLine("  Copy appsettings.example.json to appsettings.json and fill in ClientId / ClientSecret / CompanyId");
    Console.WriteLine("  (or set DITIO_Ditio__ClientId, DITIO_Ditio__ClientSecret, DITIO_Ditio__CompanyId env vars).");
    Console.WriteLine();
}

var examples = new (string Key, string Name, Func<DitioConfig, Task> Run)[]
{
    ("1",  "Authentication (get a token)",                 AuthenticationExample.RunAsync),
    ("2",  "Projects",                                     ProjectsExample.RunAsync),
    ("3",  "Work orders (tasks)",                          WorkOrdersExample.RunAsync),
    ("4",  "Documents (attach to project / work order)",   DocumentsExample.RunAsync),
    ("5",  "Users (v4)",                                   UsersExample.RunAsync),
    ("6",  "Employees (v5)",                               EmployeesV5Example.RunAsync),
    ("7",  "Machines & equipment",                         MachinesExample.RunAsync),
    ("8",  "Certificates",                                 CertificatesExample.RunAsync),
    ("9",  "Payroll export",                               PayrollExportExample.RunAsync),
    ("10", "Data extraction (everything, out of Ditio)",   DataExtractionExample.RunAsync),
    ("11", "Reference data (machine types, alert types)",  ReferenceDataExample.RunAsync),
};

var choice = args.Length > 0 ? args[0] : null;
if (choice is null)
{
    Console.WriteLine("Ditio integration examples — pick one (or pass the number as an argument):");
    foreach (var example in examples)
        Console.WriteLine($"  {example.Key,2}. {example.Name}");
    Console.Write("> ");
    choice = Console.ReadLine();
}

var selected = examples.FirstOrDefault(e => e.Key == choice?.Trim());
if (selected.Run is null)
{
    Console.WriteLine("No example selected.");
    return;
}

Console.WriteLine($"\n=== Running: {selected.Name} ===\n");
await selected.Run(config);
