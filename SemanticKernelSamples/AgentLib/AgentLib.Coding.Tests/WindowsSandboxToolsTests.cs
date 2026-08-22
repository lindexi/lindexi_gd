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

    [TestMethod(DisplayName = "执行目录位于工作区外时应向 Agent 返回具体错误")]
    public async Task ExecuteAsync_WhenSourceIsOutsideWorkspace_ReturnsDetailedError()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Combine(testRoot, "workspace");
        string outsidePath = Path.Combine(testRoot, "outside");
        Directory.CreateDirectory(workspacePath);
        Directory.CreateDirectory(outsidePath);
        var runner = new RecordingWinRemoteShellRunner();
        var tools = new WindowsSandboxTools(workspacePath, runner);

        string result = await tools.ExecuteAsync(outsidePath, "runner.exe");

        StringAssert.Contains(result, "沙箱执行失败");
        StringAssert.Contains(result, "ArgumentException");
        StringAssert.Contains(result, "路径必须位于代码工作区内");
        Assert.AreEqual(0, runner.Calls.Count);
    }

    [TestMethod(DisplayName = "AITool 调用失败时应返回具体错误而不是抛出异常")]
    public async Task AITool_WhenRunnerThrows_ReturnsDetailedError()
    {
        string workspacePath = CreateTestDirectory();
        Directory.CreateDirectory(Path.Combine(workspacePath, "runner"));
        var tools = new WindowsSandboxTools(workspacePath, new ThrowingWinRemoteShellRunner());
        AIFunction tool = tools.AsAITools().OfType<AIFunction>().Single();

        object? result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["sourceDirectory"] = "runner",
            ["executableRelativePath"] = "runner.exe",
        });

        string resultText = result?.ToString() ?? string.Empty;
        StringAssert.Contains(resultText, "沙箱执行失败");
        StringAssert.Contains(resultText, "InvalidOperationException");
        StringAssert.Contains(resultText, "真实的远端错误详情");
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
        StringAssert.EndsWith(runner.ExecutablePath, "\\bin\\TestRunner.exe");
        CollectionAssert.AreEqual(new[] { "--test", "sample data" }, runner.Arguments!.ToArray());
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
        Assert.AreEqual(runner.RemotePushPath, runner.RemotePullPath);
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "AgentLib.Coding.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class ThrowingWinRemoteShellRunner : IWinRemoteShellRunner
    {
        public Task PushAsync(string sourcePath, string remoteTargetPath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("真实的远端错误详情");

        public Task<string> ExecuteAsync(
            string executablePath,
            IReadOnlyList<string> arguments,
            int timeoutSeconds,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("不应执行");

        public Task PullAsync(string remoteSourcePath, string localOutputPath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("不应执行");
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
