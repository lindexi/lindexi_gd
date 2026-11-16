using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("export-checklist", Description = "Print export verification commands for an IME binary.")]
internal sealed class ExportChecklistOptions
{
    [Value(0, Description = "Path to the IME binary.")]
    public string? ImeFile { get; init; }
}