using System.Collections.Generic;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Logging;
using AgentLib.Model;

using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class CodingChatApplicationTests
{
    [TestMethod(DisplayName = "应用创建时应立即提供一个可复用空会话")]
    [Timeout(5000)]
    public void ApplicationCreationShouldImmediatelyProvideReusableEmptySession()
    {
        var manager = new CopilotChatManager();
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, new TestSessionStore());

        Assert.HasCount(1, application.Sessions);
        Assert.AreEqual(manager.SelectedSession.SessionId, application.SelectedSessionId);
        Assert.AreEqual(0, application.Sessions[0].MessageCount);
    }

    [TestMethod(DisplayName = "存在历史时初始化应保留启动新会话并追加历史摘要")]
    [Timeout(5000)]
    public async Task InitializeAsyncWithHistoryShouldKeepInitialSessionAndAppendHistory()
    {
        CopilotChatSession older = CreateSession("较早会话", "较早消息", DateTimeOffset.Now.AddHours(-1));
        CopilotChatSession newer = CreateSession("最近会话", "最近消息", DateTimeOffset.Now);
        var store = new TestSessionStore(older, newer);
        var manager = new CopilotChatManager();
        Guid initialSessionId = manager.SelectedSession.SessionId;
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, store);

        await application.InitializeAsync();

        Assert.AreEqual(initialSessionId, application.SelectedSessionId);
        Assert.HasCount(3, application.Sessions);
    }

    [TestMethod(DisplayName = "新建会话时应复用真正空的当前会话")]
    [Timeout(5000)]
    public async Task CreateNewSessionAsyncShouldReuseTrulyEmptySession()
    {
        var manager = new CopilotChatManager();
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, new TestSessionStore());
        await application.InitializeAsync();
        Guid emptySessionId = application.SelectedSessionId;

        await application.CreateNewSessionAsync();

        Assert.AreEqual(emptySessionId, application.SelectedSessionId);
        Assert.HasCount(1, application.Sessions);
    }

    [TestMethod(DisplayName = "当前会话非空时新建应把新会话插入列表顶部")]
    [Timeout(5000)]
    public async Task CreateNewSessionAsyncFromNonEmptySessionShouldInsertAtTop()
    {
        var manager = new CopilotChatManager();
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, new TestSessionStore());
        await application.InitializeAsync();
        await manager.AppendMessageAsync(new CopilotChatMessage(ChatRole.User, "现有问题"));
        Guid previousSessionId = application.SelectedSessionId;

        await application.CreateNewSessionAsync();

        Assert.AreNotEqual(previousSessionId, application.SelectedSessionId);
        Assert.AreEqual(application.SelectedSessionId, application.Sessions[0].SessionId);
    }

    [TestMethod(DisplayName = "打开历史失败时应恢复旧选择")]
    [Timeout(5000)]
    public async Task OpenSessionAsyncFailureShouldKeepPreviousSelection()
    {
        CopilotChatSession persisted = CreateSession("历史会话", "消息", DateTimeOffset.Now);
        var store = new TestSessionStore(persisted) { LoadException = new InvalidDataException("加载失败") };
        var manager = new CopilotChatManager();
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, store);
        Guid previousSessionId = manager.SelectedSession.SessionId;

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => application.OpenSessionAsync(persisted.SessionId));

        Assert.AreEqual(previousSessionId, application.SelectedSessionId);
    }

    [TestMethod(DisplayName = "打开历史会话后应同步历史工作路径")]
    public async Task OpenSessionAsyncShouldRestoreWorkspacePath()
    {
        string workspacePath = CreateTestDirectory();
        CopilotChatSession persisted = CreateSession("历史会话", "消息", DateTimeOffset.Now);
        persisted.WorkspacePath = workspacePath;
        var workspaceController = new CodingWorkspaceController(new TestMainThreadDispatcher());
        var application = CodingChatApplicationTestFactory.CreateApplication(
            new CopilotChatManager(),
            new TestSessionStore(persisted),
            workspaceController: workspaceController);

        await application.OpenSessionAsync(persisted.SessionId);

        Assert.AreEqual(Path.GetFullPath(workspacePath), workspaceController.NextRunWorkspacePath);
    }

    [TestMethod(DisplayName = "历史工作路径无效时应保留原会话和原工作路径")]
    public async Task OpenSessionAsyncWithInvalidWorkspacePathShouldKeepPreviousState()
    {
        string previousWorkspacePath = CreateTestDirectory();
        CopilotChatSession persisted = CreateSession("历史会话", "消息", DateTimeOffset.Now);
        persisted.WorkspacePath = Path.Join(CreateTestDirectory(), "missing");
        var manager = new CopilotChatManager();
        Guid previousSessionId = manager.SelectedSession.SessionId;
        var workspaceController = new CodingWorkspaceController(new TestMainThreadDispatcher());
        await workspaceController.ChangeWorkspaceAsync(previousWorkspacePath);
        var application = CodingChatApplicationTestFactory.CreateApplication(
            manager,
            new TestSessionStore(persisted),
            workspaceController: workspaceController);

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(
            () => application.OpenSessionAsync(persisted.SessionId));

        Assert.AreEqual(previousSessionId, application.SelectedSessionId);
        Assert.AreEqual(Path.GetFullPath(previousWorkspacePath), workspaceController.NextRunWorkspacePath);
    }

    [TestMethod(DisplayName = "初始化历史摘要时不应加载历史会话内容")]
    [Timeout(5000)]
    public async Task InitializeAsyncShouldNotLoadHistorySessionContent()
    {
        CopilotChatSession persisted = CreateSession("损坏会话", "消息", DateTimeOffset.Now);
        var store = new TestSessionStore(persisted) { LoadException = new InvalidDataException("不应加载") };
        var manager = new CopilotChatManager();
        CopilotChatSession initialSession = manager.SelectedSession;
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, store);

        await application.InitializeAsync();

        Assert.AreSame(initialSession, manager.SelectedSession);
    }

    [TestMethod(DisplayName = "删除历史失败时不应移除列表项")]
    [Timeout(5000)]
    public async Task DeleteSessionAsyncFailureShouldKeepListItem()
    {
        CopilotChatSession persisted = CreateSession("历史会话", "消息", DateTimeOffset.Now);
        var store = new TestSessionStore(persisted) { DeleteException = new IOException("删除失败") };
        var application = CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), store);
        await application.InitializeAsync();

        await Assert.ThrowsExactlyAsync<IOException>(() => application.DeleteSessionAsync(persisted.SessionId));

        Assert.IsTrue(application.Sessions.Any(session => session.SessionId == persisted.SessionId));
    }

    [TestMethod(DisplayName = "活动发送期间会话命令应全部禁用")]
    [Timeout(5000)]
    public async Task ActiveRunShouldDisableSessionCommands()
    {
        var runner = new ControllableRunner();
        var application = CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), new TestSessionStore(), runner);
        await application.InitializeAsync();
        var viewModel = new SessionListViewModel(application);
        var shell = MainViewModel.CreateForTests(viewModel, new ChatViewModel());

        Task sendTask = application.SendMessageAsync("检查代码");
        await runner.Started.Task;

        Assert.IsFalse(viewModel.CreateNewSessionCommand.CanExecute(null));
        Assert.IsFalse(shell.OpenSessionCommand.CanExecute(viewModel.Sessions[0]));
        Assert.IsFalse(viewModel.DeleteSessionCommand.CanExecute(viewModel.Sessions[0]));
        runner.Complete();
        await sendTask;
    }

    [TestMethod(DisplayName = "压缩对话时应禁用冲突操作并保存压缩后的历史")]
    [Timeout(5000)]
    public async Task CompressConversationAsyncShouldDisableConflictingOperationsAndSaveReducedHistory()
    {
        var compressionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCompression = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        const string summaryText = "这是压缩后的对话摘要";
        var chatClient = new FakeChatClient
        {
            OnGetResponseAsync = async (_, _, cancellationToken) =>
            {
                compressionStarted.TrySetResult();
                await releaseCompression.Task.WaitAsync(cancellationToken);
                return new ChatResponse([new ChatMessage(ChatRole.Assistant, summaryText)]);
            },
        };
        CopilotChatManager manager = CreateChatManager(chatClient);
        IManualSendMessageContext context = await manager.CreateManualSendMessageContextAsync();
        AgentSession agentSession = await context.GetAgentSessionAsync();
        agentSession.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.System, "系统提示"),
            new ChatMessage(ChatRole.User, "用户问题"),
            new ChatMessage(ChatRole.Assistant, "助手回答"),
        ]);
        var store = new TestSessionStore();
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, store, new ControllableRunner());
        await application.InitializeAsync();
        Assert.IsTrue(application.CanSend);
        Assert.IsTrue(application.CanCompressConversation);

        Task compressionTask = application.CompressConversationAsync();
        await compressionStarted.Task;

        Assert.IsTrue(application.IsCompressionActive);
        Assert.IsFalse(application.CanChangeSession);
        Assert.IsFalse(application.CanSend);
        Assert.IsFalse(application.CanCompressConversation);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => application.SendMessageAsync("并发发送"));

        releaseCompression.TrySetResult();
        await compressionTask;

        Assert.IsFalse(application.IsCompressionActive);
        Assert.AreEqual(1, store.SaveCount);
        Assert.AreSame(manager.SelectedSession, store.LastSavedSession);
        Assert.IsTrue(agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? compressedMessages));
        Assert.IsTrue(compressedMessages.Any(message => message.Text.Contains(summaryText, StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "循环迭代只应在启用自动压缩时执行轮末总结")]
    [DataRow(false, 0)]
    [DataRow(true, 1)]
    [Timeout(5000)]
    public async Task RunLoopIterationAsyncShouldConditionallyCompressConversation(
        bool enableAutomaticCompression,
        int expectedCompressionCount)
    {
        int compressionCount = 0;
        var chatClient = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) =>
            {
                compressionCount++;
                return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "循环摘要")]));
            },
        };
        CopilotChatManager manager = CreateChatManager(chatClient);
        IManualSendMessageContext context = await manager.CreateManualSendMessageContextAsync();
        AgentSession agentSession = await context.GetAgentSessionAsync();
        agentSession.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.System, "系统提示"),
            new ChatMessage(ChatRole.User, "用户问题"),
            new ChatMessage(ChatRole.Assistant, "助手回答"),
        ]);
        var runner = new ControllableRunner();
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, new TestSessionStore(), runner);
        application.IsLoopIterationEnabled = true;

        Task loopTask = application.RunLoopIterationAsync(
            "继续处理",
            new CodingChatRunOptions(enableAutomaticCompression, false, null));
        await runner.Started.Task;
        application.IsLoopIterationEnabled = false;
        runner.Complete();
        await loopTask;

        Assert.AreEqual(expectedCompressionCount, compressionCount);
    }

    [TestMethod(DisplayName = "循环迭代关闭自动压缩时下一轮应移除上一轮模型历史")]
    [Timeout(5000)]
    public async Task RunLoopIterationAsyncWithoutCompressionShouldRemovePreviousIterationHistory()
    {
        CopilotChatManager manager = CreateChatManager(new FakeChatClient());
        IManualSendMessageContext context = await manager.CreateManualSendMessageContextAsync();
        AgentSession agentSession = await context.GetAgentSessionAsync();
        agentSession.SetInMemoryChatHistory([new ChatMessage(ChatRole.System, "系统提示")]);
        Guid sessionId = manager.SelectedSession.SessionId;
        CodingChatApplication? application = null;
        var runner = new LoopHistoryRunner(agentSession, () => application!.IsLoopIterationEnabled = false);
        application = CodingChatApplicationTestFactory.CreateApplication(manager, new TestSessionStore(), runner);
        application.IsLoopIterationEnabled = true;

        await application.RunLoopIterationAsync(
            "继续处理",
            new CodingChatRunOptions(false, false, null));

        CollectionAssert.AreEqual(new[] { 1, 1 }, runner.HistoryCountsBeforeRun);
        Assert.AreEqual(sessionId, application.SelectedSessionId);
        Assert.HasCount(1, application.Sessions);
    }

    [TestMethod(DisplayName = "停止应立即结束循环失败后的等待")]
    [Timeout(5000)]
    public async Task StopActiveRunShouldCancelLoopRetryDelay()
    {
        var runner = new ControllableRunner();
        var application = CodingChatApplicationTestFactory.CreateApplication(
            new CopilotChatManager(),
            new TestSessionStore(),
            runner);
        application.IsLoopIterationEnabled = true;
        var retryDelayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        application.StateChanged += (_, _) =>
        {
            if (application.IsLoopActive && !application.IsRunActive)
            {
                retryDelayStarted.TrySetResult();
            }
        };

        Task loopTask = application.RunLoopIterationAsync(
            "继续处理",
            CodingChatRunOptions.Default);
        await runner.Started.Task;
        runner.Fail(new InvalidOperationException("模拟失败"));
        await retryDelayStarted.Task;

        application.StopActiveRun();
        await loopTask;

        Assert.IsFalse(application.IsLoopActive);
    }

    [TestMethod]
    public void HistoryShouldNotLoadWhenViewModelIsCreated()
    {
        var store = new TestSessionStore();
        var application = CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), store);
        _ = new SessionListViewModel(application);
        Assert.AreEqual(0, store.ListCount);
    }

    [TestMethod]
    public async Task RepeatedHistoryLoadsShouldNotDuplicateSessions()
    {
        var session = new CopilotChatSession();
        var application = CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), new TestSessionStore(session));
        var viewModel = new SessionListViewModel(application);
        await viewModel.LoadAsync();
        await viewModel.LoadAsync();
        Assert.HasCount(2, viewModel.Sessions);
    }

    [TestMethod]
    [DataRow("CUSTOM TITLE")]
    [DataRow("projects/sample")]
    public async Task HistorySearchShouldMatchTitleOrWorkspace(string query)
    {
        var session = new CopilotChatSession { WorkspacePath = "/projects/sample" };
        session.SetTitle("Custom title");
        var application = CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), new TestSessionStore(session));
        var viewModel = new SessionListViewModel(application);
        await viewModel.LoadAsync();
        viewModel.SearchText = query;
        Assert.AreEqual(session.SessionId, viewModel.Sessions.Single().SessionId);
    }

    [TestMethod]
    [DataRow("Refactor history")]
    public async Task HistorySearchShouldMatchWorkTaskName(string query)
    {
        var session = new CopilotChatSession
        {
            WorkTaskId = Guid.NewGuid(),
            WorkTaskName = query,
        };
        var application = CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), new TestSessionStore(session));
        var viewModel = new SessionListViewModel(application);
        await viewModel.LoadAsync();

        viewModel.SearchText = query;

        Assert.AreEqual(session.SessionId, viewModel.Sessions.Single().SessionId);
    }

    [TestMethod]
    public async Task HistorySearchShouldMatchWorkTaskId()
    {
        Guid workTaskId = Guid.NewGuid();
        var session = new CopilotChatSession { WorkTaskId = workTaskId, WorkTaskName = "Task" };
        var application = CodingChatApplicationTestFactory.CreateApplication(new CopilotChatManager(), new TestSessionStore(session));
        var viewModel = new SessionListViewModel(application);
        await viewModel.LoadAsync();

        viewModel.SearchText = workTaskId.ToString();

        Assert.AreEqual(session.SessionId, viewModel.Sessions.Single().SessionId);
    }

    [TestMethod]
    public async Task RenameShouldPersistTitleWithoutSwitchingSession()
    {
        var session = new CopilotChatSession();
        var manager = new CopilotChatManager();
        var store = new TestSessionStore(session);
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, store);
        await application.RenameSessionAsync(session.SessionId, "Updated title");
        Assert.AreEqual("Updated title", store.LastSavedSession?.Title);
    }

    [TestMethod]
    public async Task RenameShouldKeepCurrentSelection()
    {
        var session = new CopilotChatSession();
        var manager = new CopilotChatManager();
        Guid selectedId = manager.SelectedSession.SessionId;
        var application = CodingChatApplicationTestFactory.CreateApplication(manager, new TestSessionStore(session));
        await application.RenameSessionAsync(session.SessionId, "Updated title");
        Assert.AreEqual(selectedId, application.SelectedSessionId);
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Join(Path.GetTempPath(), $"CodingChatRoom.HistoryWorkspace.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static CopilotChatSession CreateSession(string title, string content, DateTimeOffset startedTime)
    {
        var session = new CopilotChatSession(Guid.NewGuid(), startedTime);
        session.SetTitle(title);
        session.AddMessageAsync(new CopilotChatMessage(ChatRole.User, content)).GetAwaiter().GetResult();
        return session;
    }

    private static CopilotChatManager CreateChatManager(FakeChatClient chatClient)
    {
        var manager = new CopilotChatManager();
        var model = new FakeLanguageModel(chatClient)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = "fake",
                ModelId = "fake",
                ModelName = "Fake",
            },
        };
        manager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([model]));
        return manager;
    }

    private sealed class TestMainThreadDispatcher : AgentLib.IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();

        public bool CheckAccess() => true;
    }

    private sealed class TestSessionStore : ICodingChatSessionStore
    {
        private readonly Dictionary<Guid, CopilotChatSession> _sessions;

        public TestSessionStore(params CopilotChatSession[] sessions)
        {
            _sessions = sessions.ToDictionary(session => session.SessionId);
        }

        public Exception? LoadException { get; init; }

        public Exception? DeleteException { get; init; }

        public int SaveCount { get; private set; }

        public CopilotChatSession? LastSavedSession { get; private set; }

        public int ListCount { get; private set; }

        public Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
        {
            ListCount++;
            IReadOnlyList<CopilotChatSessionSummary> summaries = _sessions.Values
                .Select(session => new CopilotChatSessionSummary
                {
                    SessionId = session.SessionId,
                    Title = session.Title,
                    WorkspacePath = session.WorkspacePath,
                    WorkTaskId = session.WorkTaskId,
                    WorkTaskName = session.WorkTaskName,
                    StartedTime = session.StartedTime,
                    MessageCount = session.ChatMessages.Count,
                })
                .OrderByDescending(summary => summary.StartedTime)
                .ToArray();
            return Task.FromResult(summaries);
        }

        public Task<CopilotChatSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            if (LoadException is not null)
            {
                return Task.FromException<CopilotChatSession>(LoadException);
            }

            return Task.FromResult(_sessions[sessionId]);
        }

        public Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            if (DeleteException is not null)
            {
                return Task.FromException<bool>(DeleteException);
            }

            return Task.FromResult(_sessions.Remove(sessionId));
        }

        public Task SaveSessionAsync(CopilotChatSession session, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastSavedSession = session;
            return Task.CompletedTask;
        }
    }

    private sealed class LoopHistoryRunner(AgentSession agentSession, Action stopLoop) : ICodingChatRunner
    {
        public List<int> HistoryCountsBeforeRun { get; } = [];

        public Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            CodingChatRunOptions options,
            CancellationToken cancellationToken)
        {
            Assert.IsTrue(agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? history));
            HistoryCountsBeforeRun.Add(history.Count);
            history.Add(new ChatMessage(ChatRole.User, "本轮用户消息"));
            history.Add(new ChatMessage(ChatRole.Assistant, "本轮助手消息"));
            agentSession.SetInMemoryChatHistory(history);
            if (HistoryCountsBeforeRun.Count == 2)
            {
                stopLoop();
            }

            return Task.FromResult(new CodingAgentRunResult(
                CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false),
                Task.FromResult<string?>(string.Empty)));
        }
    }

    private sealed class ControllableRunner : ICodingChatRunner
    {
        private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            CodingChatRunOptions options,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            return Task.FromResult(new CodingAgentRunResult(
                CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false),
                _completion.Task.WaitAsync(cancellationToken)));
        }

        public void Complete() => _completion.TrySetResult(string.Empty);

        public void Fail(Exception exception) => _completion.TrySetException(exception);
    }
}