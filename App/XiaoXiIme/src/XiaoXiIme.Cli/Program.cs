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

static int Install(InstallOptions options) => InstallationLog.Run("install", log => InstallCore(options, log));

static int InstallCore(InstallOptions options, InstallationLog log)
{
    log.Write("[Install][LocateFiles] Checking distribution files");
    var distribution = Distribution.Open(options.Payload);
    if (distribution.MissingInstallFile() is { } missing)
    {
        log.Write($"[Install][LocateFiles] MissingFile={missing}");
        Console.Error.WriteLine($"[Install][LocateFiles] MissingFile={missing}");
        return 4;
    }

    log.Write("[Install][StopUi] Waiting for any existing UI instance to exit");
    log.EnsureWritten();
    if (!ProductUiShutdown.Stop(Console.Error, log.Write))
    {
        return 7;
    }

    log.Write("[Install][Register] Deploying IME files and registering keyboard layout");
    log.EnsureWritten();
    var x64ImePath = distribution.Ime("win-x64");
    var x86ImePath = distribution.Ime("win-x86");
    var result = new WindowsImeInstaller().InstallPair(x64ImePath, x86ImePath, "XiaoXi IME");
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    log.Write(result.Message);
    if (!result.Succeeded)
    {
        return 6;
    }

    log.Write("[Install][StartUi] Launching the formal UI and waiting for the IPC readiness response");
    log.EnsureWritten();
    var hostStart = ImeHostProcessManager.StartDetached
        (distribution.Launcher, startupLog: log.Write);
    log.Write($"[Install][StartUi] PID={hostStart.ProcessId}; ChildExitCode={hostStart.ChildExitCode}; {hostStart.Message}");
    (hostStart.Succeeded ? Console.Out : Console.Error).WriteLine(hostStart.Message);
    if (!hostStart.Succeeded)
    {
        Console.Error.Write(hostStart.StandardError);
        Console.Error.Write(hostStart.StandardOutput);
    }
    return hostStart.Succeeded ? 0 : 7;
}

static int Uninstall(UninstallOptions options) => InstallationLog.Run("uninstall", log => UninstallCore(options, log));

static int UninstallCore(UninstallOptions options, InstallationLog log)
{
    log.Write("[Uninstall][StopUi] Waiting for any existing UI instance to exit");
    log.EnsureWritten();
    if (!ProductUiShutdown.Stop(Console.Error, log.Write))
    {
        return 7;
    }
    log.Write("[Uninstall][Remove] Removing keyboard layouts and deployed IME files");
    log.EnsureWritten();
    var result = new WindowsImeInstaller().UninstallExisting("XiaoXi IME", "XiaoXiIme.ime");
    (result.Succeeded ? Console.Out : Console.Error).WriteLine(result.Message);
    log.Write(result.Message);
    if (result.Succeeded)
    {
        var userDataResult = UserDictionaryUninstall.Complete(options.PurgeUserData);
        log.Write(userDataResult);
        Console.Out.WriteLine(userDataResult);
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