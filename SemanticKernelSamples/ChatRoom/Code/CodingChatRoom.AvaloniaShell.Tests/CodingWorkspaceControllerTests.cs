using AgentLib;
using AgentLib.Coding;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class CodingWorkspaceControllerTests
{
    [TestMethod(DisplayName = "有效目录应规范化并提交工作区事务")]
    [Timeout(5000)]
    public async Task ChangeWorkspaceAsync_WhenDirectoryExists_CommitsNormalizedPath()
    {
        string workspacePath = CreateTestDirectory();
        var runtime = new TestWorkspaceRuntime();
        var controller = CreateController(runtime);

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync(
            Path.Join(workspacePath, "."),
            CancellationToken.None);

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(Path.GetFullPath(workspacePath), result.CurrentPath);
        Assert.AreEqual(result.CurrentPath, controller.CommittedWorkspacePath);
        Assert.AreEqual(result.CurrentPath, controller.WorkspaceInput);
        Assert.AreEqual(1, runtime.PrepareCount);
        Assert.IsTrue(runtime.LastTransaction!.Applied);
        Assert.IsTrue(runtime.LastTransaction.Committed);
    }

    [TestMethod(DisplayName = "不存在目录应失败且不准备候选事务")]
    [Timeout(5000)]
    public async Task ChangeWorkspaceAsync_WhenDirectoryDoesNotExist_KeepsCurrentPath()
    {
        string missingPath = Path.Join(CreateTestDirectory(), "missing");
        var runtime = new TestWorkspaceRuntime();
        var controller = CreateController(runtime);

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => controller.ChangeWorkspaceAsync(
            missingPath,
            CancellationToken.None));

        Assert.IsNull(controller.CommittedWorkspacePath);
        Assert.AreEqual(0, runtime.PrepareCount);
        StringAssert.Contains(controller.StatusText, "不存在");
    }

    [TestMethod(DisplayName = "候选准备失败应保留旧工作区")]
    [Timeout(5000)]
    public async Task ChangeWorkspaceAsync_WhenCandidatePreparationFails_KeepsPreviousPath()
    {
        string currentPath = CreateTestDirectory();
        string candidatePath = CreateTestDirectory();
        var runtime = new TestWorkspaceRuntime();
        var controller = CreateController(runtime);
        await controller.ChangeWorkspaceAsync(currentPath, CancellationToken.None);
        runtime.PrepareException = new InvalidOperationException("候选初始化失败");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => controller.ChangeWorkspaceAsync(
            candidatePath,
            CancellationToken.None));

        Assert.AreEqual(Path.GetFullPath(currentPath), controller.CommittedWorkspacePath);
        Assert.AreEqual(Path.GetFullPath(currentPath), controller.WorkspaceInput);
        StringAssert.Contains(controller.StatusText, "候选初始化失败");
    }

    [TestMethod(DisplayName = "相同规范化路径不应重复准备事务")]
    [Timeout(5000)]
    public async Task ChangeWorkspaceAsync_WhenNormalizedPathIsUnchanged_DoesNotPrepareAgain()
    {
        string workspacePath = CreateTestDirectory();
        var runtime = new TestWorkspaceRuntime();
        var controller = CreateController(runtime);
        await controller.ChangeWorkspaceAsync(workspacePath, CancellationToken.None);

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync(
            Path.Join(workspacePath, "."),
            CancellationToken.None);

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(1, runtime.PrepareCount);
    }

    [TestMethod(DisplayName = "Windows 路径比较应忽略大小写")]
    [Timeout(5000)]
    public async Task ChangeWorkspaceAsync_WithWindowsComparer_DoesNotRepeatForCaseOnlyDifference()
    {
        string workspacePath = CreateTestDirectory();
        var runtime = new TestWorkspaceRuntime();
        var controller = CreateController(runtime, StringComparer.OrdinalIgnoreCase);
        await controller.ChangeWorkspaceAsync(workspacePath, CancellationToken.None);

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync(
            workspacePath.ToUpperInvariant(),
            CancellationToken.None);

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(1, runtime.PrepareCount);
    }

    [TestMethod(DisplayName = "空白路径应清除已提交工作区")]
    [Timeout(5000)]
    public async Task ChangeWorkspaceAsync_WhenPathIsBlank_ClearsWorkspace()
    {
        string workspacePath = CreateTestDirectory();
        var runtime = new TestWorkspaceRuntime();
        var controller = CreateController(runtime);
        await controller.ChangeWorkspaceAsync(workspacePath, CancellationToken.None);

        WorkspaceChangeResult result = await controller.ChangeWorkspaceAsync("   ", CancellationToken.None);

        Assert.IsTrue(result.Changed);
        Assert.IsNull(result.CurrentPath);
        Assert.IsNull(controller.CommittedWorkspacePath);
        Assert.AreEqual(string.Empty, controller.WorkspaceInput);
        Assert.IsTrue(runtime.LastTransaction!.Committed);
    }

    [TestMethod(DisplayName = "状态发布失败应回滚已应用事务")]
    [Timeout(5000)]
    public async Task ChangeWorkspaceAsync_WhenPublishingFails_RollsBackAppliedTransaction()
    {
        string workspacePath = CreateTestDirectory();
        var runtime = new TestWorkspaceRuntime();
        var dispatcher = new TestMainThreadDispatcher { ThrowOnInvocation = 2 };
        var controller = new CodingWorkspaceController(runtime, dispatcher, StringComparer.Ordinal);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => controller.ChangeWorkspaceAsync(
            workspacePath,
            CancellationToken.None));

        Assert.IsTrue(runtime.LastTransaction!.Applied);
        Assert.IsTrue(runtime.LastTransaction.RolledBack);
        Assert.IsFalse(runtime.LastTransaction.Committed);
        Assert.IsNull(controller.CommittedWorkspacePath);
    }

    private static CodingWorkspaceController CreateController(
        TestWorkspaceRuntime runtime,
        StringComparer? pathComparer = null) =>
        new(runtime, new TestMainThreadDispatcher(), pathComparer ?? StringComparer.Ordinal);

    private static string CreateTestDirectory()
    {
        string path = Path.Join(Path.GetTempPath(), $"CodingChatRoom.Workspace.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestWorkspaceRuntime : ICodingWorkspaceRuntime
    {
        public int PrepareCount { get; private set; }

        public Exception? PrepareException { get; set; }

        public TestWorkspaceTransaction? LastTransaction { get; private set; }

        public Task<IWorkspaceChangeTransaction> PrepareWorkspaceChangeAsync(
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PrepareCount++;
            if (PrepareException is not null)
            {
                return Task.FromException<IWorkspaceChangeTransaction>(PrepareException);
            }

            LastTransaction = new TestWorkspaceTransaction(workspacePath);
            return Task.FromResult<IWorkspaceChangeTransaction>(LastTransaction);
        }
    }

    private sealed class TestWorkspaceTransaction(string? workspacePath) : IWorkspaceChangeTransaction
    {
        public string? WorkspacePath { get; } = workspacePath;

        public bool Applied { get; private set; }

        public bool RolledBack { get; private set; }

        public bool Committed { get; private set; }

        public void Apply() => Applied = true;

        public ValueTask RollbackAsync()
        {
            RolledBack = true;
            return default;
        }

        public void CommitAfterPublish() => Committed = true;

        public ValueTask DisposeAsync() => default;
    }

    private sealed class TestMainThreadDispatcher : IMainThreadDispatcher
    {
        private int _invocationCount;

        public int? ThrowOnInvocation { get; init; }

        public Task InvokeAsync(Func<Task> action)
        {
            _invocationCount++;
            if (_invocationCount == ThrowOnInvocation)
            {
                throw new InvalidOperationException("UI 状态发布失败");
            }

            return action();
        }

        public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            _invocationCount++;
            if (_invocationCount == ThrowOnInvocation)
            {
                throw new InvalidOperationException("UI 状态发布失败");
            }

            return await action();
        }

        public bool CheckAccess() => true;
    }
}
