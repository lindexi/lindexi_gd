using System.Reflection;
using System.Text;
using DotNetCampus.Cli;
using DotNetCampus.Cli.Compiler;
using DotNetCampus.Cli.Exceptions;

namespace XiaoXiIme.Cli;

internal static class CliApplication
{
    internal static int Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        Func<IImeInstaller> createInstaller)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"])
        {
            args = ["help"];
        }

        try
        {
            return CommandLine.Parse(args)
                .AddHandler<InstallOptions>(options => Install(options.ImeFile, output, error, createInstaller))
                .AddHandler<InstallChecklistOptions>(options =>
                {
                    PrintInstallChecklist(options.ImeFile, output);
                    return 0;
                })
                .AddHandler<UninstallChecklistOptions>(_ =>
                {
                    PrintUninstallChecklist(output);
                    return 0;
                })
                .AddHandler<PublishChecklistOptions>(_ =>
                {
                    PrintPublishChecklist(output);
                    return 0;
                })
                .AddHandler<ExportChecklistOptions>(options =>
                {
                    PrintExportChecklist(options.ImeFile, output);
                    return 0;
                })
                .AddHandler<SystemTestPlanOptions>(options => PrintSystemTestPlan(options.Json, output, error))
                .AddHandler<SystemTestRunOptions>(options => RunSystemTests(options, output, error))
                .AddHandler<HelpOptions>(_ =>
                {
                    PrintHelp(output);
                    return 0;
                })
                .RunAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch (CommandNameNotFoundException)
        {
            error.WriteLine($"Unknown command: {args[0]}");
            PrintHelp(error);
            return 1;
        }
    }

    private static int Install(
        string? imeFile,
        TextWriter output,
        TextWriter error,
        Func<IImeInstaller> createInstaller)
    {
        if (string.IsNullOrWhiteSpace(imeFile))
        {
            error.WriteLine("The install command requires the full path to an .ime file.");
            return 2;
        }

        var fullPath = Path.GetFullPath(imeFile);
        if (!File.Exists(fullPath))
        {
            error.WriteLine($"IME file not found: {fullPath}");
            return 4;
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".ime", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine($"Installation refused. The target file must use the .ime extension: {fullPath}");
            return 4;
        }

        var result = createInstaller().Install(fullPath, "XiaoXi IME");
        if (!result.Succeeded)
        {
            error.WriteLine(result.Message);
            return 5;
        }

        output.WriteLine(result.Message);
        output.WriteLine("Record the returned HKL/layout id, then sign out or restart Windows before the smoke test if the input method is not visible.");
        return 0;
    }

    private static void PrintHelp(TextWriter output)
    {
        output.WriteLine("XiaoXiIme.Cli");
        output.WriteLine();
        output.WriteLine("Commands:");
        output.WriteLine("  publish-checklist                 Print Native AOT publish verification steps.");
        output.WriteLine("  export-checklist [ime-file]        Print export verification commands for an IME binary.");
        output.WriteLine("  install <ime-file>                  Install the IME by calling the Windows ImmInstallIME API.");
        output.WriteLine("  install-checklist [ime-file]       Print manual Windows IME installation checklist.");
        output.WriteLine("  uninstall-checklist                Print manual Windows IME uninstall and rollback checklist.");
        output.WriteLine("  system-test-plan [--json]          Print the global Windows/VM system validation plan.");
        output.WriteLine("  system-test-run <abi-host> <tsf-dll> --confirm <token> [--report <file>]");
    }

    private static int PrintSystemTestPlan(bool json, TextWriter output, TextWriter error)
    {
        var plan = SystemTestPlan.CreateDefault();
        if (json)
        {
            output.WriteLine(plan.ToJson());
            return 0;
        }

        output.WriteLine(plan.Name);
        foreach (var step in plan.Steps)
        {
            output.WriteLine($"[{step.Id}] {step.Area}: {step.Description}");
            output.WriteLine($"  Destructive: {step.Destructive}; Evidence: {step.Evidence}");
        }

        error.WriteLine("Execution is separate and requires an explicit disposable-VM confirmation token.");
        return 0;
    }

    private static Task<int> RunSystemTests(SystemTestRunOptions options, TextWriter output, TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(options.AbiHost) || string.IsNullOrWhiteSpace(options.TsfDll))
        {
            error.WriteLine("system-test-run requires <abi-host> and <tsf-dll>.");
            return Task.FromResult(2);
        }

        var reportPath = options.Report
            ?? Path.Combine(Environment.CurrentDirectory, "artifacts", "system-tests", "report.json");
        var allowDestructive = string.Equals(options.Confirm, SystemTestRunner.VmConfirmation, StringComparison.Ordinal);
        var host = Path.GetFullPath(options.AbiHost);
        var tsfDll = Path.GetFullPath(options.TsfDll);
        if (!File.Exists(host) || !File.Exists(tsfDll))
        {
            error.WriteLine("The ABI host and TSF DLL must both exist.");
            return Task.FromResult(4);
        }

        SystemTestCommand[] commands =
        [
            new("tsf-abi", host, ["abi", tsfDll]),
            new("tsf-com-activation", host, ["com-activation", tsfDll]),
        ];
        return SystemTestRunner.RunAsync(commands, reportPath, allowDestructive, output, error);
    }

    private static void PrintPublishChecklist(TextWriter output)
    {
        output.WriteLine("Native AOT publish checklist");
        output.WriteLine("1. Run: dotnet publish src\\XiaoXiIme.ImeModule\\XiaoXiIme.ImeModule.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true");
        output.WriteLine("2. Verify publish output exists under src\\XiaoXiIme.ImeModule\\bin\\Release\\net10.0\\win-x64\\publish\\.");
        output.WriteLine("3. Verify the native shared library exports traditional IME entry points: ImeInquire, ImeProcessKey, ImeToAsciiEx, ImeSelect, NotifyIME.");
        output.WriteLine("4. Copy or rename the published native library to the planned .ime file name only after export verification.");
        output.WriteLine("5. Record all Native AOT warnings; known dotnetCampus.Ipc package warnings must not be confused with project-side reflection paths.");
    }

    private static void PrintExportChecklist(string? imeFile, TextWriter output)
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

        output.Write(commands.ToString());
    }

    private static void PrintInstallChecklist(string? imeFile, TextWriter output)
    {
        imeFile ??= "<published XiaoXiIme .ime path>";
        output.WriteLine("Windows IME install checklist");
        output.WriteLine($"1. Confirm the IME file exists in a stable location: {imeFile}");
        output.WriteLine($"2. Run from an elevated process: install \"{imeFile}\"");
        output.WriteLine("3. Record the returned HKL / layout id.");
        output.WriteLine("4. Verify HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> contains the IME metadata.");
        output.WriteLine("5. Sign out/sign in or restart Windows if the layout does not appear immediately.");
        output.WriteLine("6. Switch to XiaoXi IME and run the manual smoke test.");
    }

    private static void PrintUninstallChecklist(TextWriter output)
    {
        output.WriteLine("Manual Windows IME uninstall and rollback checklist");
        output.WriteLine("Administrative privileges are required. This command does not modify the system.");
        output.WriteLine("1. Switch away from XiaoXi IME in all user sessions.");
        output.WriteLine("2. Unload the recorded HKL with UnloadKeyboardLayout where possible.");
        output.WriteLine("3. Remove current-user Keyboard Layout\\Preload entries that reference the XiaoXi layout id.");
        output.WriteLine("4. Remove HKEY_USERS\\.DEFAULT\\Keyboard Layout\\Preload entries that reference the XiaoXi layout id if they were added.");
        output.WriteLine("5. Remove Control Panel\\International\\User Profile entries that reference the layout id if present.");
        output.WriteLine("6. Remove HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\<layout id> after backing it up.");
        output.WriteLine("7. Delete the installed .ime file only after the layout is unloaded and no process locks it.");
        output.WriteLine("8. Restart affected applications or Windows if the layout remains visible.");
        output.WriteLine($"9. Keep this CLI version for audit: {Assembly.GetExecutingAssembly().GetName().Version}");
    }
}

[Command("install")]
internal sealed class InstallOptions
{
    [Value(0)]
    public string? ImeFile { get; init; }
}

[Command("install-checklist")]
internal sealed class InstallChecklistOptions
{
    [Value(0)]
    public string? ImeFile { get; init; }
}

[Command("uninstall-checklist")]
internal sealed class UninstallChecklistOptions;

[Command("publish-checklist")]
internal sealed class PublishChecklistOptions;

[Command("export-checklist")]
internal sealed class ExportChecklistOptions
{
    [Value(0)]
    public string? ImeFile { get; init; }
}

[Command("help")]
internal sealed class HelpOptions;

[Command("system-test-plan")]
internal sealed class SystemTestPlanOptions
{
    [Option("json")]
    public bool Json { get; init; }
}

[Command("system-test-run")]
internal sealed class SystemTestRunOptions
{
    [Value(0)]
    public string? AbiHost { get; init; }

    [Value(1)]
    public string? TsfDll { get; init; }

    [Option("confirm")]
    public string? Confirm { get; init; }

    [Option("report")]
    public string? Report { get; init; }
}
