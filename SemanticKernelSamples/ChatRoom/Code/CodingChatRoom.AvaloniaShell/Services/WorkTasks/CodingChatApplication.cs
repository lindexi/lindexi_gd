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

internal enum CodingWorkTaskOperationPhase
{
    Idle,
    Running,
    Compressing,
    Finalizing,
    WaitingToRetry,
}

internal sealed class CodingWorkTaskController
{
    private readonly CopilotChatManager _chatManager;
    private readonly ICodingChatSessionStore _sessionStore;
    private readonly ICodingChatRunner _chatRunner;
    private readonly CodingWorkspaceController _workspaceController;
    private readonly CodingAgent _codingAgent;
    private Guid? _workTaskId;
    private string? _workTaskName;
    private CancellationTokenSource? _activeOperationCancellationTokenSource;
    private volatile bool _isLoopIterationEnabled;
    private bool _isLoopActive;
    private CodingWorkTaskOperationPhase _operationPhase;

    public CodingWorkTaskController
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

    public bool CanSend => _operationPhase == CodingWorkTaskOperationPhase.Running
                           || (!_isLoopActive && _operationPhase == CodingWorkTaskOperationPhase.Idle);

    public bool CanCompressConversation => !HasActiveOperation
                                           && _chatManager.SelectedSession.AgentSession is not null;

    public bool IsCompressionActive => _operationPhase == CodingWorkTaskOperationPhase.Compressing;

    public bool IsLoopIterationEnabled
    {
        get => _isLoopIterationEnabled;
        set => _isLoopIterationEnabled = value;
    }

    public bool IsRunActive => _operationPhase == CodingWorkTaskOperationPhase.Running;

    public bool IsLoopActive => _isLoopActive;

    public bool IsFinalizing => _operationPhase == CodingWorkTaskOperationPhase.Finalizing;

    internal void SetWorkTask(Guid workTaskId, string workTaskName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workTaskName);
        _workTaskId = workTaskId;
        _workTaskName = workTaskName.Trim();
        ApplyWorkTaskMetadata(_chatManager.SelectedSession);
        AddOrUpdateSummary(_chatManager.SelectedSession, insertAtTop: false);
    }

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
        ApplyWorkTaskMetadata(_chatManager.SelectedSession);
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

            ApplyWorkTaskMetadata(targetSession);
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

        await SendMessageAsync
        (
            [new TextContent(prompt)],
            options,
            cancellationToken
        );
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
        if (_operationPhase == CodingWorkTaskOperationPhase.Running)
        {
            Guid sessionId = _chatManager.SelectedSession.SessionId;
            await _chatManager.ChatLogger.LogDiagnosticAsync
            (
                sessionId,
                "运行",
                $"开始提交插话。调用方取消={cancellationToken.IsCancellationRequested}。"
            );
            try
            {
                await chatRunner.InjectMessageAsync(runContents, cancellationToken);
                await _chatManager.ChatLogger.LogDiagnosticAsync(sessionId, "运行", "插话提交完成。");
            }
            catch (Exception exception)
            {
                await _chatManager.ChatLogger.LogDiagnosticAsync
                (
                    sessionId,
                    "运行",
                    $"插话提交失败。调用方取消={cancellationToken.IsCancellationRequested}。",
                    exception
                );
                throw;
            }

            return;
        }

        if (_operationPhase != CodingWorkTaskOperationPhase.Idle || _isLoopActive)
        {
            throw new InvalidOperationException("当前任务已有活动操作。");
        }

        using CancellationTokenSource operationCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperationCancellationTokenSource = operationCancellationTokenSource;
        try
        {
            await RunSingleMessageAsync
            (
                chatRunner,
                runContents,
                options ?? CodingChatRunOptions.Default,
                operationCancellationTokenSource.Token
            );
        }
        finally
        {
            if (ReferenceEquals(_activeOperationCancellationTokenSource, operationCancellationTokenSource))
            {
                _activeOperationCancellationTokenSource = null;
            }
        }
    }

    private async Task RunSingleMessageAsync
    (
        ICodingChatRunner chatRunner,
        IReadOnlyList<AIContent> runContents,
        CodingChatRunOptions options,
        CancellationToken cancellationToken
    )
    {
        SetOperationPhase(CodingWorkTaskOperationPhase.Running);
        CopilotChatSession session = _chatManager.SelectedSession;
        session.WorkspacePath = _workspaceController.NextRunWorkspacePath;
        ApplyWorkTaskMetadata(session);
        await _chatManager.ChatLogger.LogDiagnosticAsync
        (
            session.SessionId,
            "运行",
            $"运行开始。工作任务={session.WorkTaskName ?? "未设置"}；工作任务Id={session.WorkTaskId?.ToString() ?? "未设置"}；工作路径={session.WorkspacePath ?? "未设置"}；自动压缩={options.EnableAutomaticCompression}；dotnet run={options.EnableDotNetRun}；思考强度={options.ReasoningEffort?.ToString() ?? "默认"}；调用方取消={cancellationToken.IsCancellationRequested}。"
        );
        Exception? runException = null;
        try
        {
            CodingAgentRunResult runResult = await chatRunner.RunAsync
            (
                runContents,
                _workspaceController.NextRunWorkspacePath,
                options,
                cancellationToken
            );
            await runResult.CompletionTask;
            await _chatManager.ChatLogger.LogDiagnosticAsync(session.SessionId, "运行", "运行正常完成。");
        }
        catch (Exception exception)
        {
            runException = exception;
            await _chatManager.ChatLogger.LogDiagnosticAsync
            (
                session.SessionId,
                "运行",
                $"运行异常结束。异常类型={exception.GetType().FullName}；运行令牌取消={cancellationToken.IsCancellationRequested}；活动取消源存在={_activeOperationCancellationTokenSource is not null}；活动取消源已取消={_activeOperationCancellationTokenSource?.IsCancellationRequested == true}；阶段={_operationPhase}。",
                exception
            );
            throw;
        }
        finally
        {
            SetOperationPhase(CodingWorkTaskOperationPhase.Finalizing);
            try
            {
                await _chatManager.ChatLogger.LogDiagnosticAsync(session.SessionId, "会话保存", "开始保存运行后的会话。");
                await _sessionStore.SaveSessionAsync(session, CancellationToken.None);
                AddOrUpdateSummary(session, insertAtTop: true);
                await _chatManager.ChatLogger.LogDiagnosticAsync(session.SessionId, "会话保存", "运行后的会话保存完成。");
            }
            catch (Exception saveException) when (runException is not null)
            {
                await _chatManager.ChatLogger.LogDiagnosticAsync
                (
                    session.SessionId,
                    "会话保存",
                    "运行已经异常结束，随后保存会话时再次失败。将继续传播原始运行异常。",
                    saveException
                );
            }
            finally
            {
                SetOperationPhase(CodingWorkTaskOperationPhase.Idle);
                await _chatManager.ChatLogger.LogDiagnosticAsync(session.SessionId, "运行", "运行状态已恢复为空闲。");
            }
        }
    }

    public async Task RunLoopIterationAsync
    (
        string prompt,
        CodingChatRunOptions options,
        CancellationToken cancellationToken = default
    )
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
                    await RunSingleMessageAsync
                    (
                        _chatRunner,
                        [new TextContent(prompt)],
                        options,
                        operationCancellationTokenSource.Token
                    );
                    if (options.EnableAutomaticCompression)
                    {
                        await CompressConversationCoreAsync(operationCancellationTokenSource.Token);
                    }
                    else if (IsLoopIterationEnabled)
                    {
                        await _chatManager.ReduceAgentSessionOnlyAsync
                        (
                            chatReducer: LoopIterationChatReducer.Instance,
                            cancellationToken: operationCancellationTokenSource.Token
                        );
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
                        SetOperationPhase(CodingWorkTaskOperationPhase.WaitingToRetry);
                        await Task.Delay(TimeSpan.FromSeconds(10), operationCancellationTokenSource.Token);
                        SetOperationPhase(CodingWorkTaskOperationPhase.Idle);
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
            SetOperationPhase(CodingWorkTaskOperationPhase.Idle);
        }
    }

    public Task CompressConversationAsync(CancellationToken cancellationToken = default)
        => CompressConversationAsync(null, cancellationToken);

    public async Task CompressConversationAsync(string? compressionRequest, CancellationToken cancellationToken = default)
    {
        if (!CanCompressConversation)
        {
            throw new InvalidOperationException("当前会话没有可压缩的对话历史，或已有操作正在运行。");
        }

        await CompressConversationCoreAsync(cancellationToken, compressionRequest);
    }

    private async Task CompressConversationCoreAsync(CancellationToken cancellationToken, string? compressionRequest = null)
    {
        CopilotChatSession session = _chatManager.SelectedSession;
        SetOperationPhase(CodingWorkTaskOperationPhase.Compressing);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _chatManager.ReduceSessionAsync(
                chatReducer: null,
                requestText: compressionRequest,
                additionalPrompt: compressionRequest,
                cancellationToken: cancellationToken);
            await _sessionStore.SaveSessionAsync(session, CancellationToken.None);
            AddOrUpdateSummary(session, insertAtTop: true);
        }
        finally
        {
            SetOperationPhase(CodingWorkTaskOperationPhase.Idle);
        }
    }

    public async Task StopActiveRunByUserAsync()
    {
        CopilotChatSession session = _chatManager.SelectedSession;
        await _chatManager.ChatLogger.LogDiagnosticAsync
        (
            session.SessionId,
            "停止",
            $"用户请求停止运行。阶段={_operationPhase}；循环活动={_isLoopActive}；活动取消源存在={_activeOperationCancellationTokenSource is not null}；活动取消源已取消={_activeOperationCancellationTokenSource?.IsCancellationRequested == true}。"
        );
        StopActiveRun();
    }

    public void StopActiveRun()
    {
        IsLoopIterationEnabled = false;
        _activeOperationCancellationTokenSource?.Cancel();
    }

    public Task<bool> StopLanguageServerAsync() => _codingAgent.StopLanguageServerAsync();

    private void ApplyWorkTaskMetadata(CopilotChatSession session)
    {
        if (_workTaskId is not Guid workTaskId || string.IsNullOrWhiteSpace(_workTaskName))
        {
            return;
        }

        session.WorkTaskId = workTaskId;
        session.WorkTaskName = _workTaskName;
    }

    private void AddOrUpdateSummary(CopilotChatSession session, bool insertAtTop)
    {
        CopilotChatSessionSummary? existing = Sessions.FirstOrDefault(item => item.SessionId == session.SessionId);
        var summary = new CopilotChatSessionSummary
        {
            SessionId = session.SessionId,
            Title = session.Title,
            WorkspacePath = session.WorkspacePath,
            WorkTaskId = session.WorkTaskId,
            WorkTaskName = session.WorkTaskName,
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

    private bool HasActiveOperation => _isLoopActive || _operationPhase != CodingWorkTaskOperationPhase.Idle;

    private void SetOperationPhase(CodingWorkTaskOperationPhase phase)
    {
        if (_operationPhase == phase) return;
        _operationPhase = phase;
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}