using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Logging;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class CodingChatApplication
{
    private readonly CopilotChatManager _chatManager;
    private readonly ICodingChatSessionStore _sessionStore;
    private readonly ICodingChatRunner? _chatRunner;
    private readonly CodingWorkspaceController? _workspaceController;
    private CancellationTokenSource? _activeRunCancellationTokenSource;
    private bool _isCompressionActive;
    private bool _isRunActive;

    public CodingChatApplication(CopilotChatManager chatManager, ICodingChatSessionStore sessionStore)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(sessionStore);
        _chatManager = chatManager;
        _sessionStore = sessionStore;
    }

    public CodingChatApplication(
        CopilotChatManager chatManager,
        ICodingChatSessionStore sessionStore,
        ICodingChatRunner chatRunner)
        : this(chatManager, sessionStore)
    {
        ArgumentNullException.ThrowIfNull(chatRunner);
        _chatRunner = chatRunner;
    }

    public CodingChatApplication(
        CopilotChatManager chatManager,
        ICodingChatSessionStore sessionStore,
        ICodingChatRunner chatRunner,
        CodingWorkspaceController workspaceController)
        : this(chatManager, sessionStore, chatRunner)
    {
        ArgumentNullException.ThrowIfNull(workspaceController);
        _workspaceController = workspaceController;
    }

    public event EventHandler? StateChanged;

    public ObservableCollection<CopilotChatSessionSummary> Sessions { get; } = [];

    public Guid SelectedSessionId => _chatManager.SelectedSession.SessionId;

    public bool CanChangeSession => !HasActiveOperation;

    public bool CanSend => _chatRunner is not null && !_isCompressionActive;

    public bool CanCompressConversation => !HasActiveOperation
        && _chatManager.SelectedSession.AgentSession is not null;

    public bool IsCompressionActive => _isCompressionActive;

    public bool IsRunActive => _isRunActive;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CopilotChatSessionSummary> summaries = await _sessionStore
            .ListSessionsAsync(cancellationToken);
        Sessions.Clear();
        foreach (CopilotChatSessionSummary summary in summaries)
        {
            Sessions.Add(summary);
        }

        if (summaries.Count == 0)
        {
            AddOrUpdateSummary(_chatManager.SelectedSession, insertAtTop: true);
            OnStateChanged();
            return;
        }

        CopilotChatSession initialSession = _chatManager.SelectedSession;
        foreach (CopilotChatSessionSummary summary in summaries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                CopilotChatSession restoredSession = await _sessionStore
                    .LoadSessionAsync(summary.SessionId, cancellationToken);
                _chatManager.AddSession(restoredSession, select: true);
                if (!ReferenceEquals(initialSession, restoredSession))
                {
                    _chatManager.RemoveSession(initialSession);
                }

                break;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }

        OnStateChanged();
    }

    public Task CreateNewSessionAsync(CancellationToken cancellationToken = default)
    {
        EnsureCanChangeSession();
        cancellationToken.ThrowIfCancellationRequested();
        AddOrUpdateSummary(_chatManager.SelectedSession, insertAtTop: false);
        _chatManager.CreateNewSession();
        AddOrUpdateSummary(_chatManager.SelectedSession, insertAtTop: true);
        OnStateChanged();
        return Task.CompletedTask;
    }

    public async Task OpenSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        EnsureCanChangeSession();
        if (_chatManager.SelectedSession.SessionId == sessionId)
        {
            return;
        }

        CopilotChatSession previousSession = _chatManager.SelectedSession;
        bool sessionAlreadyLoaded = _chatManager.ChatSessions.Any(session => session.SessionId == sessionId);
        try
        {
            CopilotChatSession targetSession;
            if (sessionAlreadyLoaded)
            {
                targetSession = _chatManager.ChatSessions.Single(session => session.SessionId == sessionId);
                _chatManager.SelectedSession = targetSession;
            }
            else
            {
                targetSession = await _sessionStore
                    .LoadSessionAsync(sessionId, cancellationToken);
                _chatManager.AddSession(targetSession, select: true);
            }

            AddOrUpdateSummary(targetSession, insertAtTop: false);
            OnStateChanged();
        }
        catch
        {
            _chatManager.SelectedSession = previousSession;
            OnStateChanged();
            throw;
        }
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        EnsureCanChangeSession();
        CopilotChatSession? session = _chatManager.ChatSessions.FirstOrDefault(item => item.SessionId == sessionId);
        await _sessionStore.DeleteSessionAsync(sessionId, cancellationToken);

        if (session is not null)
        {
            _chatManager.RemoveSession(session);
        }
        CopilotChatSessionSummary? summary = Sessions.FirstOrDefault(item => item.SessionId == sessionId);
        if (summary is not null)
        {
            Sessions.Remove(summary);
        }

        AddOrUpdateSummary(_chatManager.SelectedSession, insertAtTop: true);
        OnStateChanged();
    }

    public async Task SendMessageAsync(
        string prompt,
        bool enableAutomaticCompression = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("消息内容不能为空。", nameof(prompt));
        }

        await SendMessageAsync([new TextContent(prompt)], enableAutomaticCompression, cancellationToken);
    }

    public async Task SendMessageAsync(
        IReadOnlyList<AIContent> contents,
        bool enableAutomaticCompression = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);
        var runContents = new List<AIContent>(contents);
        if (runContents.Count == 0)
        {
            throw new ArgumentException("消息内容不能为空。", nameof(contents));
        }

        ICodingChatRunner chatRunner = _chatRunner
            ?? throw new InvalidOperationException("编程代理运行器尚未初始化。");
        if (_isCompressionActive)
        {
            throw new InvalidOperationException("对话压缩期间不能发送消息。");
        }

        if (_isRunActive)
        {
            await chatRunner.InjectMessageAsync(runContents, cancellationToken);
            return;
        }

        CancellationTokenSource runCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeRunCancellationTokenSource = runCancellationTokenSource;
        _isRunActive = true;
        OnStateChanged();
        CopilotChatSession session = _chatManager.SelectedSession;
        Exception? runException = null;
        try
        {
            CodingAgentRunResult runResult = await chatRunner
                .RunAsync(
                    runContents,
                    _workspaceController?.CommittedWorkspacePath,
                    enableAutomaticCompression,
                    runCancellationTokenSource.Token);
            await runResult.CompletionTask;
            if (ReferenceEquals(_activeRunCancellationTokenSource, runCancellationTokenSource))
            {
                _activeRunCancellationTokenSource = null;
                _isRunActive = false;
                OnStateChanged();
            }
        }
        catch (Exception exception)
        {
            runException = exception;
            throw;
        }
        finally
        {
            try
            {
                await _sessionStore
                    .SaveSessionAsync(session, CancellationToken.None);
                AddOrUpdateSummary(session, insertAtTop: true);
            }
            catch when (runException is not null)
            {
            }
            finally
            {
                if (ReferenceEquals(_activeRunCancellationTokenSource, runCancellationTokenSource))
                {
                    _activeRunCancellationTokenSource = null;
                    _isRunActive = false;
                }

                runCancellationTokenSource.Dispose();
                OnStateChanged();
            }
        }
    }

    public async Task RunLoopIterationAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("消息内容不能为空。", nameof(prompt));
        }

        while (true)
        {
            try
            {
                await SendMessageAsync(prompt, enableAutomaticCompression: true, cancellationToken);
                await CompressConversationAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    public async Task CompressConversationAsync(CancellationToken cancellationToken = default)
    {
        if (!CanCompressConversation)
        {
            throw new InvalidOperationException("当前会话没有可压缩的对话历史，或已有操作正在运行。");
        }

        CopilotChatSession session = _chatManager.SelectedSession;
        _isCompressionActive = true;
        OnStateChanged();
        try
        {
            await _chatManager
                .ReduceSessionAsync();
            await _sessionStore
                .SaveSessionAsync(session, CancellationToken.None);
            AddOrUpdateSummary(session, insertAtTop: true);
        }
        finally
        {
            _isCompressionActive = false;
            OnStateChanged();
        }
    }

    public void StopActiveRun()
    {
        _activeRunCancellationTokenSource?.Cancel();
    }

    private void AddOrUpdateSummary(CopilotChatSession session, bool insertAtTop)
    {
        CopilotChatSessionSummary? existing = Sessions.FirstOrDefault(item => item.SessionId == session.SessionId);
        var summary = new CopilotChatSessionSummary
        {
            SessionId = session.SessionId,
            Title = session.Title,
            StartedTime = session.StartedTime,
            MessageCount = session.ChatMessages.Count(message => !message.IsPresetInfo),
        };

        if (existing is not null)
        {
            int index = Sessions.IndexOf(existing);
            Sessions[index] = summary;
            if (insertAtTop && index > 0)
            {
                Sessions.Move(index, 0);
            }

            return;
        }

        if (insertAtTop)
        {
            Sessions.Insert(0, summary);
        }
        else
        {
            Sessions.Add(summary);
        }
    }

    private void EnsureCanChangeSession()
    {
        if (!CanChangeSession)
        {
            throw new InvalidOperationException("活动发送期间不能切换会话。");
        }
    }

    private bool HasActiveOperation => _isRunActive || _isCompressionActive;

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}