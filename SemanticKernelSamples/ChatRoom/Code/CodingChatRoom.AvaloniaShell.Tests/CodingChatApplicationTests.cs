using System.Collections.Generic;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Logging;
using AgentLib.Model;

using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class CodingChatApplicationTests
{
    [TestMethod(DisplayName = "没有历史时初始化应保留一个可复用空会话")]
    [Timeout(5000)]
    public async Task InitializeAsyncWithoutHistoryShouldKeepReusableEmptySession()
    {
        var manager = new CopilotChatManager();
        var application = new CodingChatApplication(manager, new TestSessionStore());

        await application.InitializeAsync();

        Assert.HasCount(1, application.Sessions);
        Assert.AreEqual(manager.SelectedSession.SessionId, application.SelectedSessionId);
        Assert.AreEqual(0, application.Sessions[0].MessageCount);
    }

    [TestMethod(DisplayName = "存在历史时初始化应打开最近会话并移除未使用初始空会话")]
    [Timeout(5000)]
    public async Task InitializeAsyncWithHistoryShouldOpenMostRecentSession()
    {
        CopilotChatSession older = CreateSession("较早会话", "较早消息", DateTimeOffset.Now.AddHours(-1));
        CopilotChatSession newer = CreateSession("最近会话", "最近消息", DateTimeOffset.Now);
        var store = new TestSessionStore(older, newer);
        var manager = new CopilotChatManager();
        Guid initialSessionId = manager.SelectedSession.SessionId;
        var application = new CodingChatApplication(manager, store);

        await application.InitializeAsync();

        Assert.AreEqual(newer.SessionId, application.SelectedSessionId);
        Assert.IsFalse(manager.ChatSessions.Any(session => session.SessionId == initialSessionId));
        Assert.HasCount(2, application.Sessions);
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

    [TestMethod(DisplayName = "初始化恢复全部失败时不应污染管理器会话集合")]
    [Timeout(5000)]
    public async Task InitializeAsyncRestoreFailureShouldNotPolluteManager()
    {
        CopilotChatSession persisted = CreateSession("损坏会话", "消息", DateTimeOffset.Now);
        var store = new TestSessionStore(persisted) { LoadException = new InvalidDataException("加载失败") };
        var manager = new CopilotChatManager();
        CopilotChatSession initialSession = manager.SelectedSession;
        var application = new CodingChatApplication(manager, store);

        await application.InitializeAsync();

        Assert.HasCount(1, manager.ChatSessions);
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

    private static CopilotChatSession CreateSession(string title, string content, DateTimeOffset startedTime)
    {
        var session = new CopilotChatSession(Guid.NewGuid(), startedTime);
        session.SetTitle(title);
        session.AddMessageAsync(new CopilotChatMessage(ChatRole.User, content)).GetAwaiter().GetResult();
        return session;
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
            => Task.CompletedTask;
    }

    private sealed class ControllableRunner : ICodingChatRunner
    {
        private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
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