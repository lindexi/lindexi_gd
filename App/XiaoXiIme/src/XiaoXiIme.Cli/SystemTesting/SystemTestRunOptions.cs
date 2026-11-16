using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

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