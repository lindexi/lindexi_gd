using DotNetCampus.Cli;
using XiaoXiIme.Cli;

if (args.Length == 0)
{
    args = ["--help"];
}

return await CommandLine.Parse(args)
    .AddHelpHandler()
    .AddHandler<InstallOptions>(Install)
    .AddHandler<UninstallOptions>(Uninstall)
    .AddHandler<SystemTestPlanOptions>(PrintSystemTestPlan)
    .AddHandler<SystemTestRunOptions>(RunSystemTests)
    .AddHandler<PayloadBuildOptions>(options => IntegrationPayloadBuilder.BuildAsync(options, Console.Out, Console.Error))
    .AddHandler<IntegrationRunOptions>(options => IntegrationTestRunner.RunAsync(options, Console.Out, Console.Error, static () => new WindowsImeInstaller()))
    .AddHandler<NativeImeLoadProbeOptions>(options => NativeImeLoadProbe.Run(options, Console.Out, Console.Error))
    .RunAsync();

static int Install(InstallOptions options)
{
    if (!string.Equals(options.Confirm, SystemTestRunner.VmConfirmation, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"Installation refused. Pass --confirm {SystemTestRunner.VmConfirmation} only inside a disposable VM.");
        return 3;
    }

    var manifestPath = IntegrationTestRunner.ResolveManifestPath(options.Payload, AppContext.BaseDirectory, Environment.CurrentDirectory);
    if (manifestPath is null)
    {
        Console.Error.WriteLine("Payload manifest was not found.");
        return 4;
    }

    var root = Path.GetDirectoryName(manifestPath)!;
    var manifest = IntegrationPayloadManifest.Load(manifestPath);
    var verificationError = IntegrationTestRunner.VerifyPayload(root, manifest);
    if (verificationError is not null)
    {
        Console.Error.WriteLine(verificationError);
        return 5;
    }
    if (!manifest.NativeComponents.TryGetValue("x64", out var x64Components)
        || !manifest.NativeComponents.TryGetValue("x86", out var x86Components))
    {
        Console.Error.WriteLine("The payload does not contain both x64 and x86 IME components.");
        return 5;
    }

    var x64ImePath = Path.GetFullPath(Path.Combine(root, x64Components.ImeFile.Replace('/', Path.DirectorySeparatorChar)));
    var x86ImePath = Path.GetFullPath(Path.Combine(root, x86Components.ImeFile.Replace('/', Path.DirectorySeparatorChar)));
    var result = new WindowsImeInstaller().InstallPair(x64ImePath, x86ImePath, "XiaoXi IME");
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    return result.Succeeded ? 0 : 6;
}

static int Uninstall(UninstallOptions options)
{
    if (!string.Equals(options.Confirm, SystemTestRunner.VmConfirmation, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"Uninstallation refused. Pass --confirm {SystemTestRunner.VmConfirmation} only inside a disposable VM.");
        return 3;
    }

    var result = new WindowsImeInstaller().UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    return result.Succeeded ? 0 : 6;
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