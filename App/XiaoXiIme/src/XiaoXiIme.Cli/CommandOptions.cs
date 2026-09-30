using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("install", Description = "Install the x64/x86 IME pair from a payload and keep it installed.")]
internal sealed class InstallOptions
{
    [Value(0, Description = "Payload directory or payload manifest path.")]
    public string? Payload { get; init; }

    [Option("confirm", Description = "Disposable-VM confirmation token.", ValueName = "token")]
    public string? Confirm { get; init; }
}

[Command("uninstall", Description = "Uninstall XiaoXiIme and remove its deployed files.")]
internal sealed class UninstallOptions
{
    [Option("confirm", Description = "Disposable-VM confirmation token.", ValueName = "token")]
    public string? Confirm { get; init; }

    [Option("purge-user-data", Description = "Also delete the per-user learned dictionary. User data is retained by default.")]
    public bool PurgeUserData { get; init; }
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

    [Option("no-build", Description = "Collect existing publish outputs without invoking dotnet build/publish.")]
    public bool NoBuild { get; init; }

    [Option("dictionary-source", Description = "XiaoXiIme native TSV directory used to compile payload dictionary packages.", ValueName = "directory")]
    public string? DictionarySource { get; init; }

    [Option("staging-directory", Description = "Existing publish staging directory used by --no-build tests and local payload assembly.", ValueName = "directory")]
    public string? StagingDirectory { get; init; }

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


    [Option("skip-tsf", Description = "Skip TSF ABI and COM activation validation.")]
    public bool SkipTsf { get; init; }
}

[Command("native-ime-load-probe", Description = "Internal isolated native IME loader probe.")]
internal sealed class NativeImeLoadProbeOptions
{
    [Value(0, Description = "Full path to the IME binary.")]
    public string? ImeFile { get; init; }
}

[Command("dictionary-update", Description = "Compile local XiaoXiIme TSV sources and update a dictionary package.")]
internal sealed class DictionaryUpdateOptions
{
    [Value(0, Description = "Directory containing *.phonetic.tsv and optional *.shape.tsv/*.symbols.tsv files.")]
    public string? SourceDirectory { get; init; }

    [Value(1, Description = "Target dictionary package directory.")]
    public string? PackageDirectory { get; init; }

    [Option("scheme", Description = "Input scheme: fullPinyin or xiaoheDoublePinyin.", ValueName = "name")]
    public string Scheme { get; init; } = "fullPinyin";
}

[Command("dictionary-rollback", Description = "Exchange a dictionary package with its retained previous version.")]
internal sealed class DictionaryRollbackOptions
{
    [Value(0, Description = "Target dictionary package directory.")]
    public string? PackageDirectory { get; init; }
}

[Command("dictionary-convert-sewzc", Description = "One-time conversion of a SeWZC dictionary snapshot into XiaoXiIme native TSV sources.")]
internal sealed class DictionaryConvertSeWzcOptions
{
    [Value(0, Description = "Copied SeWZC data/dictionaries snapshot directory.")]
    public string? SourceDirectory { get; init; }

    [Value(1, Description = "Target XiaoXiIme data/dictionaries directory.")]
    public string? TargetDirectory { get; init; }
}

[Command("dictionary-inspect", Description = "Validate a dictionary package and report its version, path, entry counts, and SeWZC attribution.")]
internal sealed class DictionaryInspectOptions
{
    [Value(0, Description = "Dictionary package directory.")]
    public string? PackageDirectory { get; init; }

    [Option("json", Description = "Write the inspection report as JSON.")]
    public bool Json { get; init; }
}

[Command("dictionary-build-packages", Description = "Compile full Pinyin and Xiaohe packages from native TSV sources without Native AOT publish.")]
internal sealed class DictionaryBuildPackagesOptions
{
    [Value(0, Description = "XiaoXiIme native TSV source directory.")]
    public string? SourceDirectory { get; init; }

    [Value(1, Description = "Host output directory that receives both compiled packages.")]
    public string? HostOutputDirectory { get; init; }
}