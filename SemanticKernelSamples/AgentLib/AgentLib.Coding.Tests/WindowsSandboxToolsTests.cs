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
        var tools = new WindowsSandboxTools(workspacePath, new RecordingWinRemoteShellRunner());

        AIFunction tool = tools.AsAITools().OfType<AIFunction>().Single();

        Assert.AreEqual("execute_in_windows_sandbox", tool.Name);
    }

    [TestMethod(DisplayName = "执行目录位于工作区外时应拒绝执行")]
    public async Task ExecuteAsync_WhenSourceIsOutsideWorkspace_ThrowsArgumentException()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Combine(testRoot, "workspace");
        string outsidePath = Path.Combine(testRoot, "outside");
        Directory.CreateDirectory(workspacePath);
        Directory.CreateDirectory(outsidePath);
        var runner = new RecordingWinRemoteShellRunner();
        var tools = new WindowsSandboxTools(workspacePath, runner);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => tools.ExecuteAsync(outsidePath, "runner.exe"));

        Assert.AreEqual(0, runner.Calls.Count);
    }

    [TestMethod(DisplayName = "沙盒执行应推送、逐项执行并拉取任务目录")]
    public async Task ExecuteAsync_WhenExecutionSucceeds_PushesExecutesAndPullsTaskDirectory()
    {
        string workspacePath = CreateTestDirectory();
        string sourcePath = Path.Combine(workspacePath, "runner");
        Directory.CreateDirectory(sourcePath);
        var runner = new RecordingWinRemoteShellRunner("runner output");
        var tools = new WindowsSandboxTools(workspacePath, runner);

        string result = await tools.ExecuteAsync(
            "runner",
            "bin\\TestRunner.exe",
            arguments: ["--test", "sample data"]);

        CollectionAssert.AreEqual(new[] { "push", "exec", "pull" }, runner.Calls);
        Assert.AreEqual("cmd.exe", runner.ExecutablePath);
        CollectionAssert.AreEqual(new[] { "/D", "/C" }, runner.Arguments!.Take(2).ToArray());
        StringAssert.Contains(runner.Arguments[2], "bin\\TestRunner.exe");
        StringAssert.Contains(runner.Arguments[2], "sample data");
        StringAssert.Contains(result, "runner output");
    }

    [TestMethod(DisplayName = "沙盒推送应使用独立远端任务目录")]
    public async Task ExecuteAsync_WhenExecutionStarts_UsesDedicatedRemoteTaskDirectory()
    {
        string workspacePath = CreateTestDirectory();
        Directory.CreateDirectory(Path.Combine(workspacePath, "runner"));
        var runner = new RecordingWinRemoteShellRunner();
        var tools = new WindowsSandboxTools(workspacePath, runner);

        await tools.ExecuteAsync("runner", "TestRunner.exe", outputRelativePath: "results");

        StringAssert.StartsWith(runner.RemotePushPath, @"C:\CodingAgentSandbox\Tasks\");
        StringAssert.EndsWith(runner.RemotePullPath, "\\results");
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "AgentLib.Coding.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class RecordingWinRemoteShellRunner(string output = "") : IWinRemoteShellRunner
    {
        public List<string> Calls { get; } = [];

        public string? ExecutablePath { get; private set; }

        public IReadOnlyList<string>? Arguments { get; private set; }

        public string? RemotePushPath { get; private set; }

        public string? RemotePullPath { get; private set; }

        public Task PushAsync(string sourcePath, string remoteTargetPath, CancellationToken cancellationToken)
        {
            Calls.Add("push");
            RemotePushPath = remoteTargetPath;
            return Task.CompletedTask;
        }

        public Task<string> ExecuteAsync(
            string executablePath,
            IReadOnlyList<string> arguments,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            Calls.Add("exec");
            ExecutablePath = executablePath;
            Arguments = arguments;
            return Task.FromResult(output);
        }

        public Task PullAsync(string remoteSourcePath, string localOutputPath, CancellationToken cancellationToken)
        {
            Calls.Add("pull");
            RemotePullPath = remoteSourcePath;
            return Task.CompletedTask;
        }
    }
}
