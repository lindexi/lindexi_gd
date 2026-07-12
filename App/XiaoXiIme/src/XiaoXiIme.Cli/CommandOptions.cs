using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("install", Description = "Install the IME by calling the Windows ImmInstallIME API.")]
internal sealed class InstallOptions
{
    [Value(0, Description = "Full path to the .ime file.")]
    public string? ImeFile { get; init; }

    [Option("allow-system-changes", Description = "Confirm that this command may modify Windows.")]
    public bool AllowSystemChanges { get; init; }
}

[Command("install-checklist", Description = "Print the manual Windows IME installation checklist.")]
internal sealed class InstallChecklistOptions
{
    [Value(0, Description = "Path to the .ime file.")]
    public string? ImeFile { get; init; }
}

[Command("uninstall-checklist", Description = "Print the manual Windows IME uninstall and rollback checklist.")]
internal sealed class UninstallChecklistOptions;

[Command("publish-checklist", Description = "Print Native AOT publish verification steps.")]
internal sealed class PublishChecklistOptions;

[Command("export-checklist", Description = "Print export verification commands for an IME binary.")]
internal sealed class ExportChecklistOptions
{
    [Value(0, Description = "Path to the IME binary.")]
    public string? ImeFile { get; init; }
}

[Command("system-test-plan", Description = "Print the global Windows/VM system validation plan.")]
internal sealed class SystemTestPlanOptions
{
    [Option("json", Description = "Write the plan as JSON.")]
    public bool Json { get; init; }
}

[Command("system-test-run", Description = "Run Windows/VM system validation commands.")]
internal sealed class SystemTestRunOptions
{
    [Value(0, Description = "Path to the ABI test host.")]
    public string? AbiHost { get; init; }

    [Value(1, Description = "Path to the TSF DLL.")]
    public string? TsfDll { get; init; }

    [Option("confirm", Description = "Disposable-VM confirmation token.", ValueName = "token")]
    public string? Confirm { get; init; }

    [Option("report", Description = "Path to the generated report.", ValueName = "file")]
    public string? Report { get; init; }
}

[Command("payload-build", Description = "Build a self-contained integration-test payload directory.")]
internal sealed class PayloadBuildOptions
{
    [Option("output", Description = "Payload output directory.", ValueName = "directory")]
    public string? Output { get; init; }

    [Option("runtime", Description = "Windows runtime identifier.", ValueName = "rid")]
    public string RuntimeIdentifier { get; init; } = "win-x64";

    [Option("no-build", Description = "Collect existing publish outputs without invoking dotnet build/publish.")]
    public bool NoBuild { get; init; }
}

[Command("integration-run", Description = "Run the destructive VM integration-test lifecycle from a payload manifest.")]
internal sealed class IntegrationRunOptions
{
    [Value(0, Description = "Payload directory or payload manifest path.")]
    public string? Payload { get; init; }

    [Option("confirm", Description = "Disposable-VM confirmation token.", ValueName = "token")]
    public string? Confirm { get; init; }

    [Option("report", Description = "Path to the generated report.", ValueName = "file")]
    public string? Report { get; init; }

    [Option("keep-installed", Description = "Do not uninstall XiaoXiIme after validation.")]
    public bool KeepInstalled { get; init; }
}