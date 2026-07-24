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

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class CodingChatApplication
{
    private readonly CopilotChatManager _chatManager;
    private readonly ICodingChatSessionStore _sessionStore;
    private readonly ICodingChatRunner? _chatRunner;
    private readonly CodingWorkspaceController? _workspaceController;
    private CancellationTokenSource? _activeRunCancellationTokenSource;
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

    public bool CanChangeSession => !_isRunActive;

    public bool CanSend => _chatRunner is not null && !_isRunActive;

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

    public async Task SendMessageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("消息内容不能为空。", nameof(prompt));
        }

        ICodingChatRunner chatRunner = _chatRunner
            ?? throw new InvalidOperationException("编程代理运行器尚未初始化。");
        if (_isRunActive)
        {
            throw new InvalidOperationException("已有活动发送正在运行。");
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
                .RunAsync(prompt, _workspaceController?.CommittedWorkspacePath, runCancellationTokenSource.Token);
            await runResult.CompletionTask;
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

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}