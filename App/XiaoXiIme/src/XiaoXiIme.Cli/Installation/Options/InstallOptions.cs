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