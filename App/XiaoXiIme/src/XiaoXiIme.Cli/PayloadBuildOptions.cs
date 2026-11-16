using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("payload-build", Description = "Build a self-contained integration-test payload directory.")]
internal sealed class PayloadBuildOptions
{
    [Option("experience", Description = "Build a standalone zero-configuration experience with one win-x64 UI runtime.")]
    public bool Experience { get; init; }

    [Option("output", Description = "Payload output directory.", ValueName = "directory")]
    public string? Output { get; init; }

    [Option("no-build", Description = "Collect existing publish outputs without invoking dotnet build/publish.")]
    public bool NoBuild { get; init; }

    [Option("dictionary-source", Description = "XiaoXiIme native TSV directory used to compile payload dictionary packages.", ValueName = "directory")]
    public string? DictionarySource { get; init; }

    [Option("staging-directory", Description = "Existing publish staging directory used by --no-build tests and local payload assembly.", ValueName = "directory")]
    public string? StagingDirectory { get; init; }

}