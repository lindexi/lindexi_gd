using System.Collections.Generic;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Logging;
using AgentLib.Model;

using CodingChatRoom.AvaloniaShell.Services;

using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class CodingChatSendingTests
{
    [TestMethod(DisplayName = "发送应使用启动时已提交的工作路径")]
    [Timeout(5000)]
    public async Task SendMessageAsyncShouldUseCommittedWorkspacePath()
    {
        string workspacePath = CreateTestDirectory();
        var manager = new CopilotChatManager();
        var runner = new TestCodingChatRunner(manager);
        var workspaceController = new CodingWorkspaceController(
            new TestWorkspaceRuntime(),
            new ImmediateMainThreadDispatcher());
        await workspaceController.ChangeWorkspaceAsync(workspacePath, CancellationToken.None);
        var application = new CodingChatApplication(
            manager,
            new TestSessionStore(),
            runner,
            workspaceController);
        await application.InitializeAsync();

        Task sendTask = application.SendMessageAsync("检查工作区");
        await runner.Started.Task;

        Assert.AreEqual(Path.GetFullPath(workspacePath), runner.ObservedWorkspacePath);
        runner.Complete("完成");
        await sendTask;
    }

    [TestMethod(DisplayName = "发送默认应启用自动压缩")]
    [Timeout(5000)]
    public async Task SendMessageAsyncShouldEnableAutomaticCompressionByDefault()
    {
        var manager = new CopilotChatManager();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, new TestSessionStore(), runner);
        await application.InitializeAsync();

        Task sendTask = application.SendMessageAsync("检查代码");
        await runner.Started.Task;

        Assert.IsTrue(runner.ObservedAutomaticCompressionEnabled);
        runner.Complete("完成");
        await sendTask;
    }

    [TestMethod(DisplayName = "发送应允许关闭自动压缩")]
    [Timeout(5000)]
    public async Task SendMessageAsyncShouldAllowDisablingAutomaticCompression()
    {
        var manager = new CopilotChatManager();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, new TestSessionStore(), runner);
        await application.InitializeAsync();

        Task sendTask = application.SendMessageAsync("检查代码", enableAutomaticCompression: false);
        await runner.Started.Task;

        Assert.IsFalse(runner.ObservedAutomaticCompressionEnabled);
        runner.Complete("完成");
        await sendTask;
    }

    [TestMethod(DisplayName = "发送多模态内容时应保留文本图片顺序和类型")]
    [Timeout(5000)]
    public async Task SendMessageAsyncShouldPreserveMultimodalContentOrder()
    {
        var manager = new CopilotChatManager();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, new TestSessionStore(), runner);
        await application.InitializeAsync();
        IReadOnlyList<AIContent> contents =
        [
            new TextContent("分析图片"),
            new DataContent(new byte[] { 1, 2, 3 }, "image/jpeg"),
        ];

        Task sendTask = application.SendMessageAsync(contents);
        await runner.Started.Task;

        IReadOnlyList<AIContent> observedContents = runner.ObservedContents!;
        Assert.HasCount(2, observedContents);
        Assert.IsInstanceOfType<TextContent>(observedContents[0]);
        DataContent imageContent = Assert.IsInstanceOfType<DataContent>(observedContents[1]);
        Assert.AreEqual("image/jpeg", imageContent.MediaType);
        runner.Complete("完成");
        await sendTask;
    }

    [TestMethod(DisplayName = "单次发送应只启动一次运行并在完成后保存会话")]
    [Timeout(5000)]
    public async Task SendMessageAsyncShouldRunOnceAndSaveCompletedSession()
    {
        var manager = new CopilotChatManager();
        var store = new TestSessionStore();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, store, runner);
        await application.InitializeAsync();

        Task sendTask = application.SendMessageAsync("检查代码");
        await runner.Started.Task;

        Assert.AreEqual(1, runner.RunCount);
        Assert.IsTrue(application.IsRunActive);
        Assert.HasCount(3, manager.SelectedSession.ChatMessages);
        Assert.AreSame(runner.AssistantMessage, manager.SelectedSession.ChatMessages[2]);

        runner.Complete("已完成");
        await sendTask;

        Assert.IsFalse(application.IsRunActive);
        Assert.AreEqual("已完成", runner.AssistantMessage.Content);
        Assert.AreEqual(1, store.SaveCount);
        Assert.AreEqual(manager.SelectedSession.SessionId, application.Sessions[0].SessionId);
        Assert.AreEqual(2, application.Sessions[0].MessageCount);
    }

    [TestMethod(DisplayName = "运行期间插话后应通过当前助手消息收到更新")]
    [Timeout(5000)]
    public async Task SendMessageAsyncWhileActiveShouldUpdateCurrentAssistantMessage()
    {
        var manager = new CopilotChatManager();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, new TestSessionStore(), runner);
        await application.InitializeAsync();
        Task firstSend = application.SendMessageAsync("第一条");
        await runner.Started.Task;
        CopilotChatMessage assistantMessage = runner.AssistantMessage;

        await application.SendMessageAsync("第二条");

        Assert.AreEqual("插话回复：第二条", assistantMessage.Content);
        runner.Complete("完成");
        await firstSend;
    }

    [TestMethod(DisplayName = "运行结束中间态插话后仍应通过当前助手消息收到更新")]
    [Timeout(5000)]
    public async Task SendMessageAsyncWhileRunIsFinishingShouldUpdateCurrentAssistantMessage()
    {
        var manager = new CopilotChatManager();
        var store = new TestSessionStore { BlockSave = true };
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, store, runner);
        await application.InitializeAsync();
        Task firstSend = application.SendMessageAsync("第一条");
        await runner.Started.Task;
        CopilotChatMessage assistantMessage = runner.AssistantMessage;
        runner.Complete("首轮完成");
        await store.SaveStarted.Task;

        Task secondSend = application.SendMessageAsync("结束边界插话");
        await runner.SecondRunStarted.Task;
        CopilotChatMessage secondAssistantMessage = runner.AssistantMessage;
        runner.Complete("结束边界回复");
        store.ReleaseSave();
        await Task.WhenAll(firstSend, secondSend);

        Assert.AreEqual("首轮完成", assistantMessage.Content);
        Assert.AreEqual("结束边界回复", secondAssistantMessage.Content);
    }

    [TestMethod(DisplayName = "停止活动发送应取消完整运行生命周期")]
    [Timeout(5000)]
    public async Task StopActiveRunShouldCancelCompletionTask()
    {
        var manager = new CopilotChatManager();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, new TestSessionStore(), runner);
        await application.InitializeAsync();
        Task sendTask = application.SendMessageAsync("等待取消");
        await runner.Started.Task;

        application.StopActiveRun();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await sendTask);
        Assert.IsTrue(runner.ObservedCancellationToken.IsCancellationRequested);
        Assert.IsFalse(application.IsRunActive);
    }

    [TestMethod(DisplayName = "运行异常后应恢复可发送状态")]
    [Timeout(5000)]
    public async Task SendMessageAsyncFailureShouldRestoreAvailableState()
    {
        var manager = new CopilotChatManager();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, new TestSessionStore(), runner);
        await application.InitializeAsync();
        Task sendTask = application.SendMessageAsync("触发异常");
        await runner.Started.Task;

        runner.Fail(new InvalidOperationException("模型失败"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sendTask);
        Assert.IsFalse(application.IsRunActive);
        Assert.IsTrue(application.CanSend);
    }

    [TestMethod(DisplayName = "运行失败且保存也失败时应保留运行异常")]
    [Timeout(5000)]
    public async Task RunFailureShouldTakePrecedenceOverPersistenceFailure()
    {
        var manager = new CopilotChatManager();
        var store = new TestSessionStore { SaveException = new IOException("保存失败") };
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, store, runner);
        await application.InitializeAsync();
        Task sendTask = application.SendMessageAsync("触发异常");
        await runner.Started.Task;
        runner.Fail(new InvalidOperationException("模型失败"));

        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await sendTask);

        Assert.AreEqual("模型失败", exception.Message);
        Assert.IsFalse(application.IsRunActive);
    }

    [TestMethod(DisplayName = "运行成功但保存失败时应抛出保存异常")]
    [Timeout(5000)]
    public async Task PersistenceFailureShouldBeThrownAfterSuccessfulRun()
    {
        var manager = new CopilotChatManager();
        var store = new TestSessionStore { SaveException = new IOException("保存失败") };
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, store, runner);
        await application.InitializeAsync();
        Task sendTask = application.SendMessageAsync("正常运行");
        await runner.Started.Task;
        runner.Complete("完成");

        IOException exception = await Assert.ThrowsExactlyAsync<IOException>(async () => await sendTask);

        Assert.AreEqual("保存失败", exception.Message);
        Assert.IsFalse(application.IsRunActive);
    }

    [TestMethod(DisplayName = "空回复完成后应保存没有占位符的助手消息")]
    [Timeout(5000)]
    public async Task EmptyResponseShouldSaveAssistantMessageWithoutPlaceholder()
    {
        var manager = new CopilotChatManager();
        var store = new TestSessionStore();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, store, runner);
        await application.InitializeAsync();
        Task sendTask = application.SendMessageAsync("无内容回复");
        await runner.Started.Task;

        runner.Complete(null);
        await sendTask;

        Assert.AreEqual(string.Empty, runner.AssistantMessage.Content);
        Assert.AreNotEqual(CopilotChatMessage.PlaceholderContent, store.SavedSession!.ChatMessages[2].Content);
    }

    [TestMethod(DisplayName = "发送期间切换选择后保存仍应捕获开始运行的会话实例")]
    [Timeout(5000)]
    public async Task SendMessageAsyncShouldSaveCapturedSessionInstance()
    {
        var manager = new CopilotChatManager();
        var store = new TestSessionStore();
        var runner = new TestCodingChatRunner(manager);
        var application = new CodingChatApplication(manager, store, runner);
        await application.InitializeAsync();
        CopilotChatSession runningSession = manager.SelectedSession;
        Task sendTask = application.SendMessageAsync("检查代码");
        await runner.Started.Task;
        var otherSession = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        manager.AddSession(otherSession, select: true);

        runner.Complete("完成");
        await sendTask;

        Assert.AreSame(runningSession, store.SavedSession);
    }

    private sealed class TestCodingChatRunner(CopilotChatManager manager) : ICodingChatRunner
    {
        private TaskCompletionSource<string?> _completion = CreateCompletionSource();

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondRunStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RunCount { get; private set; }

        public CancellationToken ObservedCancellationToken { get; private set; }

        public string? ObservedWorkspacePath { get; private set; }

        public bool ObservedAutomaticCompressionEnabled { get; private set; }

        public IReadOnlyList<AIContent>? ObservedContents { get; private set; }

        public IReadOnlyList<AIContent>? InjectedContents { get; private set; }

        public CopilotChatMessage AssistantMessage { get; private set; } = CreateAssistantMessage();

        public async Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            bool enableAutomaticCompression,
            CancellationToken cancellationToken)
        {
            RunCount++;
            if (RunCount > 1)
            {
                _completion = CreateCompletionSource();
                AssistantMessage = CreateAssistantMessage();
            }

            ObservedContents = contents;
            ObservedWorkspacePath = workspacePath;
            ObservedAutomaticCompressionEnabled = enableAutomaticCompression;
            ObservedCancellationToken = cancellationToken;
            var userMessage = CopilotChatMessage.CreateUser(contents);
            await manager.AppendMessageAsync(userMessage, cancellationToken);
            await manager.SelectedSession.AddMessageAsync(AssistantMessage);
            Started.TrySetResult();
            if (RunCount == 2)
            {
                SecondRunStarted.TrySetResult();
            }

            return new CodingAgentRunResult(AssistantMessage, CompleteAsync(cancellationToken));
        }

        public Task InjectMessageAsync(
            IReadOnlyList<AIContent> contents,
            CancellationToken cancellationToken)
        {
            InjectedContents = contents;
            string text = Assert.IsInstanceOfType<TextContent>(contents[0]).Text;
            AssistantMessage.ClearMessageItems();
            AssistantMessage.AppendText($"插话回复：{text}");
            return Task.CompletedTask;
        }

        public void Complete(string? content)
        {
            AssistantMessage.ClearMessageItems();
            if (!string.IsNullOrEmpty(content))
            {
                AssistantMessage.AppendText(content);
            }

            _completion.TrySetResult(content);
        }

        public void Fail(Exception exception) => _completion.TrySetException(exception);

        private async Task<string?> CompleteAsync(CancellationToken cancellationToken)
        {
            return await _completion.Task.WaitAsync(cancellationToken);
        }

        private static TaskCompletionSource<string?> CreateCompletionSource()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static CopilotChatMessage CreateAssistantMessage()
            => CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, isPresetInfo: false);
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Join(Path.GetTempPath(), $"CodingChatRoom.SendWorkspace.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestWorkspaceRuntime : ICodingWorkspaceRuntime
    {
        public Task<IWorkspaceChangeTransaction> PrepareWorkspaceChangeAsync(
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IWorkspaceChangeTransaction>(new TestWorkspaceTransaction(workspacePath));
        }
    }

    private sealed class TestWorkspaceTransaction(string? workspacePath) : IWorkspaceChangeTransaction
    {
        public string? WorkspacePath { get; } = workspacePath;

        public void Apply()
        {
        }

        public ValueTask RollbackAsync() => default;

        public void CommitAfterPublish()
        {
        }

        public ValueTask DisposeAsync() => default;
    }

    private sealed class ImmediateMainThreadDispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();

        public bool CheckAccess() => true;
    }

    private sealed class TestSessionStore : ICodingChatSessionStore
    {
        private readonly TaskCompletionSource _continueSave = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Exception? SaveException { get; init; }

        public bool BlockSave { get; init; }

        public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SaveCount { get; private set; }

        public CopilotChatSession? SavedSession { get; private set; }

        public Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CopilotChatSessionSummary>>([]);

        public Task<CopilotChatSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async Task SaveSessionAsync(CopilotChatSession session, CancellationToken cancellationToken = default)
        {
            SaveStarted.TrySetResult();
            if (BlockSave)
            {
                await _continueSave.Task.WaitAsync(cancellationToken);
            }

            if (SaveException is not null)
            {
                throw SaveException;
            }

            SaveCount++;
            SavedSession = session;
        }

        public void ReleaseSave() => _continueSave.TrySetResult();
    }
}
