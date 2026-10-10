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

/// <summary>
/// 表示工作任务的完整业务操作状态，统一驱动界面属性和命令可用性。
/// 循环状态在单轮完成后仍保持活动，只有最外层操作完成收尾并清理取消源后才进入空闲。
/// </summary>
internal enum CodingWorkTaskOperationState
{
    /// <summary>没有活动操作，可以发送、压缩或切换会话。</summary>
    Idle,

    /// <summary>单次运行正在执行，允许向活动运行提交插话。</summary>
    Running,

    /// <summary>正在执行手动压缩，尚未进入保存收尾。</summary>
    Compressing,

    /// <summary>单次运行或手动压缩正在保存及收尾，操作尚未结束。</summary>
    Finalizing,

    /// <summary>循环中的当前轮正在执行，允许插话；取消循环勾选不立即终止当前轮。</summary>
    LoopRunning,

    /// <summary>循环正在执行轮末压缩，仍属于同一个活动循环操作。</summary>
    LoopCompressing,

    /// <summary>循环正在保存当前轮或压缩结果，尚未决定进入下一轮或结束循环。</summary>
    LoopFinalizing,

    /// <summary>循环执行失败后等待重试，可通过停止取消等待。</summary>
    LoopWaitingToRetry,
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
    private CodingWorkTaskOperationState _operationState;

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

    internal ICodingChatRunner CodingRunner => _chatRunner;

    public event EventHandler? StateChanged;

    public ObservableCollection<CopilotChatSessionSummary> Sessions { get; } = [];

    public Guid SelectedSessionId => _chatManager.SelectedSession.SessionId;

    public bool CanChangeSession => !HasActiveOperation;

    public bool CanSend => !HasActiveOperation || IsRunActive;

    public bool CanCompressConversation => !HasActiveOperation;

    public bool IsCompressionActive => _operationState is CodingWorkTaskOperationState.Compressing
        or CodingWorkTaskOperationState.LoopCompressing;

    public bool IsLoopIterationEnabled
    {
        get => _isLoopIterationEnabled;
        set => _isLoopIterationEnabled = value;
    }

    public bool IsRunActive => _operationState is CodingWorkTaskOperationState.Running
        or CodingWorkTaskOperationState.LoopRunning;

    public bool IsLoopActive => _operationState is CodingWorkTaskOperationState.LoopRunning
        or CodingWorkTaskOperationState.LoopCompressing
        or CodingWorkTaskOperationState.LoopFinalizing
        or CodingWorkTaskOperationState.LoopWaitingToRetry;

    public bool IsFinalizing => _operationState is CodingWorkTaskOperationState.Finalizing
        or CodingWorkTaskOperationState.LoopFinalizing;

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
            cancellationToken: cancellationToken
        );
    }

    public async Task SendMessageAsync
    (
        IReadOnlyList<AIContent> contents,
        CodingChatRunOptions? options = null,
        ICodingChatRunner? runner = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(contents);
        var runContents = new List<AIContent>(contents);
        if (runContents.Count == 0)
        {
            throw new ArgumentException("消息内容不能为空。", nameof(contents));
        }

        ICodingChatRunner chatRunner = runner ?? _chatRunner;
        if (IsRunActive)
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

        if (HasActiveOperation)
        {
            throw new InvalidOperationException("当前任务已有活动操作。");
        }

        using CancellationTokenSource operationCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperationCancellationTokenSource = operationCancellationTokenSource;
        UpdateOperationState(CodingWorkTaskOperationState.Running);
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
            UpdateOperationState(default);
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
        AdvanceOperationState(CodingWorkTaskOperationState.Running);
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
            ICodingAgentRunResult runResult = await chatRunner.RunAsync
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
                $"运行异常结束。异常类型={exception.GetType().FullName}；运行令牌取消={cancellationToken.IsCancellationRequested}；活动取消源存在={_activeOperationCancellationTokenSource is not null}；活动取消源已取消={_activeOperationCancellationTokenSource?.IsCancellationRequested == true}；状态={_operationState}。",
                exception
            );
            throw;
        }
        finally
        {
            AdvanceOperationState(CodingWorkTaskOperationState.Finalizing);
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

        }
    }

    public async Task RunLoopIterationAsync
    (
        string prompt,
        CodingChatRunOptions options,
        ICodingChatRunner? runner = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("消息内容不能为空。", nameof(prompt));
        }

        if (HasActiveOperation)
        {
            throw new InvalidOperationException("当前任务已有活动操作。");
        }

        using CancellationTokenSource operationCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperationCancellationTokenSource = operationCancellationTokenSource;
        ICodingChatRunner chatRunner = runner ?? _chatRunner;
        UpdateOperationState(CodingWorkTaskOperationState.LoopRunning);
        try
        {
            while (IsLoopIterationEnabled)
            {
                try
                {
                    await RunSingleMessageAsync
                    (
                        chatRunner,
                        [new TextContent(prompt)],
                        options,
                        operationCancellationTokenSource.Token
                    );
                    if (options.EnableAutomaticCompression)
                    {
                        await CompressConversationCoreAsync(chatRunner, operationCancellationTokenSource.Token);
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
                        UpdateOperationState(CodingWorkTaskOperationState.LoopWaitingToRetry);
                        await Task.Delay(TimeSpan.FromSeconds(10), operationCancellationTokenSource.Token);
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

            UpdateOperationState(default);
        }
    }

    public Task<bool> CompressConversationAsync(CancellationToken cancellationToken = default)
        => CompressConversationAsync(null, cancellationToken: cancellationToken);

    public async Task<bool> CompressConversationAsync(string? compressionRequest,
        ICodingChatRunner? runner = null, CancellationToken cancellationToken = default)
    {
        if (!CanCompressConversation)
        {
            throw new InvalidOperationException("当前会话没有可压缩的对话历史，或已有操作正在运行。");
        }

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperationCancellationTokenSource = operationCancellation;
        UpdateOperationState(CodingWorkTaskOperationState.Compressing);
        try
        {
            return await CompressConversationCoreAsync(runner ?? _chatRunner, operationCancellation.Token, compressionRequest);
        }
        finally
        {
            _activeOperationCancellationTokenSource = null;
            UpdateOperationState(default);
        }
    }

    private async Task<bool> CompressConversationCoreAsync(ICodingChatRunner runner,
        CancellationToken cancellationToken, string? compressionRequest = null)
    {
        CopilotChatSession session = _chatManager.SelectedSession;
        AdvanceOperationState(CodingWorkTaskOperationState.Compressing);
        cancellationToken.ThrowIfCancellationRequested();
        if (!await runner.TryCompactConversationAsync(session, compressionRequest, cancellationToken)) return false;
        AdvanceOperationState(CodingWorkTaskOperationState.Finalizing);
        await _sessionStore.SaveSessionAsync(session, CancellationToken.None);
        AddOrUpdateSummary(session, insertAtTop: true);
        return true;
    }

    public async Task StopActiveRunByUserAsync()
    {
        CopilotChatSession session = _chatManager.SelectedSession;
        await _chatManager.ChatLogger.LogDiagnosticAsync
        (
            session.SessionId,
            "停止",
            $"用户请求停止运行。状态={_operationState}；循环活动={IsLoopActive}；活动取消源存在={_activeOperationCancellationTokenSource is not null}；活动取消源已取消={_activeOperationCancellationTokenSource?.IsCancellationRequested == true}。"
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

    private bool HasActiveOperation => _operationState != CodingWorkTaskOperationState.Idle;

    private void AdvanceOperationState(CodingWorkTaskOperationState state)
    {
        UpdateOperationState(IsLoopActive ? state switch
        {
            CodingWorkTaskOperationState.Running => CodingWorkTaskOperationState.LoopRunning,
            CodingWorkTaskOperationState.Compressing => CodingWorkTaskOperationState.LoopCompressing,
            CodingWorkTaskOperationState.Finalizing => CodingWorkTaskOperationState.LoopFinalizing,
            _ => state,
        } : state);
    }

    private void UpdateOperationState(CodingWorkTaskOperationState state)
    {
        if (_operationState == state) return;
        _operationState = state;
        OnStateChanged();
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}