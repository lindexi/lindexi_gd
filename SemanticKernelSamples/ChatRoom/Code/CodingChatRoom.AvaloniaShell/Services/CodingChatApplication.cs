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

internal enum CodingChatOperationPhase
{
    Idle,
    Running,
    Compressing,
    Finalizing,
    WaitingToRetry,
}

internal sealed class CodingChatApplication
{
    private readonly CopilotChatManager _chatManager;
    private readonly ICodingChatSessionStore _sessionStore;
    private readonly ICodingChatRunner _chatRunner;
    private readonly CodingWorkspaceController _workspaceController;
    private readonly CodingAgent _codingAgent;
    private CancellationTokenSource? _activeOperationCancellationTokenSource;
    private volatile bool _isLoopIterationEnabled;
    private bool _isLoopActive;
    private CodingChatOperationPhase _operationPhase;

    public CodingChatApplication
    (
        CopilotChatManager chatManager,
        ICodingChatSessionStore sessionStore,
        ICodingChatRunner chatRunner,
        CodingWorkspaceController workspaceController,
        CodingAgent codingAgent
    )
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(sessionStore);
        ArgumentNullException.ThrowIfNull(chatRunner);
        ArgumentNullException.ThrowIfNull(workspaceController);
        ArgumentNullException.ThrowIfNull(codingAgent);
        _chatManager = chatManager;
        _sessionStore = sessionStore;
        _chatRunner = chatRunner;
        _workspaceController = workspaceController;
        _codingAgent = codingAgent;
        AddOrUpdateSummary(_chatManager.SelectedSession, insertAtTop: true);
    }

    public event EventHandler? StateChanged;

    public ObservableCollection<CopilotChatSessionSummary> Sessions { get; } = [];

    public Guid SelectedSessionId => _chatManager.SelectedSession.SessionId;

    public bool CanChangeSession => !HasActiveOperation;

    public bool CanSend => _operationPhase == CodingChatOperationPhase.Running
                           || (!_isLoopActive && _operationPhase == CodingChatOperationPhase.Idle);

    public bool CanCompressConversation => !HasActiveOperation
                                           && _chatManager.SelectedSession.AgentSession is not null;

    public bool IsCompressionActive => _operationPhase == CodingChatOperationPhase.Compressing;

    public bool IsLoopIterationEnabled
    {
        get => _isLoopIterationEnabled;
        set => _isLoopIterationEnabled = value;
    }

    public bool IsRunActive => _operationPhase == CodingChatOperationPhase.Running;

    public bool IsLoopActive => _isLoopActive;

    public bool IsFinalizing => _operationPhase == CodingChatOperationPhase.Finalizing;

    private readonly HashSet<Guid> _deletedSessionIds = [];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await foreach (CopilotChatSessionSummary summary in _sessionStore.EnumerateSessionsAsync(cancellationToken))
        {
            AddSessionSummaries([summary]);
        }
    }

    internal Task<IReadOnlyList<CopilotChatSessionSummary>> LoadSessionSummariesAsync
    (
        CancellationToken cancellationToken = default
    )
        => _sessionStore.ListSessionsAsync(cancellationToken);

    internal void AddSessionSummaries(IReadOnlyList<CopilotChatSessionSummary> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        foreach (CopilotChatSessionSummary summary in summaries)
        {
            if (!_deletedSessionIds.Contains(summary.SessionId)
                && Sessions.All(item => item.SessionId != summary.SessionId)
                && _chatManager.ChatSessions.All(session => session.SessionId != summary.SessionId))
            {
                Sessions.Add(summary);
            }
        }

    }

    public async Task RenameSessionAsync(Guid sessionId, string title, CancellationToken cancellationToken = default)
    {
        EnsureCanChangeSession();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        CopilotChatSession session = _chatManager.ChatSessions.FirstOrDefault(item => item.SessionId == sessionId)
            ?? await _sessionStore.LoadSessionAsync(sessionId, cancellationToken);
        string previousTitle = session.Title;
        try
        {
            session.SetTitle(title.Trim());
            await _sessionStore.SaveSessionAsync(session, cancellationToken);
            AddOrUpdateSummary(session, insertAtTop: false);
        }
        catch
        {
            session.SetTitle(previousTitle);
            throw;
        }
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
        string? previousWorkspacePath = _workspaceController.NextRunWorkspacePath;
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

            await _workspaceController
                .ChangeWorkspaceAsync(targetSession.WorkspacePath, cancellationToken);
            AddOrUpdateSummary(targetSession, insertAtTop: false);
            OnStateChanged();
        }
        catch
        {
            _chatManager.SelectedSession = previousSession;
            await _workspaceController
                .ChangeWorkspaceAsync(previousWorkspacePath, CancellationToken.None);
            OnStateChanged();
            throw;
        }
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        EnsureCanChangeSession();
        CopilotChatSession? session = _chatManager.ChatSessions.FirstOrDefault(item => item.SessionId == sessionId);
        await _sessionStore.DeleteSessionAsync(sessionId, cancellationToken);
        _deletedSessionIds.Add(sessionId);

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

    public async Task SendMessageAsync
    (
        string prompt,
        CodingChatRunOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("消息内容不能为空。", nameof(prompt));
        }

        await SendMessageAsync(
            [new TextContent(prompt)],
            options,
            cancellationToken);
    }

    public async Task SendMessageAsync
    (
        IReadOnlyList<AIContent> contents,
        CodingChatRunOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(contents);
        var runContents = new List<AIContent>(contents);
        if (runContents.Count == 0)
        {
            throw new ArgumentException("消息内容不能为空。", nameof(contents));
        }

        ICodingChatRunner chatRunner = _chatRunner;
        if (_operationPhase == CodingChatOperationPhase.Running)
        {
            await chatRunner.InjectMessageAsync(runContents, cancellationToken);
            return;
        }

        if (_operationPhase != CodingChatOperationPhase.Idle || _isLoopActive)
        {
            throw new InvalidOperationException("当前任务已有活动操作。");
        }

        using CancellationTokenSource operationCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperationCancellationTokenSource = operationCancellationTokenSource;
        try
        {
            await RunSingleMessageAsync(
                chatRunner,
                runContents,
                options ?? CodingChatRunOptions.Default,
                operationCancellationTokenSource.Token);
        }
        finally
        {
            if (ReferenceEquals(_activeOperationCancellationTokenSource, operationCancellationTokenSource))
            {
                _activeOperationCancellationTokenSource = null;
            }
        }
    }

    private async Task RunSingleMessageAsync(
        ICodingChatRunner chatRunner,
        IReadOnlyList<AIContent> runContents,
        CodingChatRunOptions options,
        CancellationToken cancellationToken)
    {
        SetOperationPhase(CodingChatOperationPhase.Running);
        CopilotChatSession session = _chatManager.SelectedSession;
        session.WorkspacePath = _workspaceController.NextRunWorkspacePath;
        Exception? runException = null;
        try
        {
            CodingAgentRunResult runResult = await chatRunner.RunAsync(
                runContents,
                _workspaceController.NextRunWorkspacePath,
                options,
                cancellationToken);
            await runResult.CompletionTask;
        }
        catch (Exception exception)
        {
            runException = exception;
            throw;
        }
        finally
        {
            SetOperationPhase(CodingChatOperationPhase.Finalizing);
            try
            {
                await _sessionStore.SaveSessionAsync(session, CancellationToken.None);
                AddOrUpdateSummary(session, insertAtTop: true);
            }
            catch when (runException is not null)
            {
            }
            finally
            {
                SetOperationPhase(CodingChatOperationPhase.Idle);
            }
        }
    }

    public async Task RunLoopIterationAsync(
        string prompt,
        CodingChatRunOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("消息内容不能为空。", nameof(prompt));
        }

        if (_activeOperationCancellationTokenSource is not null)
        {
            throw new InvalidOperationException("当前任务已有活动操作。");
        }

        using CancellationTokenSource operationCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperationCancellationTokenSource = operationCancellationTokenSource;
        _isLoopActive = true;
        OnStateChanged();
        try
        {
            while (IsLoopIterationEnabled)
            {
                try
                {
                    await RunSingleMessageAsync(
                        _chatRunner,
                        [new TextContent(prompt)],
                        options,
                        operationCancellationTokenSource.Token);
                    if (options.EnableAutomaticCompression)
                    {
                        await CompressConversationCoreAsync(operationCancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    if (!IsLoopIterationEnabled)
                    {
                        return;
                    }

                    try
                    {
                        SetOperationPhase(CodingChatOperationPhase.WaitingToRetry);
                        await Task.Delay(TimeSpan.FromSeconds(10), operationCancellationTokenSource.Token);
                        SetOperationPhase(CodingChatOperationPhase.Idle);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
        finally
        {
            if (ReferenceEquals(_activeOperationCancellationTokenSource, operationCancellationTokenSource))
            {
                _activeOperationCancellationTokenSource = null;
            }

            _isLoopActive = false;
            SetOperationPhase(CodingChatOperationPhase.Idle);
        }
    }

    public async Task CompressConversationAsync(CancellationToken cancellationToken = default)
    {
        if (!CanCompressConversation)
        {
            throw new InvalidOperationException("当前会话没有可压缩的对话历史，或已有操作正在运行。");
        }

        await CompressConversationCoreAsync(cancellationToken);
    }

    private async Task CompressConversationCoreAsync(CancellationToken cancellationToken)
    {
        CopilotChatSession session = _chatManager.SelectedSession;
        SetOperationPhase(CodingChatOperationPhase.Compressing);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _chatManager.ReduceSessionAsync();
            await _sessionStore.SaveSessionAsync(session, CancellationToken.None);
            AddOrUpdateSummary(session, insertAtTop: true);
        }
        finally
        {
            SetOperationPhase(CodingChatOperationPhase.Idle);
        }
    }

    public void StopActiveRun()
    {
        IsLoopIterationEnabled = false;
        _activeOperationCancellationTokenSource?.Cancel();
    }

    public Task<bool> StopLanguageServerAsync() => _codingAgent.StopLanguageServerAsync();

    private void AddOrUpdateSummary(CopilotChatSession session, bool insertAtTop)
    {
        CopilotChatSessionSummary? existing = Sessions.FirstOrDefault(item => item.SessionId == session.SessionId);
        var summary = new CopilotChatSessionSummary
        {
            SessionId = session.SessionId,
            Title = session.Title,
            WorkspacePath = session.WorkspacePath,
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

    private bool HasActiveOperation => _isLoopActive || _operationPhase != CodingChatOperationPhase.Idle;

    private void SetOperationPhase(CodingChatOperationPhase phase)
    {
        if (_operationPhase == phase) return;
        _operationPhase = phase;
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}