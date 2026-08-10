using System.Text.RegularExpressions;

using AgentLib.Coding.Sandboxes;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

/// <summary>
/// <see cref="WindowsSandboxTools"/> 的单元测试。
/// </summary>
[TestClass]
public sealed class WindowsSandboxToolsTests
{
    [TestMethod(DisplayName = "沙盒工具集合只应暴露高层执行工具")]
    public void AsAITools_WhenSandboxIsConfigured_ContainsOnlyExecutionTool()
    {
        string workspacePath = CreateTestDirectory();
        var tools = new WindowsSandboxTools(
            workspacePath,
            new RecordingWinRemoteShellRunner());

        AIFunction tool = tools.AsAITools().OfType<AIFunction>().Single();

        Assert.AreEqual("execute_in_windows_sandbox", tool.Name);
    }

    [TestMethod(DisplayName = "执行目录位于工作区外时不应调用远程服务")]
    public async Task ExecuteAsync_WhenSourceIsOutsideWorkspace_ReturnsValidationError()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Combine(testRoot, "workspace");
        string outsidePath = Path.Combine(testRoot, "outside");
        Directory.CreateDirectory(workspacePath);
        Directory.CreateDirectory(outsidePath);
        var runner = new RecordingWinRemoteShellRunner();
        var tools = CreateTools(workspacePath, runner);

        string result = await tools.ExecuteAsync(outsidePath, "runner.exe");

        StringAssert.Contains(result, "执行目录必须位于代码工作区内");
        Assert.AreEqual(0, runner.CallCount);
    }

    [TestMethod(DisplayName = "命令参数包含 cmd 环境变量字符时不应调用远程服务")]
    public async Task ExecuteAsync_WhenArgumentContainsExpansionCharacter_ReturnsValidationError()
    {
        string workspacePath = CreateTestDirectory();
        Directory.CreateDirectory(Path.Combine(workspacePath, "runner"));
        var runner = new RecordingWinRemoteShellRunner();
        var tools = CreateTools(workspacePath, runner);

        string result = await tools.ExecuteAsync("runner", "runner.exe", arguments: ["%TEMP%"]);

        StringAssert.Contains(result, "不能包含换行符、百分号、感叹号或双引号");
        Assert.AreEqual(0, runner.CallCount);
    }

    [TestMethod(DisplayName = "沙盒执行应依次推送执行并拉取整个任务目录")]
    public async Task ExecuteAsync_WhenExecutionSucceeds_PushesExecutesAndPullsTaskDirectory()
    {
        string workspacePath = CreateTestDirectory();
        string sourcePath = Path.Combine(workspacePath, "runner");
        Directory.CreateDirectory(sourcePath);
        var runner = new RecordingWinRemoteShellRunner(exitCode: 0);
        var tools = CreateTools(workspacePath, runner);

        string result = await tools.ExecuteAsync("runner", "bin\\TestRunner.exe", arguments: ["--test", "sample data"]);

        CollectionAssert.AreEqual(new[] { "push", "exec", "pull" }, runner.Calls);
        StringAssert.StartsWith(runner.Command!, "cmd.exe /D /V:ON /S /C");
        StringAssert.Contains(runner.Command!, "bin\\TestRunner.exe");
        StringAssert.Contains(runner.Command!, "\"sample data\"");
        StringAssert.Contains(result, "状态：执行成功");
    }

    [TestMethod(DisplayName = "远端执行返回非零退出码时仍应拉取结果")]
    public async Task ExecuteAsync_WhenExecutableFails_PullsResultsAndReturnsExitCode()
    {
        string workspacePath = CreateTestDirectory();
        Directory.CreateDirectory(Path.Combine(workspacePath, "runner"));
        var runner = new RecordingWinRemoteShellRunner(exitCode: 7);
        var tools = CreateTools(workspacePath, runner);

        string result = await tools.ExecuteAsync("runner", "TestRunner.exe", outputRelativePath: "results");

        Assert.AreEqual("pull", runner.Calls.Last());
        StringAssert.EndsWith(runner.RemotePullPath, "\\results");
        StringAssert.Contains(result, "状态：执行失败");
        StringAssert.Contains(result, "退出码：7");
    }

    private static WindowsSandboxTools CreateTools(string workspacePath, IWinRemoteShellRunner runner) =>
        new(workspacePath, runner);

    private static string CreateTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "AgentLib.Coding.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class RecordingWinRemoteShellRunner(int exitCode = 0) : IWinRemoteShellRunner
    {
        private static readonly Regex MarkerRegex = new(
            @"(__CODING_AGENT_EXIT_CODE_[0-9a-f]{32}=)",
            RegexOptions.CultureInvariant);

        public List<string> Calls { get; } = [];

        public int CallCount => Calls.Count;

        public string? Command { get; private set; }

        public string? RemotePullPath { get; private set; }

        public Task PushAsync(string sourcePath, string remoteTargetPath, CancellationToken cancellationToken)
        {
            Calls.Add("push");
            return Task.CompletedTask;
        }

        public Task<string> ExecuteAsync(string command, int timeoutSeconds, CancellationToken cancellationToken)
        {
            Calls.Add("exec");
            Command = command;
            Match marker = MarkerRegex.Match(command);
            Assert.IsTrue(marker.Success);
            return Task.FromResult($"runner output{Environment.NewLine}{marker.Groups[1].Value}{exitCode}");
        }

        public Task PullAsync(string remoteSourcePath, string localOutputPath, CancellationToken cancellationToken)
        {
            Calls.Add("pull");
            RemotePullPath = remoteSourcePath;
            return Task.CompletedTask;
        }
    }
}
