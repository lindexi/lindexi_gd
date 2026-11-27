using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("install-checklist", Description = "Print the manual Windows IME installation checklist.")]
internal sealed class InstallChecklistOptions
{
    [Value(0, Description = "Path to the .ime file.")]
    public string? ImeFile { get; init; }
}