using XiaoXiIme.Cli;

namespace XiaoXiIme.Cli.Tests;

public class SystemTestCommandTests
{
    [Fact]
    public void SystemTestPlanJsonContainsAllMajorAreas()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = CliApplication.Run(["system-test-plan", "--json"], output, error, static () => throw new NotSupportedException());

        Assert.Equal(0, exitCode);
        Assert.Contains("Traditional IME", output.ToString());
        Assert.Contains("TSF", output.ToString());
        Assert.Contains("Host/IPC", output.ToString());
        Assert.Contains("End-to-end", output.ToString());
        Assert.Contains("Rollback", output.ToString());
    }

    [Fact]
    public async Task RunnerRejectsExecutionWithoutVmConfirmation()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await SystemTestRunner.RunAsync([], "unused.json", false, output, error);

        Assert.Equal(6, exitCode);
        Assert.Contains(SystemTestRunner.VmConfirmation, error.ToString());
        Assert.False(File.Exists("unused.json"));
    }
}