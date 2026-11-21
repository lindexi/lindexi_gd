using DotNetCampus.Cli;
using XiaoXiIme.Cli;
using XiaoXiIme.ImeIpc;

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
    .AddHandler<IntegrationRunOptions>
    (options => IntegrationTestRunner.RunAsync
        (options, Console.Out, Console.Error, static () => new WindowsImeInstaller())
    )
    .AddHandler<NativeImeLoadProbeOptions>(options => NativeImeLoadProbe.Run(options, Console.Out, Console.Error))
    .AddHandler<DictionaryUpdateOptions>(options => LocalDictionaryCommands.Update(options, Console.Out, Console.Error))
    .AddHandler<DictionaryRollbackOptions>
        (options => LocalDictionaryCommands.Rollback(options, Console.Out, Console.Error))
    .AddHandler<DictionaryConvertSeWzcOptions>
        (options => LocalDictionaryCommands.ConvertSeWzc(options, Console.Out, Console.Error))
    .AddHandler<DictionaryBuildPackagesOptions>
        (options => LocalDictionaryCommands.BuildPackages(options, Console.Out, Console.Error))
    .AddHandler<DictionaryInspectOptions>
        (options => LocalDictionaryCommands.Inspect(options, Console.Out, Console.Error))
    .RunAsync();

static int Install(InstallOptions options)
{
    var distribution = Distribution.Open(options.Payload);
    if (distribution.MissingInstallFile() is { } missing)
    {
        Console.Error.WriteLine($"[Install][LocateFiles] MissingFile={missing}");
        return 4;
    }

    if (!ProductUiShutdown.Stop(Console.Error))
    {
        return 7;
    }

    var x64ImePath = distribution.Ime("win-x64");
    var x86ImePath = distribution.Ime("win-x86");
    var result = new WindowsImeInstaller().InstallPair(x64ImePath, x86ImePath, "XiaoXi IME");
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    if (!result.Succeeded)
    {
        return 6;
    }

    var hostStart = ImeHostProcessManager.StartDetached
        (distribution.Launcher);
    (hostStart.Succeeded ? Console.Out : Console.Error).WriteLine(hostStart.Message);
    return hostStart.Succeeded ? 0 : 7;
}

static int Uninstall(UninstallOptions options)
{
    if (!ProductUiShutdown.Stop(Console.Error))
    {
        return 7;
    }
    var result = new WindowsImeInstaller().UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    if (result.Succeeded)
    {
        Console.Out.WriteLine(UserDictionaryUninstall.Complete(options.PurgeUserData));
    }

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

    var reportPath = options.Report ?? Path.Combine
        (Environment.CurrentDirectory, "artifacts", "system-tests", "report.json");
    SystemTestCommand[] commands =
    [
        new("tsf-abi", Path.GetFullPath(options.AbiHost), ["abi", Path.GetFullPath(options.TsfDll)]),
        new
        (
            "tsf-com-activation", Path.GetFullPath(options.AbiHost),
            ["com-activation", Path.GetFullPath(options.TsfDll)]
        ),
    ];
    return SystemTestRunner.RunAsync
    (
        commands, reportPath, string.Equals(options.Confirm, SystemTestRunner.VmConfirmation, StringComparison.Ordinal),
        Console.Out, Console.Error
    );
}