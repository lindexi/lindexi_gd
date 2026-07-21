using AgentLib.Coding;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

/// <summary>
/// <see cref="CodingWorkspaceToolProvider"/> 的单元测试。
/// </summary>
[TestClass]
public sealed class CodingWorkspaceToolProviderTests
{
    [TestMethod(DisplayName = "Language Server 启动失败时仍应发布完整工作区工具")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task SetWorkspacePathAsync_WhenLanguageServerCannotStart_PublishesAllTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceToolProvider(invalidLanguageServerPath);

        await roleTool.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        CollectionAssert.AreEquivalent(
            new[]
            {
                "get_projects_in_solution",
                "get_files_in_project",
                "code_search",
                "find_symbol",
                "find_all_references",
                "ListDirectory",
                "FindEntriesByName",
                "FindFilesMatchingPattern",
                "ReadFileLines",
                "WriteFileContent",
                "ReplaceStringInFile",
                "MultiReplaceStringInFile",
                "run_build",
                "run_tests",
                "read_last_log_lines",
                "search_last_log",
            },
            roleTool.AITools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod(DisplayName = "Language Server 启动失败时符号工具应返回错误信息")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task CodeSearchAsync_WhenLanguageServerCannotStart_ReturnsErrorMessage()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await roleTool.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        AIFunction codeSearch = roleTool.AITools
            .OfType<AIFunction>()
            .Single(tool => tool.Name == "code_search");

        object? result = await codeSearch.InvokeAsync(new AIFunctionArguments
        {
            ["searchQueries"] = new[] { "Sample" },
        });

        StringAssert.Contains(result?.ToString(), "roslyn_language_server_unavailable");
    }

    [TestMethod(DisplayName = "清空工作区时应移除已发布工具")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task SetWorkspacePathAsync_WhenWorkspaceIsCleared_RemovesTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var toolProvider = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await toolProvider.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        await toolProvider.SetWorkspacePathAsync(null, CancellationToken.None);

        Assert.IsEmpty(toolProvider.AITools);
    }

    [TestMethod(DisplayName = "切换到无效工作区失败时应保留现有工具")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task SetWorkspacePathAsync_WhenNewWorkspaceIsInvalid_KeepsExistingTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var toolProvider = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await toolProvider.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        string[] originalToolNames = toolProvider.AITools.Select(tool => tool.Name).ToArray();

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => toolProvider.SetWorkspacePathAsync(
            Path.Join(workspacePath, "missing"),
            CancellationToken.None));

        CollectionAssert.AreEqual(originalToolNames, toolProvider.AITools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod(DisplayName = "工作区切换后旧会话应由运行租约保活")]
    public async Task LeaseShouldKeepRetiredSessionAliveUntilReleased()
    {
        var firstResource = new TrackingAsyncDisposable();
        var secondResource = new TrackingAsyncDisposable();
        var provider = new CodingWorkspaceToolProvider(
            "test-server",
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(
                path,
                [CreateTool(Path.GetFileName(path))],
                path.EndsWith("first", StringComparison.Ordinal) ? firstResource : secondResource)));
        await provider.SetWorkspacePathAsync("first", CancellationToken.None);
        await using CodingWorkspaceToolLease lease = await provider.AcquireLeaseAsync();

        await provider.SetWorkspacePathAsync("second", CancellationToken.None);

        Assert.AreEqual(0, firstResource.DisposeCount);
        Assert.AreEqual("first", lease.WorkspacePath);
        Assert.AreEqual("first", lease.Tools.Single().Name);

        await lease.DisposeAsync();
        await firstResource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, firstResource.DisposeCount);
        await provider.DisposeAsync();
        Assert.AreEqual(1, secondResource.DisposeCount);
    }

    [TestMethod(DisplayName = "Provider 释放应等待最后一个租约")]
    public async Task DisposeAsyncShouldWaitForActiveLease()
    {
        var resource = new TrackingAsyncDisposable();
        var provider = new CodingWorkspaceToolProvider(
            "test-server",
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [], resource)));
        await provider.SetWorkspacePathAsync("workspace", CancellationToken.None);
        CodingWorkspaceToolLease lease = await provider.AcquireLeaseAsync();

        Task disposeTask = provider.DisposeAsync().AsTask();

        Assert.IsFalse(disposeTask.IsCompleted);
        Assert.AreEqual(0, resource.DisposeCount);
        await lease.DisposeAsync();
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, resource.DisposeCount);
    }

    [TestMethod(DisplayName = "迟到的旧候选不应覆盖较新的工作区")]
    public async Task SetWorkspacePathAsync_WhenOlderCandidateFinishesLast_KeepsNewerWorkspace()
    {
        var firstReady = new TaskCompletionSource<CodingWorkspaceToolSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReady = new TaskCompletionSource<CodingWorkspaceToolSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstResource = new TrackingAsyncDisposable();
        var secondResource = new TrackingAsyncDisposable();
        var provider = new CodingWorkspaceToolProvider(
            "test-server",
            (path, _, _) => path == "first" ? firstReady.Task : secondReady.Task);

        Task firstChange = provider.SetWorkspacePathAsync("first", CancellationToken.None);
        Task secondChange = provider.SetWorkspacePathAsync("second", CancellationToken.None);
        secondReady.SetResult(new CodingWorkspaceToolSession("second", [], secondResource));
        await secondChange;
        firstReady.SetResult(new CodingWorkspaceToolSession("first", [], firstResource));
        await firstChange;

        Assert.AreEqual("second", provider.WorkspacePath);
        await firstResource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, firstResource.DisposeCount);
        Assert.AreEqual(0, secondResource.DisposeCount);
        await provider.DisposeAsync();
    }

    private static AITool CreateTool(string name) => AIFunctionFactory.Create(() => name, name);

    private sealed class TrackingAsyncDisposable : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            Disposed.TrySetResult();
            return default;
        }
    }

    private static string CreateInvalidLanguageServerFile(string workspacePath)
    {
        string filePath = Path.Join(workspacePath, "invalid-language-server.txt");
        File.WriteAllText(filePath, "not an executable");
        return filePath;
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Join(
            AppContext.BaseDirectory,
            nameof(CodingWorkspaceToolProviderTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
