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
        var application = new CodingChatApplication(manager, new TestSessionStore());

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
        var application = new CodingChatApplication(manager, store);

        await application.InitializeAsync();

        Assert.AreEqual(initialSessionId, application.SelectedSessionId);
        Assert.HasCount(3, application.Sessions);
    }

    [TestMethod(DisplayName = "新建会话时应复用真正空的当前会话")]
    [Timeout(5000)]
    public async Task CreateNewSessionAsyncShouldReuseTrulyEmptySession()
    {
        var manager = new CopilotChatManager();
        var application = new CodingChatApplication(manager, new TestSessionStore());
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
        var application = new CodingChatApplication(manager, new TestSessionStore());
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
        var application = new CodingChatApplication(manager, store);
        Guid previousSessionId = manager.SelectedSession.SessionId;

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => application.OpenSessionAsync(persisted.SessionId));

        Assert.AreEqual(previousSessionId, application.SelectedSessionId);
    }

    [TestMethod(DisplayName = "初始化历史摘要时不应加载历史会话内容")]
    [Timeout(5000)]
    public async Task InitializeAsyncShouldNotLoadHistorySessionContent()
    {
        CopilotChatSession persisted = CreateSession("损坏会话", "消息", DateTimeOffset.Now);
        var store = new TestSessionStore(persisted) { LoadException = new InvalidDataException("不应加载") };
        var manager = new CopilotChatManager();
        CopilotChatSession initialSession = manager.SelectedSession;
        var application = new CodingChatApplication(manager, store);

        await application.InitializeAsync();

        Assert.AreSame(initialSession, manager.SelectedSession);
    }

    [TestMethod(DisplayName = "删除历史失败时不应移除列表项")]
    [Timeout(5000)]
    public async Task DeleteSessionAsyncFailureShouldKeepListItem()
    {
        CopilotChatSession persisted = CreateSession("历史会话", "消息", DateTimeOffset.Now);
        var store = new TestSessionStore(persisted) { DeleteException = new IOException("删除失败") };
        var application = new CodingChatApplication(new CopilotChatManager(), store);
        await application.InitializeAsync();

        await Assert.ThrowsExactlyAsync<IOException>(() => application.DeleteSessionAsync(persisted.SessionId));

        Assert.IsTrue(application.Sessions.Any(session => session.SessionId == persisted.SessionId));
    }

    [TestMethod(DisplayName = "活动发送期间会话命令应全部禁用")]
    [Timeout(5000)]
    public async Task ActiveRunShouldDisableSessionCommands()
    {
        var runner = new ControllableRunner();
        var application = new CodingChatApplication(new CopilotChatManager(), new TestSessionStore(), runner);
        await application.InitializeAsync();
        var viewModel = new SessionListViewModel(application);

        Task sendTask = application.SendMessageAsync("检查代码");
        await runner.Started.Task;

        Assert.IsFalse(viewModel.CreateNewSessionCommand.CanExecute(null));
        Assert.IsFalse(viewModel.OpenSessionCommand.CanExecute(viewModel.Sessions[0]));
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
        var application = new CodingChatApplication(manager, store, new ControllableRunner());
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

        public Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CopilotChatSessionSummary> summaries = _sessions.Values
                .Select(session => new CopilotChatSessionSummary
                {
                    SessionId = session.SessionId,
                    Title = session.Title,
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

    private sealed class ControllableRunner : ICodingChatRunner
    {
        private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            bool enableAutomaticCompression,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            return Task.FromResult(new CodingAgentRunResult(
                CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false),
                _completion.Task.WaitAsync(cancellationToken)));
        }

        public void Complete() => _completion.TrySetResult(string.Empty);
    }
}