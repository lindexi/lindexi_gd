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
    [Timeout(15000)]
    public async Task SetWorkspacePathAsync_WhenLanguageServerCannotStart_PublishesAllTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceToolProvider(invalidLanguageServerPath);

        await roleTool.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        await using CodingWorkspaceToolLease lease = await roleTool.AcquireLeaseAsync();

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
            lease.Tools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod(DisplayName = "Language Server 启动失败时符号工具应返回错误信息")]
    [Timeout(15000)]
    public async Task CodeSearchAsync_WhenLanguageServerCannotStart_ReturnsErrorMessage()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await roleTool.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        await using CodingWorkspaceToolLease lease = await roleTool.AcquireLeaseAsync();
        AIFunction codeSearch = lease.Tools
            .OfType<AIFunction>()
            .Single(tool => tool.Name == "code_search");

        object? result = await codeSearch.InvokeAsync(new AIFunctionArguments
        {
            ["searchQueries"] = new[] { "Sample" },
        });

        StringAssert.Contains(result?.ToString(), "roslyn_language_server_unavailable");
    }

    [TestMethod(DisplayName = "清空工作区时应移除已发布工具")]
    [Timeout(15000)]
    public async Task SetWorkspacePathAsync_WhenWorkspaceIsCleared_RemovesTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var toolProvider = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await toolProvider.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        await toolProvider.SetWorkspacePathAsync(null, CancellationToken.None);
        await using CodingWorkspaceToolLease lease = await toolProvider.AcquireLeaseAsync();

        Assert.IsEmpty(lease.Tools);
    }

    [TestMethod(DisplayName = "切换到无效工作区失败时应保留现有工具")]
    [Timeout(15000)]
    public async Task SetWorkspacePathAsync_WhenNewWorkspaceIsInvalid_KeepsExistingTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var toolProvider = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await toolProvider.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        string[] originalToolNames = await GetCurrentToolNamesAsync(toolProvider);

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => toolProvider.SetWorkspacePathAsync(
            Path.Join(workspacePath, "missing"),
            CancellationToken.None));

        CollectionAssert.AreEqual(originalToolNames, await GetCurrentToolNamesAsync(toolProvider));
    }

    [TestMethod(DisplayName = "工作区切换后旧会话应由运行租约保活")]
    [Timeout(5000)]
    public async Task LeaseShouldKeepRetiredSessionAliveUntilReleased()
    {
        var firstResource = new TrackingAsyncDisposable();
        var secondResource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
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
    [Timeout(5000)]
    public async Task DisposeAsyncShouldWaitForActiveLease()
    {
        var resource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
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

    [TestMethod(DisplayName = "Provider 重复释放应只释放当前 Session 一次")]
    [Timeout(5000)]
    public async Task DisposeAsyncShouldBeIdempotent()
    {
        var resource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [], resource)));
        await provider.SetWorkspacePathAsync("workspace", CancellationToken.None);

        Task firstDispose = provider.DisposeAsync().AsTask();
        Task secondDispose = provider.DisposeAsync().AsTask();

        await Task.WhenAll(firstDispose, secondDispose).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, resource.DisposeCount);
    }

    [TestMethod(DisplayName = "候选创建取消时应保留当前 Session")]
    [Timeout(5000)]
    public async Task SetWorkspacePathAsyncWhenCandidateCreationIsCanceledShouldKeepCurrentSession()
    {
        var currentResource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            async (path, _, cancellationToken) =>
            {
                if (path == "current")
                {
                    return new CodingWorkspaceToolSession(path, [CreateTool("current")], currentResource);
                }

                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("不可达");
            });
        await provider.SetWorkspacePathAsync("current", CancellationToken.None);
        using var cancellationTokenSource = new CancellationTokenSource();
        Task changeTask = provider.SetWorkspacePathAsync("next", cancellationTokenSource.Token);

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await changeTask);
        Assert.AreEqual("current", provider.WorkspacePath);
        CodingWorkspaceToolLease lease = await provider.AcquireLeaseAsync();
        Assert.AreEqual("current", lease.Tools.Single().Name);
        await lease.DisposeAsync();
        await provider.DisposeAsync();
        Assert.AreEqual(1, currentResource.DisposeCount);
    }

    [TestMethod(DisplayName = "并发候选应按实际发布顺序切换工作区")]
    [Timeout(5000)]
    public async Task SetWorkspacePathAsync_WhenOlderCandidateFinishesLast_PublishesLastCompletedCandidate()
    {
        var firstReady = new TaskCompletionSource<CodingWorkspaceToolSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReady = new TaskCompletionSource<CodingWorkspaceToolSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstResource = new TrackingAsyncDisposable();
        var secondResource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            (path, _, _) => path == "first" ? firstReady.Task : secondReady.Task);

        Task firstChange = provider.SetWorkspacePathAsync("first", CancellationToken.None);
        Task secondChange = provider.SetWorkspacePathAsync("second", CancellationToken.None);
        secondReady.SetResult(new CodingWorkspaceToolSession("second", [], secondResource));
        await secondChange;
        firstReady.SetResult(new CodingWorkspaceToolSession("first", [], firstResource));
        await firstChange;

        Assert.AreEqual("first", provider.WorkspacePath);
        await secondResource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(0, firstResource.DisposeCount);
        Assert.AreEqual(1, secondResource.DisposeCount);
        await provider.DisposeAsync();
    }

    [TestMethod(DisplayName = "Session 应冻结候选创建时的工具集合")]
    [Timeout(5000)]
    public async Task SessionShouldFreezeToolsAtCreationTime()
    {
        var tools = new List<AITool> { CreateTool("original") };
        await using var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, tools)));
        await provider.SetWorkspacePathAsync("workspace", CancellationToken.None);

        tools.Clear();
        await using CodingWorkspaceToolLease lease = await provider.AcquireLeaseAsync();

        CollectionAssert.AreEqual(new[] { "original" }, lease.Tools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod(DisplayName = "Provider 释放后创建完成的候选应自行释放资源")]
    [Timeout(5000)]
    public async Task CandidateCreatedAfterProviderDisposalShouldDisposeCreatedSession()
    {
        var candidateReady = new TaskCompletionSource<CodingWorkspaceToolSession>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var resource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            (_, _, _) => candidateReady.Task);
        Task<IWorkspaceChangeTransaction> transactionTask = provider.PrepareWorkspaceChangeAsync(
            "workspace",
            CancellationToken.None);

        Task disposeTask = provider.DisposeAsync().AsTask();

        await disposeTask.WaitAsync(TimeSpan.FromSeconds(1));
        candidateReady.SetResult(new CodingWorkspaceToolSession("workspace", [], resource));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
            await transactionTask.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.AreEqual(1, resource.DisposeCount);
    }

    [TestMethod(DisplayName = "Applied 事务回滚释放资源时不应阻塞当前租约获取")]
    [Timeout(5000)]
    public async Task AppliedTransactionRollbackShouldDisposeOutsideLifecycleLock()
    {
        var rejectedResource = new BlockingAsyncDisposable();
        var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(
                path,
                [CreateTool(path)],
                path == "candidate" ? rejectedResource : null)));
        IWorkspaceChangeTransaction? transaction = null;
        Task? rollbackTask = null;
        try
        {
            await provider.SetWorkspacePathAsync("current", CancellationToken.None);
            transaction = await provider.PrepareWorkspaceChangeAsync(
                "candidate",
                CancellationToken.None);
            transaction.Apply();

            rollbackTask = transaction.RollbackAsync().AsTask();
            await rejectedResource.DisposeStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            await using CodingWorkspaceToolLease lease = await provider
                .AcquireLeaseAsync()
                .WaitAsync(TimeSpan.FromSeconds(1));
            Assert.AreEqual("current", lease.WorkspacePath);
        }
        finally
        {
            rejectedResource.ReleaseDispose.TrySetResult();
            if (rollbackTask is not null)
            {
                await rollbackTask.WaitAsync(TimeSpan.FromSeconds(1));
            }
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }

            await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        }
    }

    [TestMethod(DisplayName = "Prepare 和 Apply 均不应提前发布候选工作区")]
    [Timeout(5000)]
    public async Task PrepareAndApplyShouldKeepCommittedWorkspaceUnchanged()
    {
        await using CodingWorkspaceToolProvider provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [CreateTool(path)])));
        await provider.SetWorkspacePathAsync("current", CancellationToken.None);
        await using IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);

        Assert.AreEqual("current", provider.WorkspacePath);
        transaction.Apply();

        Assert.AreEqual("current", provider.WorkspacePath);
        await using CodingWorkspaceToolLease lease = await provider.AcquireLeaseAsync();
        Assert.AreEqual("current", lease.WorkspacePath);
    }

    [TestMethod(DisplayName = "同一工作区事务只能 Apply 一次")]
    [Timeout(5000)]
    public async Task TransactionShouldApplyOnlyOnce()
    {
        await using CodingWorkspaceToolProvider provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [])));
        await using IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);
        transaction.Apply();

        _ = Assert.ThrowsExactly<InvalidOperationException>(transaction.Apply);
    }

    [TestMethod(DisplayName = "Applied 屏障解除前应拒绝第二个事务 Apply")]
    [Timeout(5000)]
    public async Task SecondTransactionShouldWaitUntilAppliedBarrierIsResolved()
    {
        await using CodingWorkspaceToolProvider provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [])));
        await using IWorkspaceChangeTransaction first = await provider.PrepareWorkspaceChangeAsync(
            "first",
            CancellationToken.None);
        await using IWorkspaceChangeTransaction second = await provider.PrepareWorkspaceChangeAsync(
            "second",
            CancellationToken.None);
        first.Apply();

        _ = Assert.ThrowsExactly<InvalidOperationException>(second.Apply);
        await first.RollbackAsync();
        second.Apply();
        second.CommitAfterPublish();

        Assert.AreEqual("second", provider.WorkspacePath);
    }

    [TestMethod(DisplayName = "发布前回滚应保留当前工作区并释放候选资源")]
    [Timeout(5000)]
    public async Task RollbackBeforePublishShouldKeepCurrentWorkspaceAndDisposeCandidate()
    {
        var candidateResource = new TrackingAsyncDisposable();
        await using CodingWorkspaceToolProvider provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(
                path,
                [CreateTool(path)],
                path == "candidate" ? candidateResource : null)));
        await provider.SetWorkspacePathAsync("current", CancellationToken.None);
        await using IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);
        transaction.Apply();

        await transaction.RollbackAsync();

        Assert.AreEqual("current", provider.WorkspacePath);
        Assert.AreEqual(1, candidateResource.DisposeCount);
    }

    [TestMethod(DisplayName = "发布后提交应切换工作区并由旧租约延迟退休旧资源")]
    [Timeout(5000)]
    public async Task CommitAfterPublishShouldSwitchWorkspaceAndRetireOldSessionAfterLeaseRelease()
    {
        var currentResource = new TrackingAsyncDisposable();
        var candidateResource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(
                path,
                [CreateTool(path)],
                path == "current" ? currentResource : candidateResource)));
        await provider.SetWorkspacePathAsync("current", CancellationToken.None);
        CodingWorkspaceToolLease currentLease = await provider.AcquireLeaseAsync();
        await using IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);
        transaction.Apply();

        transaction.CommitAfterPublish();

        Assert.AreEqual("candidate", provider.WorkspacePath);
        Assert.AreEqual(0, currentResource.DisposeCount);
        await currentLease.DisposeAsync();
        await currentResource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, currentResource.DisposeCount);
        await provider.DisposeAsync();
        Assert.AreEqual(1, candidateResource.DisposeCount);
    }

    [TestMethod(DisplayName = "发布后提交的事务不得回滚")]
    [Timeout(5000)]
    public async Task CommittedTransactionShouldRejectRollback()
    {
        await using CodingWorkspaceToolProvider provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [])));
        await using IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);
        transaction.Apply();
        transaction.CommitAfterPublish();

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await transaction.RollbackAsync());
        Assert.AreEqual("candidate", provider.WorkspacePath);
    }

    [TestMethod(DisplayName = "发布后重复提交事务应保持幂等")]
    [Timeout(5000)]
    public async Task CommitAfterPublishShouldBeIdempotent()
    {
        var currentResource = new TrackingAsyncDisposable();
        var candidateResource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(
                path,
                [],
                path == "current" ? currentResource : candidateResource)));
        await provider.SetWorkspacePathAsync("current", CancellationToken.None);
        await using IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);
        transaction.Apply();

        transaction.CommitAfterPublish();
        transaction.CommitAfterPublish();

        Assert.AreEqual("candidate", provider.WorkspacePath);
        await currentResource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, currentResource.DisposeCount);
        await provider.DisposeAsync();
        Assert.AreEqual(1, candidateResource.DisposeCount);
    }

    [TestMethod(DisplayName = "Provider 释放应等待 Applied 事务提交或回滚")]
    [Timeout(5000)]
    public async Task DisposeAsyncShouldWaitForAppliedTransactionResolution()
    {
        var candidateResource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [], candidateResource)));
        await using IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);
        transaction.Apply();

        Task disposeTask = provider.DisposeAsync().AsTask();

        Assert.IsFalse(disposeTask.IsCompleted);
        transaction.CommitAfterPublish();
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, candidateResource.DisposeCount);
    }

    [TestMethod(DisplayName = "Provider 释放应自动回滚尚未 Apply 的事务")]
    [Timeout(5000)]
    public async Task DisposeAsyncShouldRollbackPreparedTransaction()
    {
        var candidateResource = new TrackingAsyncDisposable();
        var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, [], candidateResource)));
        IWorkspaceChangeTransaction transaction = await provider.PrepareWorkspaceChangeAsync(
            "candidate",
            CancellationToken.None);

        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(1, candidateResource.DisposeCount);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(transaction.Apply);
        await transaction.DisposeAsync();
    }

    private static AITool CreateTool(string name) => AIFunctionFactory.Create(() => name, name);

    private static CodingWorkspaceToolProvider CreateProvider(
        Func<string, string, CancellationToken, Task<CodingWorkspaceToolSession>> createSession) =>
        new(new TestSessionProvider(createSession));

    private static async Task<string[]> GetCurrentToolNamesAsync(CodingWorkspaceToolProvider provider)
    {
        await using CodingWorkspaceToolLease lease = await provider.AcquireLeaseAsync();
        return lease.Tools.Select(tool => tool.Name).ToArray();
    }

    private sealed class TestSessionProvider(
        Func<string, string, CancellationToken, Task<CodingWorkspaceToolSession>> createSession)
        : ICodingWorkspaceToolSessionProvider
    {
        public Task<CodingWorkspaceToolSession> CreateAsync(
            string workspacePath,
            CancellationToken cancellationToken) =>
            createSession(workspacePath, "test-server", cancellationToken);
    }

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

    private sealed class BlockingAsyncDisposable : IAsyncDisposable
    {
        public TaskCompletionSource DisposeStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseDispose { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask DisposeAsync()
        {
            DisposeStarted.TrySetResult();
            await ReleaseDispose.Task;
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
