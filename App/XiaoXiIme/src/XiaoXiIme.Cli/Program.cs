using DotNetCampus.Cli;
using XiaoXiIme.Cli;

if (args.Length == 0)
{
    args = ["--help"];
}

return await CommandLine.Parse(args)
    .AddHelpHandler()
    .AddHandler<InstallOptions>(Install)
    .AddHandler<SystemTestPlanOptions>(PrintSystemTestPlan)
    .AddHandler<SystemTestRunOptions>(RunSystemTests)
    .AddHandler<PayloadBuildOptions>(options => IntegrationPayloadBuilder.BuildAsync(options, Console.Out, Console.Error))
    .AddHandler<IntegrationRunOptions>(options => IntegrationTestRunner.RunAsync(options, Console.Out, Console.Error, static () => new WindowsImeInstaller()))
    .RunAsync();

static int Install(InstallOptions options)
{
    if (!options.AllowSystemChanges)
    {
        Console.Error.WriteLine("Installation refused. Pass --allow-system-changes explicitly.");
        return 3;
    }
    var environment = Environment.GetEnvironmentVariable("XIAOXIIME_ENVIRONMENT");
    if (environment is not ("Test" or "VirtualMachine"))
    {
        Console.Error.WriteLine("Installation refused. Set XIAOXIIME_ENVIRONMENT=Test or VirtualMachine.");
        return 3;
    }
    if (string.IsNullOrWhiteSpace(options.ImeFile))
    {
        Console.Error.WriteLine("The install command requires the full path to an .ime file.");
        return 2;
    }
    var fullPath = Path.GetFullPath(options.ImeFile);
    if (!File.Exists(fullPath) || !string.Equals(Path.GetExtension(fullPath), ".ime", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Valid IME file not found: {fullPath}");
        return 4;
    }
    var result = new WindowsImeInstaller().Install(fullPath, "XiaoXi IME");
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    return result.Succeeded ? 0 : 5;
}

static int PrintSystemTestPlan(SystemTestPlanOptions options)
{
    var plan = SystemTestPlan.CreateDefault();
    if (options.Json)
    {
        Console.WriteLine(plan.ToJson());
        return 0;
    }
    Console.WriteLine(plan.Name);
    foreach (var step in plan.Steps)
    {
        Console.WriteLine($"[{step.Id}] {step.Area}: {step.Description}");
    }
    return 0;
}

static Task<int> RunSystemTests(SystemTestRunOptions options)
{
    if (string.IsNullOrWhiteSpace(options.AbiHost) || string.IsNullOrWhiteSpace(options.TsfDll))
    {
        Console.Error.WriteLine("system-test-run requires <abi-host> and <tsf-dll>.");
        return Task.FromResult(2);
    }
    var reportPath = options.Report ?? Path.Combine(Environment.CurrentDirectory, "artifacts", "system-tests", "report.json");
    SystemTestCommand[] commands =
    [
        new("tsf-abi", Path.GetFullPath(options.AbiHost), ["abi", Path.GetFullPath(options.TsfDll)]),
        new("tsf-com-activation", Path.GetFullPath(options.AbiHost), ["com-activation", Path.GetFullPath(options.TsfDll)]),
    ];
    return SystemTestRunner.RunAsync(commands, reportPath, string.Equals(options.Confirm, SystemTestRunner.VmConfirmation, StringComparison.Ordinal), Console.Out, Console.Error);
}