using System.Reflection;
using System.Text;
using DotNetCampus.Cli;
using XiaoXiIme.Cli;

if (args.Length == 0)
{
    args = ["--help"];
}

return await CommandLine.Parse(args)
    .AddHelpHandler()
    .AddHandler<InstallOptions>(Install)
    .AddHandler<InstallChecklistOptions>(options =>
    {
        PrintInstallChecklist(options.ImeFile);
        return 0;
    })
    .AddHandler<UninstallChecklistOptions>(_ =>
    {
        PrintUninstallChecklist();
        return 0;
    })
    .AddHandler<PublishChecklistOptions>(_ =>
    {
        PrintPublishChecklist();
        return 0;
    })
    .AddHandler<ExportChecklistOptions>(options =>
    {
        PrintExportChecklist(options.ImeFile);
        return 0;
    })
    .AddHandler<SystemTestPlanOptions>(PrintSystemTestPlan)
    .AddHandler<SystemTestRunOptions>(RunSystemTests)
    .RunAsync();

static int Install(InstallOptions options)
{
    if (string.IsNullOrWhiteSpace(options.ImeFile))
    {
        Console.Error.WriteLine("The install command requires the full path to an .ime file.");
        return 2;
    }

    var fullPath = Path.GetFullPath(options.ImeFile);
    if (!File.Exists(fullPath))
    {
        Console.Error.WriteLine($"IME file not found: {fullPath}");
        return 4;
    }

    if (!string.Equals(Path.GetExtension(fullPath), ".ime", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Installation refused. The target file must use the .ime extension: {fullPath}");
        return 4;
    }

    var result = new WindowsImeInstaller().Install(fullPath, "XiaoXi IME");
    if (!result.Succeeded)
    {
        Console.Error.WriteLine(result.Message);
        return 5;
    }

    Console.WriteLine(result.Message);
    Console.WriteLine("Record the returned HKL/layout id, then sign out or restart Windows before the smoke test if the input method is not visible.");
    return 0;
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
        Console.WriteLine($"  Destructive: {step.Destructive}; Evidence: {step.Evidence}");
    }

    Console.Error.WriteLine("Execution is separate and requires an explicit disposable-VM confirmation token.");
    return 0;
}

static Task<int> RunSystemTests(SystemTestRunOptions options)
{
    if (string.IsNullOrWhiteSpace(options.AbiHost) || string.IsNullOrWhiteSpace(options.TsfDll))
    {
        Console.Error.WriteLine("system-test-run requires <abi-host> and <tsf-dll>.");
        return Task.FromResult(2);
    }

    var reportPath = options.Report
        ?? Path.Combine(Environment.CurrentDirectory, "artifacts", "system-tests", "report.json");
    var allowDestructive = string.Equals(options.Confirm, SystemTestRunner.VmConfirmation, StringComparison.Ordinal);
    var host = Path.GetFullPath(options.AbiHost);
    var tsfDll = Path.GetFullPath(options.TsfDll);
    if (!File.Exists(host) || !File.Exists(tsfDll))
    {
        Console.Error.WriteLine("The ABI host and TSF DLL must both exist.");
        return Task.FromResult(4);
    }

    SystemTestCommand[] commands =
    [
        new("tsf-abi", host, ["abi", tsfDll]),
        new("tsf-com-activation", host, ["com-activation", tsfDll]),
    ];
    return SystemTestRunner.RunAsync(commands, reportPath, allowDestructive, Console.Out, Console.Error);
}

static void PrintPublishChecklist()
{
    Console.WriteLine("Native AOT publish checklist");
    Console.WriteLine("1. Run: dotnet publish src\\XiaoXiIme.ImeModule\\XiaoXiIme.ImeModule.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true");
    Console.WriteLine("2. Verify publish output exists under src\\XiaoXiIme.ImeModule\\bin\\Release\\net10.0\\win-x64\\publish\\.");
    Console.WriteLine("3. Verify the native shared library exports traditional IME entry points: ImeInquire, ImeProcessKey, ImeToAsciiEx, ImeSelect, NotifyIME.");
    Console.WriteLine("4. Copy or rename the published native library to the planned .ime file name only after export verification.");
    Console.WriteLine("5. Record all Native AOT warnings; known dotnetCampus.Ipc package warnings must not be confused with project-side reflection paths.");
}

static void PrintExportChecklist(string? imeFile)
{
    imeFile ??= "src\\XiaoXiIme.ImeModule\\bin\\Release\\net10.0\\win-x64\\publish\\XiaoXiIme.ImeModule.dll";
    var commands = new StringBuilder()
        .AppendLine("Export verification checklist")
        .AppendLine("This command does not inspect or modify the system. Run one of the following checks manually before registration.")
        .AppendLine($"Target binary: {imeFile}")
        .AppendLine("1. Visual Studio Developer Command Prompt:")
        .AppendLine($"   dumpbin /exports \"{imeFile}\"")
        .AppendLine("2. PowerShell with Visual Studio tools on PATH:")
        .AppendLine($"   dumpbin /exports \"{imeFile}\" | Select-String \"ImeInquire|ImeProcessKey|ImeToAsciiEx|ImeSelect|NotifyIME\"")
        .AppendLine("3. Required export names:")
        .AppendLine("   ImeInquire, ImeProcessKey, ImeToAsciiEx, ImeSelect, NotifyIME")
        .AppendLine("4. Do not call ImmInstallIME until all required exports are present.");

    Console.Write(commands.ToString());
}

static void PrintInstallChecklist(string? imeFile)
{
    imeFile ??= "<published XiaoXiIme .ime path>";
    Console.WriteLine("Windows IME install checklist");
    Console.WriteLine($"1. Confirm the IME file exists in a stable location: {imeFile}");
    Console.WriteLine($"2. Run from an elevated process: install \"{imeFile}\"");
    Console.WriteLine("3. Record the returned HKL / layout id.");
    Console.WriteLine("4. Verify HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> contains the IME metadata.");
    Console.WriteLine("5. Sign out/sign in or restart Windows if the layout does not appear immediately.");
    Console.WriteLine("6. Switch to XiaoXi IME and run the manual smoke test.");
}

static void PrintUninstallChecklist()
{
    Console.WriteLine("Manual Windows IME uninstall and rollback checklist");
    Console.WriteLine("Administrative privileges are required. This command does not modify the system.");
    Console.WriteLine("1. Switch away from XiaoXi IME in all user sessions.");
    Console.WriteLine("2. Unload the recorded HKL with UnloadKeyboardLayout where possible.");
    Console.WriteLine("3. Remove current-user Keyboard Layout\\Preload entries that reference the XiaoXi layout id.");
    Console.WriteLine("4. Remove HKEY_USERS\\.DEFAULT\\Keyboard Layout\\Preload entries that reference the XiaoXi layout id if they were added.");
    Console.WriteLine("5. Remove Control Panel\\International\\User Profile entries that reference the layout id if present.");
    Console.WriteLine("6. Remove HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> after backing it up.");
    Console.WriteLine("7. Delete the installed .ime file only after the layout is unloaded and no process locks it.");
    Console.WriteLine("8. Restart affected applications or Windows if the layout remains visible.");
    Console.WriteLine($"9. Keep this CLI version for audit: {Assembly.GetExecutingAssembly().GetName().Version}");
}
