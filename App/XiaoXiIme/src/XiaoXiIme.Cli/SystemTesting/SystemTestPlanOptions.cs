using DotNetCampus.Cli.Compiler;

namespace XiaoXiIme.Cli;

[Command("system-test-plan", Description = "Print the global Windows/VM system validation plan.")]
internal sealed class SystemTestPlanOptions
{
    [Option("json", Description = "Write the plan as JSON.")]
    public bool Json { get; init; }
}