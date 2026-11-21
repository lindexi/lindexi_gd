using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("integration-run", Description = "Uninstall, install, test, and clean up the input method from the distribution.")]
internal sealed class IntegrationRunOptions
{
    [Value(0, Description = "Distribution directory.")]
    public string? Payload { get; init; }

    [Option("report", Description = "Path to the generated report.", ValueName = "file")]
    public string? Report { get; init; }


    [Option("skip-tsf", Description = "Skip TSF ABI and COM activation validation.")]
    public bool SkipTsf { get; init; }
}