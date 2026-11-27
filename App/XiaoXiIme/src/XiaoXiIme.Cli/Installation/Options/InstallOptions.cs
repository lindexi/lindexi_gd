using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("install", Description = "Install the x64/x86 IME pair from a payload and keep it installed.")]
internal sealed class InstallOptions
{
    [Value(0, Description = "Distribution directory.")]
    public string? Payload { get; init; }
}