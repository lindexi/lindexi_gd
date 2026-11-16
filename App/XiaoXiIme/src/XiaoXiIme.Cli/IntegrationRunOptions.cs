using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

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