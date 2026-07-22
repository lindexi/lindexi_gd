using AgentLib.ChatRoom.Domain;
using AgentLib.ChatRoom.Runtime;

using System.Collections.ObjectModel;
using System.Threading.Channels;

namespace AgentLib.ChatRoom.Coordination;

/// <summary>
/// 聊天室命令执行结果。
/// </summary>
public enum ChatRoomCommandOutcome
{
    Applied,
    NoOp,
    Rejected,
    Stale,
}

/// <summary>
/// 尝试关闭房间的结果。
/// </summary>
public enum ChatRoomCloseOutcome
{
    Closed,
    AlreadyClosed,
    Stuck,
}

/// <summary>
/// 房间关闭结果。
/// </summary>
public sealed record ChatRoomCloseResult(ChatRoomCloseOutcome Outcome, ChatRoomState State);

/// <summary>
/// 聊天室命令回执。
/// </summary>
public sealed record ChatRoomCommandReceipt
{
    internal ChatRoomCommandReceipt(
        Guid commandId,
        ChatRoomCommandOutcome outcome,
        ChatRoomState state,
        string? detail = null,
        Guid? executionId = null)
    {
        CommandId = commandId;
        Outcome = outcome;
        State = state;
        Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
        ExecutionId = executionId;
    }

    public Guid CommandId { get; }

    public ChatRoomCommandOutcome Outcome { get; }

    public ChatRoomState State { get; }

    public string? Detail { get; }

    public Guid? ExecutionId { get; }
}

/// <summary>
/// 原子取得的房间快照与后续事件订阅。
/// </summary>
public sealed class ChatRoomCoordinatorSubscription : IAsyncDisposable
{
    private readonly ChatRoomCoordinator _owner;
    private readonly Channel<ChatRoomChange> _channel;
    private readonly Channel<ChatRoomExecutionEvent> _executionEvents;
    private int _isDisposed;

    internal ChatRoomCoordinatorSubscription(
        ChatRoomCoordinator owner,
        ChatRoomSnapshot initialSnapshot,
        long nextEventSequence)
    {
        _owner = owner;
        InitialSnapshot = initialSnapshot;
        NextEventSequence = nextEventSequence;
        _channel = Channel.CreateUnbounded<ChatRoomChange>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
        _executionEvents = Channel.CreateUnbounded<ChatRoomExecutionEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
    }

    /// <summary>
    /// 订阅建立时的完整一致快照。
    /// </summary>
    public ChatRoomSnapshot InitialSnapshot { get; }

    /// <summary>
    /// 后续第一条事件应使用的序号。
    /// </summary>
    public long NextEventSequence { get; }

    /// <summary>
    /// 读取订阅建立后的状态变化。
    /// </summary>
    public IAsyncEnumerable<ChatRoomChange> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// 读取流式增量和审批请求等瞬态执行事件。
    /// </summary>
    public IAsyncEnumerable<ChatRoomExecutionEvent> ReadExecutionEventsAsync(
        CancellationToken cancellationToken = default) =>
        _executionEvents.Reader.ReadAllAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
        {
            _owner.RemoveSubscription(this);
        }

        return default;
    }

    internal void Publish(ChatRoomChange change)
    {
        _channel.Writer.TryWrite(change);
    }

    internal void PublishExecutionEvent(ChatRoomExecutionEvent executionEvent)
    {
        _executionEvents.Writer.TryWrite(executionEvent);
    }

    internal void Complete(Exception? error = null)
    {
        _channel.Writer.TryComplete(error);
        _executionEvents.Writer.TryComplete(error);
    }
}

/// <summary>
/// 以单写者命令循环持有聊天室领域状态，并在循环外运行角色模型调用。
/// </summary>
public sealed class ChatRoomCoordinator : IAsyncDisposable
{
    private readonly object _stateSync = new();
    private readonly object _executionSync = new();
    private readonly object _backgroundSync = new();
    private readonly Channel<CommandEnvelope> _commands;
    private readonly ChatRoomRoleRuntimeRegistry _runtimeRegistry;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<Guid, ChatRoomCommandReceipt> _commandReceipts = [];
    private readonly Dictionary<ChatRoomRoleIdentity, ChatRoomRoleCheckpoint> _checkpoints = [];
    private readonly Dictionary<(Guid ExecutionId, string ApprovalId), PendingApproval> _pendingApprovals = [];
    private readonly List<ChatRoomCoordinatorSubscription> _subscriptions = [];
    private readonly HashSet<Task> _backgroundExecutions = [];
    private readonly LinkedList<string> _autoLoopQueue = [];
    private readonly Dictionary<string, int> _autoLoopTurnsByRole = new(StringComparer.Ordinal);
    private readonly Task _commandLoop;
    private readonly object _disposeSync = new();
    private ChatRoomState _state;
    private CancellationTokenSource? _activeExecutionCancellation;
    private string? _workspacePath;
    private string? _lastAutoLoopRoleId;
    private long _nextEventSequence = 1;
    private Task? _disposeTask;
    private int _isDisposed;

    /// <summary>
    /// 创建单写者聊天室协调器。
    /// </summary>
    public ChatRoomCoordinator(
        ChatRoomSnapshot initialSnapshot,
        ChatRoomRoleRuntimeRegistry runtimeRegistry,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        ArgumentNullException.ThrowIfNull(runtimeRegistry);
        if (initialSnapshot.State.CurrentExecution is not null)
        {
            throw new ArgumentException("协调器只能从不包含活动执行的快照启动。", nameof(initialSnapshot));
        }
        if (initialSnapshot.State.LifecycleStatus != ChatRoomLifecycleStatus.Open)
        {
            throw new ArgumentException("协调器只能从 Open 状态启动。", nameof(initialSnapshot));
        }

        ChatRoomSnapshot isolatedSnapshot = initialSnapshot.DeepClone();
        _state = isolatedSnapshot.State;
        foreach (ChatRoomRoleCheckpoint checkpoint in isolatedSnapshot.RoleCheckpoints)
        {
            _checkpoints.Add(checkpoint.RoleIdentity, checkpoint);
        }

        _runtimeRegistry = runtimeRegistry;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _commands = Channel.CreateUnbounded<CommandEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
        _commandLoop = ProcessCommandsAsync();
    }

    /// <summary>
    /// 获取当前不可变状态。
    /// </summary>
    public ChatRoomState State
    {
        get
        {
            lock (_stateSync)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// 获取当前瞬态工作区路径。
    /// </summary>
    public string? WorkspacePath
    {
        get
        {
            lock (_stateSync)
            {
                return _workspacePath;
            }
        }
    }

    /// <summary>
    /// 获取当前完整一致快照。
    /// </summary>
    public ChatRoomSnapshot GetSnapshot()
    {
        lock (_stateSync)
        {
            return CreateSnapshotLocked();
        }
    }

    /// <summary>
    /// 原子取得当前快照与后续事件位置。
    /// </summary>
    public ChatRoomCoordinatorSubscription Subscribe()
    {
        ThrowIfDisposed();
        lock (_stateSync)
        {
            ThrowIfDisposed();
            var subscription = new ChatRoomCoordinatorSubscription(
                this,
                CreateSnapshotLocked(),
                _nextEventSequence);
            _subscriptions.Add(subscription);
            return subscription;
        }
    }

    /// <summary>
    /// 将命令加入单写者队列并等待回执。
    /// 命令一旦入队，即使调用方停止等待，协调器仍会完成该命令。
    /// </summary>
    public async Task<ChatRoomCommandReceipt> ExecuteAsync(
        ChatRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ThrowIfDisposed();
        var envelope = new CommandEnvelope(command);
        await _commands.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
        return await envelope.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 请求关闭并等待活动执行在期限内真实终止。
    /// </summary>
    public async Task<ChatRoomCloseResult> TryCloseAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }
        if (State.LifecycleStatus == ChatRoomLifecycleStatus.Closed)
        {
            return new ChatRoomCloseResult(ChatRoomCloseOutcome.AlreadyClosed, State);
        }

        await ExecuteAsync(new CloseRoomCommand(Guid.NewGuid()), cancellationToken).ConfigureAwait(false);
        if (State.LifecycleStatus == ChatRoomLifecycleStatus.Closed)
        {
            return new ChatRoomCloseResult(ChatRoomCloseOutcome.Closed, State);
        }

        Guid executionId = State.CurrentExecution?.ExecutionId
            ?? throw new InvalidOperationException("Closing 状态缺少活动执行。");
        try
        {
            await WaitForStateAsync(
                state => state.LifecycleStatus == ChatRoomLifecycleStatus.Closed,
                timeout,
                cancellationToken).ConfigureAwait(false);
            return new ChatRoomCloseResult(ChatRoomCloseOutcome.Closed, State);
        }
        catch (TimeoutException)
        {
            await ExecuteAsync(new MarkExecutionStuckCommand(Guid.NewGuid(), executionId), cancellationToken)
                .ConfigureAwait(false);
            return new ChatRoomCloseResult(ChatRoomCloseOutcome.Stuck, State);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            _disposeTask ??= DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    private void TransitionClosedAfterExecution(Guid executionId, string roleId)
    {
        ChatRoomState closed = CreateState(
            revision: _state.Revision + 1,
            currentExecution: null,
            preserveCurrentExecution: false,
            lifecycleStatus: ChatRoomLifecycleStatus.Closed,
            autoLoop: CreateIdleAutoLoopState());
        PublishState(
            closed,
            ChatRoomChangeKind.LifecycleChanged,
            executionId: executionId,
            roleId: roleId,
            detail: "活动执行已终止，房间已关闭。");
    }

    internal void RemoveSubscription(ChatRoomCoordinatorSubscription subscription)
    {
        lock (_stateSync)
        {
            _subscriptions.Remove(subscription);
        }
        subscription.Complete();
    }

    private async Task ProcessCommandsAsync()
    {
        Exception? completionError = null;
        try
        {
            await foreach (CommandEnvelope envelope in _commands.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                try
                {
                    ChatRoomCommandReceipt receipt = await ProcessCommandAsync(envelope.Command).ConfigureAwait(false);
                    envelope.Completion.TrySetResult(receipt);
                }
                catch (Exception ex)
                {
                    envelope.Completion.TrySetException(ex);
                }
            }
        }
        catch (Exception ex)
        {
            completionError = ex;
            throw;
        }
        finally
        {
            while (_commands.Reader.TryRead(out CommandEnvelope? envelope))
            {
                envelope.Completion.TrySetException(
                    completionError ?? new ObjectDisposedException(nameof(ChatRoomCoordinator)));
            }
        }
    }

    private async Task<ChatRoomCommandReceipt> ProcessCommandAsync(ChatRoomCommand command)
    {
        if (_commandReceipts.TryGetValue(command.CommandId, out ChatRoomCommandReceipt? existingReceipt))
        {
            return existingReceipt;
        }

        ChatRoomCommandReceipt receipt = command switch
        {
            RenameRoomCommand rename => HandleRename(rename),
            AppendHumanMessageCommand append => HandleAppendHumanMessage(append),
            AddRoleCommand add => await HandleAddRoleAsync(add).ConfigureAwait(false),
            UpdateRoleCommand update => await HandleUpdateRoleAsync(update).ConfigureAwait(false),
            RemoveRoleCommand remove => await HandleRemoveRoleAsync(remove).ConfigureAwait(false),
            ChangeWorkspaceCommand workspace => await HandleChangeWorkspaceAsync(workspace).ConfigureAwait(false),
            StartRoleExecutionCommand start => HandleStartExecution(start),
            StartAutoLoopCommand startAutoLoop => HandleStartAutoLoop(startAutoLoop),
            StopAutoLoopCommand stopAutoLoop => HandleStopAutoLoop(stopAutoLoop),
            RespondToApprovalCommand response => HandleApprovalResponse(response),
            CompleteRoleExecutionCommand complete => HandleCompleteExecution(complete),
            FailRoleExecutionCommand fail => HandleFailExecution(fail),
            PublishStreamDeltaCommand delta => HandleStreamDelta(delta),
            RequestApprovalCommand approval => HandleApprovalRequest(approval),
            StopRoomCommand stop => HandleStop(stop),
            CloseRoomCommand close => HandleClose(close),
            ForceAbandonRoomCommand abandon => HandleForceAbandon(abandon),
            MarkExecutionStuckCommand stuck => HandleExecutionStuck(stuck),
            _ => throw new NotSupportedException($"不支持的聊天室命令：{command.GetType().Name}。"),
        };
        _commandReceipts.Add(command.CommandId, receipt);
        return receipt;
    }

    private ChatRoomCommandReceipt HandleRename(RenameRoomCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }
        if (string.Equals(_state.Title, command.Title, StringComparison.Ordinal))
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }

        ChatRoomState next = CreateState(
            title: command.Title,
            lastActivityAt: Later(_state.LastActivityAt, _utcNow()),
            revision: _state.Revision + 1,
            persistenceHealth: ChatRoomPersistenceHealth.Dirty,
            lastPersistenceError: null);
        PublishState(next, ChatRoomChangeKind.RoomUpdated, detail: "房间标题已更新。");
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private ChatRoomCommandReceipt HandleAppendHumanMessage(AppendHumanMessageCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }

        DateTimeOffset now = _utcNow();
        var message = new ChatRoomMessage(
            _state.NextMessageSequence,
            Guid.NewGuid(),
            ChatRoomMessageKind.Human,
            command.Content,
            now,
            command.HumanRoleId,
            command.HumanRoleName,
            ParseMentions(command.Content, _state.Roles));
        ChatRoomMessage[] messages = [.. _state.Messages, message];
        ChatRoomState next = CreateState(
            lastActivityAt: Later(_state.LastActivityAt, now),
            revision: _state.Revision + 1,
            nextMessageSequence: _state.NextMessageSequence + 1,
            messages: messages,
            persistenceHealth: ChatRoomPersistenceHealth.Dirty,
            lastPersistenceError: null);
        PublishState(next, ChatRoomChangeKind.MessageCommitted, messageId: message.MessageId);
        if (_state.AutoLoop.Status == ChatRoomAutoLoopStatus.Running)
        {
            EnqueueMentionedRoles(command.Content);
            EnqueueDefaultRoles();
            TryStartNextAutoLoopRole();
        }
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private async Task<ChatRoomCommandReceipt> HandleAddRoleAsync(AddRoleCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }
        if (_state.Roles.Any(role => string.Equals(
                role.Identity.RoleId,
                command.Definition.Identity.RoleId,
                StringComparison.Ordinal)))
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色标识已存在。");
        }
        if (_state.Roles.Any(role => string.Equals(role.RoleName, command.Definition.RoleName, StringComparison.Ordinal)))
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色名称已存在。");
        }

        if (!command.Definition.IsHuman)
        {
            await _runtimeRegistry.AddAsync(command.Definition).ConfigureAwait(false);
        }

        ChatRoomRoleDefinition[] roles = [.. _state.Roles, command.Definition];
        ChatRoomState next = CreateState(
            lastActivityAt: Later(_state.LastActivityAt, _utcNow()),
            revision: _state.Revision + 1,
            roles: roles,
            persistenceHealth: ChatRoomPersistenceHealth.Dirty,
            lastPersistenceError: null);
        PublishState(next, ChatRoomChangeKind.RolesChanged, roleId: command.Definition.Identity.RoleId);
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private async Task<ChatRoomCommandReceipt> HandleUpdateRoleAsync(UpdateRoleCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }
        int index = FindRoleIndex(command.Definition.Identity.RoleId);
        if (index < 0)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色不存在。");
        }
        ChatRoomRoleDefinition previous = _state.Roles[index];
        if (_state.CurrentExecution?.RoleIdentity == previous.Identity)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色正在执行，不能更新。");
        }
        if (previous == command.Definition)
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }
        if (command.Definition.RuntimeVersion <= previous.RuntimeVersion)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色更新必须递增 RuntimeVersion。");
        }
        if (_state.Roles.Where((_, roleIndex) => roleIndex != index).Any(role =>
                string.Equals(role.RoleName, command.Definition.RoleName, StringComparison.Ordinal)))
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色名称已存在。");
        }

        if (previous.IsHuman && !command.Definition.IsHuman)
        {
            await _runtimeRegistry.AddAsync(command.Definition).ConfigureAwait(false);
        }
        else if (!previous.IsHuman && command.Definition.IsHuman)
        {
            await _runtimeRegistry.RemoveAsync(previous.Identity.RoleId).ConfigureAwait(false);
        }
        else if (!previous.IsHuman)
        {
            await _runtimeRegistry.ReplaceAsync(command.Definition).ConfigureAwait(false);
        }

        ChatRoomRoleDefinition[] roles = _state.Roles.ToArray();
        roles[index] = command.Definition;
        var consumed = new Dictionary<string, long>(_state.ConsumedThroughSequenceByRole, StringComparer.Ordinal);
        lock (_stateSync)
        {
            _checkpoints.Remove(previous.Identity);
        }
        consumed[command.Definition.Identity.RoleId] = 0;
        ChatRoomState next = CreateState(
            lastActivityAt: Later(_state.LastActivityAt, _utcNow()),
            revision: _state.Revision + 1,
            roles: roles,
            consumedThroughSequenceByRole: consumed,
            persistenceHealth: ChatRoomPersistenceHealth.Dirty,
            lastPersistenceError: null);
        PublishState(next, ChatRoomChangeKind.RolesChanged, roleId: command.Definition.Identity.RoleId);
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private async Task<ChatRoomCommandReceipt> HandleRemoveRoleAsync(RemoveRoleCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }
        int index = FindRoleIndex(command.RoleId);
        if (index < 0)
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }
        ChatRoomRoleDefinition role = _state.Roles[index];
        if (_state.CurrentExecution?.RoleIdentity == role.Identity)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色正在执行，不能删除。");
        }

        if (!role.IsHuman)
        {
            await _runtimeRegistry.RemoveAsync(role.Identity.RoleId).ConfigureAwait(false);
        }
        lock (_stateSync)
        {
            _checkpoints.Remove(role.Identity);
        }
        ChatRoomRoleDefinition[] roles = _state.Roles.Where((_, roleIndex) => roleIndex != index).ToArray();
        var consumed = new Dictionary<string, long>(_state.ConsumedThroughSequenceByRole, StringComparer.Ordinal);
        consumed.Remove(role.Identity.RoleId);
        ChatRoomState next = CreateState(
            lastActivityAt: Later(_state.LastActivityAt, _utcNow()),
            revision: _state.Revision + 1,
            roles: roles,
            consumedThroughSequenceByRole: consumed,
            persistenceHealth: ChatRoomPersistenceHealth.Dirty,
            lastPersistenceError: null);
        PublishState(next, ChatRoomChangeKind.RolesChanged, roleId: role.Identity.RoleId);
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private async Task<ChatRoomCommandReceipt> HandleChangeWorkspaceAsync(ChangeWorkspaceCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }
        if (AreSameWorkspace(_workspacePath, command.WorkspacePath))
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }
        if (_state.CurrentExecution is not null)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色执行期间不能切换工作区。");
        }

        var leases = new List<ChatRoomRoleRuntimeLease>();
        var transactions = new List<IChatRoomRoleWorkspaceTransaction>();
        try
        {
            foreach (ChatRoomRoleDefinition role in _state.Roles.Where(role => !role.IsHuman))
            {
                ChatRoomRoleRuntimeLease lease = _runtimeRegistry.Acquire(role);
                leases.Add(lease);
                IChatRoomRoleWorkspaceTransaction transaction = await lease
                    .PrepareWorkspaceChangeAsync(command.WorkspacePath)
                    .ConfigureAwait(false);
                transactions.Add(transaction);
            }

            foreach (IChatRoomRoleWorkspaceTransaction transaction in transactions)
            {
                transaction.Apply();
            }

            _workspacePath = command.WorkspacePath;
            ChatRoomState next = CreateState(
                revision: _state.Revision + 1,
                workspaceVersion: _state.WorkspaceVersion + 1);
            PublishState(next, ChatRoomChangeKind.RoomUpdated, detail: "工作区授权已更新。");
            foreach (IChatRoomRoleWorkspaceTransaction transaction in transactions)
            {
                transaction.CommitAfterPublish();
            }

            return Receipt(command, ChatRoomCommandOutcome.Applied);
        }
        catch
        {
            List<Exception>? rollbackExceptions = null;
            foreach (IChatRoomRoleWorkspaceTransaction transaction in transactions.AsEnumerable().Reverse())
            {
                try
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    (rollbackExceptions ??= []).Add(ex);
                }
            }
            if (rollbackExceptions is not null)
            {
                throw new AggregateException("工作区发布失败且回滚发生错误。", rollbackExceptions);
            }

            throw;
        }
        finally
        {
            foreach (IChatRoomRoleWorkspaceTransaction transaction in transactions)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
            foreach (ChatRoomRoleRuntimeLease lease in leases)
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private ChatRoomCommandReceipt HandleStartExecution(StartRoleExecutionCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }
        return StartExecutionCore(command, command.RoleId, autoLoopExecution: false);
    }

    private ChatRoomCommandReceipt HandleStartAutoLoop(StartAutoLoopCommand command)
    {
        if (!CanMutate(command, out ChatRoomCommandReceipt? rejection))
        {
            return rejection;
        }
        if (_state.AutoLoop.Status != ChatRoomAutoLoopStatus.Idle)
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp, "自动循环已经运行。");
        }
        if (_state.CurrentExecution is not null)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "已有手动执行，不能启动自动循环。");
        }

        _autoLoopQueue.Clear();
        _autoLoopTurnsByRole.Clear();
        _lastAutoLoopRoleId = null;
        EnqueueMentionedRolesFromLatestMessage();
        EnqueueDefaultRoles();
        var autoLoop = new ChatRoomAutoLoopState(
            ChatRoomAutoLoopStatus.Running,
            completedTurns: 0,
            command.MaxTurns,
            command.MaxTurnsPerRole);
        ChatRoomState next = CreateState(
            revision: _state.Revision + 1,
            autoLoop: autoLoop);
        PublishState(next, ChatRoomChangeKind.AutoLoopChanged, detail: "自动循环已启动。");
        TryStartNextAutoLoopRole();
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private ChatRoomCommandReceipt HandleStopAutoLoop(StopAutoLoopCommand command)
    {
        if (_state.AutoLoop.Status == ChatRoomAutoLoopStatus.Idle)
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }

        _autoLoopQueue.Clear();
        if (_state.CurrentExecution is not null)
        {
            CancelActiveExecution();
            ChatRoomState stopping = CreateState(
                revision: _state.Revision + 1,
                autoLoop: new ChatRoomAutoLoopState(
                    ChatRoomAutoLoopStatus.Stopping,
                    _state.AutoLoop.CompletedTurns,
                    _state.AutoLoop.MaxTurns,
                    _state.AutoLoop.MaxTurnsPerRole));
            PublishState(stopping, ChatRoomChangeKind.AutoLoopChanged, detail: "自动循环正在停止。");
            return Receipt(command, ChatRoomCommandOutcome.Applied, "正在等待当前执行取消。");
        }

        TransitionAutoLoopToIdle("自动循环已停止。");
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private ChatRoomCommandReceipt StartExecutionCore(
        ChatRoomCommand command,
        string roleId,
        bool autoLoopExecution)
    {
        if (_state.CurrentExecution is not null)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "房间已有活动执行。");
        }
        ChatRoomRoleDefinition? definition = _state.Roles.FirstOrDefault(role =>
            string.Equals(role.Identity.RoleId, roleId, StringComparison.Ordinal));
        if (definition is null)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "角色不存在。");
        }
        if (definition.IsHuman)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "人类角色不能启动模型执行。");
        }

        long consumedThroughSequence = _state.ConsumedThroughSequenceByRole.TryGetValue(
            definition.Identity.RoleId,
            out long consumed)
            ? consumed
            : 0;
        long inputThroughSequence = _state.NextMessageSequence - 1;
        ChatRoomMessage[] inputMessages = _state.Messages
            .Where(message => message.MessageSequence > consumedThroughSequence)
            .ToArray();
        if (inputMessages.Length == 0)
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp, "没有新的公开消息可供角色消费。");
        }

        ChatRoomRoleRuntimeLease runtimeLease;
        try
        {
            runtimeLease = _runtimeRegistry.Acquire(definition);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return Receipt(command, ChatRoomCommandOutcome.Rejected, ex.Message);
        }

        _checkpoints.TryGetValue(definition.Identity, out ChatRoomRoleCheckpoint? checkpoint);
        var request = new ChatRoomRoleExecutionRequest(
            command.CommandId,
            definition,
            inputMessages,
            inputThroughSequence,
            checkpoint,
            _workspacePath);
        var execution = new ChatRoomExecutionState(
            command.CommandId,
            definition.Identity,
            definition.RuntimeVersion,
            _state.WorkspaceVersion,
            inputThroughSequence,
            ChatRoomExecutionStatus.Running,
            _utcNow());
        ChatRoomState next = CreateState(
            revision: _state.Revision + 1,
            currentExecution: execution,
            preserveCurrentExecution: false);
        PublishState(
            next,
            ChatRoomChangeKind.ExecutionChanged,
            executionId: execution.ExecutionId,
            roleId: definition.Identity.RoleId,
            detail: "角色执行已开始。");

        var executionCancellation = new CancellationTokenSource();
        SetActiveExecutionCancellation(executionCancellation);
        TrackBackgroundExecution(RunRoleExecutionAsync(
            runtimeLease,
            request,
            _state.RoomInstanceId,
            _state.WorkspaceVersion,
            new CoordinatorExecutionEventSink(
                this,
                _state.RoomId,
                _state.RoomInstanceId,
                command.CommandId,
                definition.Identity,
                definition.RuntimeVersion,
                _state.WorkspaceVersion),
            executionCancellation));
        return Receipt(
            command,
            ChatRoomCommandOutcome.Applied,
            autoLoopExecution ? "自动循环角色执行已开始。" : null,
            command.CommandId);
    }

    private ChatRoomCommandReceipt HandleCompleteExecution(CompleteRoleExecutionCommand command)
    {
        ChatRoomExecutionState? execution = _state.CurrentExecution;
        if (!MatchesCurrentExecution(
                command.RoomInstanceId,
                command.Candidate.ExecutionId,
                command.Candidate.RoleIdentity,
                command.Candidate.RoleRuntimeVersion,
                command.WorkspaceVersion,
                execution))
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "候选结果不再属于当前执行。");
        }
        if (_state.LifecycleStatus == ChatRoomLifecycleStatus.Closing)
        {
            ClearActiveExecutionCancellation();
            CancelPendingApprovals(execution!.ExecutionId);
            TransitionClosedAfterExecution(execution.ExecutionId, execution.RoleIdentity.RoleId);
            return Receipt(command, ChatRoomCommandOutcome.Stale, "房间关闭期间的候选结果已丢弃。", execution.ExecutionId);
        }
        if (command.Candidate.CandidateCheckpoint.ConsumedThroughSequence != execution!.InputThroughSequence)
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "候选消费水位与执行租约不一致。");
        }
        ChatRoomRoleDefinition definition = FindRole(execution.RoleIdentity.RoleId);
        if (command.Candidate.CandidateCheckpoint.ExecutionKind != definition.ExecutionKind)
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "候选执行种类与当前角色不一致。");
        }
        long previousSessionRevision = _checkpoints.TryGetValue(
            execution.RoleIdentity,
            out ChatRoomRoleCheckpoint? previousCheckpoint)
            ? previousCheckpoint.SessionRevision
            : 0;
        if (command.Candidate.CandidateCheckpoint.SessionRevision != previousSessionRevision + 1)
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "候选私有会话修订号不连续。");
        }

        DateTimeOffset now = _utcNow();
        var messages = _state.Messages.ToList();
        Guid? messageId = null;
        long nextMessageSequence = _state.NextMessageSequence;
        if (!string.IsNullOrWhiteSpace(command.Candidate.PublicContent))
        {
            var message = new ChatRoomMessage(
                nextMessageSequence,
                Guid.NewGuid(),
                ChatRoomMessageKind.Assistant,
                command.Candidate.PublicContent!,
                now,
                execution.RoleIdentity.RoleId,
                definition.RoleName,
                ParseMentions(command.Candidate.PublicContent!, _state.Roles),
                command.Candidate.ModelDisplayName);
            messages.Add(message);
            messageId = message.MessageId;
            nextMessageSequence++;
        }

        long revision = _state.Revision + 1;
        ChatRoomRoleCheckpoint checkpoint = command.Candidate.CandidateCheckpoint.Commit(revision);
        var consumed = new Dictionary<string, long>(_state.ConsumedThroughSequenceByRole, StringComparer.Ordinal)
        {
            [execution.RoleIdentity.RoleId] = execution.InputThroughSequence,
        };
        ClearActiveExecutionCancellation();
        CancelPendingApprovals(execution.ExecutionId);
        ChatRoomState next = CreateState(
            lastActivityAt: Later(_state.LastActivityAt, now),
            revision: revision,
            nextMessageSequence: nextMessageSequence,
            messages: messages,
            consumedThroughSequenceByRole: consumed,
            currentExecution: null,
            preserveCurrentExecution: false,
            persistenceHealth: ChatRoomPersistenceHealth.Dirty,
            lastPersistenceError: null);
        lock (_stateSync)
        {
            _checkpoints[checkpoint.RoleIdentity] = checkpoint;
            PublishState(
                next,
                messageId is null ? ChatRoomChangeKind.ExecutionChanged : ChatRoomChangeKind.MessageCommitted,
                executionId: execution.ExecutionId,
                messageId: messageId,
                roleId: execution.RoleIdentity.RoleId,
                detail: "角色执行已完成。");
        }
        ContinueAutoLoopAfterExecution(execution.RoleIdentity.RoleId, command.Candidate.PublicContent);
        return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: execution.ExecutionId);
    }

    private ChatRoomCommandReceipt HandleFailExecution(FailRoleExecutionCommand command)
    {
        ChatRoomExecutionState? execution = _state.CurrentExecution;
        if (!MatchesCurrentExecution(
                command.RoomInstanceId,
                command.ExecutionId,
                command.RoleIdentity,
                command.RoleRuntimeVersion,
                command.WorkspaceVersion,
                execution))
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "失败结果不再属于当前执行。");
        }
        if (_state.LifecycleStatus == ChatRoomLifecycleStatus.Closing)
        {
            ClearActiveExecutionCancellation();
            CancelPendingApprovals(command.ExecutionId);
            TransitionClosedAfterExecution(command.ExecutionId, command.RoleIdentity.RoleId);
            return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: command.ExecutionId);
        }

        ClearActiveExecutionCancellation();
        CancelPendingApprovals(command.ExecutionId);
        ChatRoomState next = CreateState(
            revision: _state.Revision + 1,
            currentExecution: null,
            preserveCurrentExecution: false);
        PublishState(
            next,
            ChatRoomChangeKind.ExecutionChanged,
            executionId: command.ExecutionId,
            roleId: command.RoleIdentity.RoleId,
            detail: command.WasCanceled ? "角色执行已取消。" : command.FailureMessage ?? "角色执行失败。");
        ContinueAutoLoopAfterExecution(command.RoleIdentity.RoleId, publicContent: null);
        return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: command.ExecutionId);
    }

    private ChatRoomCommandReceipt HandleStop(StopRoomCommand command)
    {
        _autoLoopQueue.Clear();
        bool autoLoopWasRunning = _state.AutoLoop.Status != ChatRoomAutoLoopStatus.Idle;
        if (_state.CurrentExecution is null)
        {
            if (autoLoopWasRunning)
            {
                TransitionAutoLoopToIdle("房间执行已停止。");
                return Receipt(command, ChatRoomCommandOutcome.Applied);
            }

            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }

        CancelActiveExecution();
        if (autoLoopWasRunning)
        {
            ChatRoomState stopping = CreateState(
                revision: _state.Revision + 1,
                autoLoop: new ChatRoomAutoLoopState(
                    ChatRoomAutoLoopStatus.Stopping,
                    _state.AutoLoop.CompletedTurns,
                    _state.AutoLoop.MaxTurns,
                    _state.AutoLoop.MaxTurnsPerRole));
            PublishState(stopping, ChatRoomChangeKind.AutoLoopChanged, detail: "房间执行正在停止。");
        }
        return Receipt(
            command,
            ChatRoomCommandOutcome.Applied,
            "已请求取消当前执行。",
            _state.CurrentExecution.ExecutionId);
    }

    private ChatRoomCommandReceipt HandleStreamDelta(PublishStreamDeltaCommand command)
    {
        if (!MatchesCurrentExecution(
                command.RoomInstanceId,
                command.ExecutionId,
                command.RoleIdentity,
                command.RoleRuntimeVersion,
                command.WorkspaceVersion,
                _state.CurrentExecution))
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "流式增量不再属于当前执行。");
        }

        ChatRoomStreamDelta delta;
        lock (_stateSync)
        {
            delta = new ChatRoomStreamDelta(
                _state.RoomId,
                _state.RoomInstanceId,
                _nextEventSequence++,
                command.ExecutionId,
                command.RoleIdentity.RoleId,
                command.Kind,
                command.Content);
            PublishExecutionEventLocked(new ChatRoomStreamDeltaEvent(delta));
        }
        return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: command.ExecutionId);
    }

    private ChatRoomCommandReceipt HandleApprovalRequest(RequestApprovalCommand command)
    {
        if (!MatchesCurrentExecution(
                command.RoomInstanceId,
                command.ExecutionId,
                command.RoleIdentity,
                command.RoleRuntimeVersion,
                command.WorkspaceVersion,
                _state.CurrentExecution))
        {
            command.Completion.TrySetCanceled();
            return Receipt(command, ChatRoomCommandOutcome.Stale, "审批请求不再属于当前执行。");
        }
        var approvalKey = (command.ExecutionId, command.ApprovalId);
        if (_pendingApprovals.ContainsKey(approvalKey))
        {
            command.Completion.TrySetException(new InvalidOperationException("同一执行中的 ApprovalId 必须唯一。"));
            return Receipt(command, ChatRoomCommandOutcome.Rejected, "ApprovalId 已存在。");
        }

        var pending = new PendingApproval(command.Completion);
        _pendingApprovals.Add(approvalKey, pending);
        ChatRoomExecutionState execution = _state.CurrentExecution!;
        ChatRoomState awaitingApproval = CreateState(
            revision: _state.Revision + 1,
            currentExecution: new ChatRoomExecutionState(
                execution.ExecutionId,
                execution.RoleIdentity,
                execution.RoleRuntimeVersion,
                execution.WorkspaceVersion,
                execution.InputThroughSequence,
                ChatRoomExecutionStatus.AwaitingApproval,
                execution.StartedAt,
                command.ApprovalId),
            preserveCurrentExecution: false);
        PublishState(
            awaitingApproval,
            ChatRoomChangeKind.ExecutionChanged,
            executionId: execution.ExecutionId,
            roleId: execution.RoleIdentity.RoleId,
            detail: "角色执行正在等待审批。");
        lock (_stateSync)
        {
            var approval = new ChatRoomApprovalRequest(
                _state.RoomId,
                _state.RoomInstanceId,
                _nextEventSequence++,
                command.ExecutionId,
                command.ApprovalId,
                command.RoleIdentity.RoleId,
                command.Title,
                command.Detail);
            PublishExecutionEventLocked(new ChatRoomApprovalRequestedEvent(approval));
        }
        return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: command.ExecutionId);
    }

    private ChatRoomCommandReceipt HandleApprovalResponse(RespondToApprovalCommand command)
    {
        ChatRoomApprovalResponse response = command.Response;
        if (response.RoomId != _state.RoomId
            || response.RoomInstanceId != _state.RoomInstanceId
            || _state.CurrentExecution?.ExecutionId != response.ExecutionId)
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "审批响应不再属于当前房间执行。");
        }
        if (!_pendingApprovals.Remove(
                (response.ExecutionId, response.ApprovalId),
                out PendingApproval? pending))
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale, "审批响应未匹配等待中的请求。");
        }

        pending.Completion.TrySetResult(response.Decision);
        ChatRoomExecutionState execution = _state.CurrentExecution;
        ChatRoomState running = CreateState(
            revision: _state.Revision + 1,
            currentExecution: new ChatRoomExecutionState(
                execution.ExecutionId,
                execution.RoleIdentity,
                execution.RoleRuntimeVersion,
                execution.WorkspaceVersion,
                execution.InputThroughSequence,
                ChatRoomExecutionStatus.Running,
                execution.StartedAt),
            preserveCurrentExecution: false);
        PublishState(
            running,
            ChatRoomChangeKind.ExecutionChanged,
            executionId: execution.ExecutionId,
            roleId: execution.RoleIdentity.RoleId,
            detail: response.Decision == ChatRoomApprovalDecision.Approved
                ? "审批已通过。"
                : "审批已拒绝。");
        return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: response.ExecutionId);
    }

    private ChatRoomCommandReceipt HandleClose(CloseRoomCommand command)
    {
        if (_state.LifecycleStatus == ChatRoomLifecycleStatus.Closed)
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }
        if (_state.CurrentExecution is not null)
        {
            _autoLoopQueue.Clear();
            CancelActiveExecution();
            ChatRoomState closing = CreateState(
                revision: _state.Revision + 1,
                lifecycleStatus: ChatRoomLifecycleStatus.Closing,
                autoLoop: new ChatRoomAutoLoopState(
                    ChatRoomAutoLoopStatus.Stopping,
                    _state.AutoLoop.CompletedTurns,
                    _state.AutoLoop.MaxTurns,
                    _state.AutoLoop.MaxTurnsPerRole));
            PublishState(closing, ChatRoomChangeKind.LifecycleChanged, detail: "房间正在等待活动执行终止。");
            return Receipt(command, ChatRoomCommandOutcome.Applied, "房间正在关闭。");
        }

        ChatRoomState closed = CreateState(
            revision: _state.Revision + 1,
            lifecycleStatus: ChatRoomLifecycleStatus.Closed,
            autoLoop: CreateIdleAutoLoopState());
        PublishState(closed, ChatRoomChangeKind.LifecycleChanged, detail: "房间已关闭。");
        return Receipt(command, ChatRoomCommandOutcome.Applied);
    }

    private ChatRoomCommandReceipt HandleExecutionStuck(MarkExecutionStuckCommand command)
    {
        ChatRoomExecutionState? execution = _state.CurrentExecution;
        if (execution is null || execution.ExecutionId != command.ExecutionId)
        {
            return Receipt(command, ChatRoomCommandOutcome.Stale);
        }

        ChatRoomState stuck = CreateState(
            revision: _state.Revision + 1,
            currentExecution: new ChatRoomExecutionState(
                execution.ExecutionId,
                execution.RoleIdentity,
                execution.RoleRuntimeVersion,
                execution.WorkspaceVersion,
                execution.InputThroughSequence,
                ChatRoomExecutionStatus.Stuck,
                execution.StartedAt,
                failureMessage: "底层执行未在关闭期限内终止。"),
            preserveCurrentExecution: false,
            lifecycleStatus: ChatRoomLifecycleStatus.CloseFaulted,
            autoLoop: CreateIdleAutoLoopState());
        PublishState(
            stuck,
            ChatRoomChangeKind.LifecycleChanged,
            executionId: execution.ExecutionId,
            roleId: execution.RoleIdentity.RoleId,
            detail: "房间关闭超时，执行已标记为 Stuck。");
        return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: execution.ExecutionId);
    }

    private ChatRoomCommandReceipt HandleForceAbandon(ForceAbandonRoomCommand command)
    {
        if (_state.LifecycleStatus == ChatRoomLifecycleStatus.Closed)
        {
            return Receipt(command, ChatRoomCommandOutcome.NoOp);
        }

        Guid? executionId = _state.CurrentExecution?.ExecutionId;
        string? roleId = _state.CurrentExecution?.RoleIdentity.RoleId;
        if (executionId is Guid activeExecutionId)
        {
            CancelActiveExecution();
            CancelPendingApprovals(activeExecutionId);
        }
        ChatRoomState closed = CreateState(
            revision: _state.Revision + 1,
            currentExecution: null,
            preserveCurrentExecution: false,
            lifecycleStatus: ChatRoomLifecycleStatus.Closed,
            autoLoop: CreateIdleAutoLoopState());
        PublishState(
            closed,
            ChatRoomChangeKind.LifecycleChanged,
            executionId: executionId,
            roleId: roleId,
            detail: executionId is null ? "房间已关闭。" : "房间已逻辑放弃未终止执行并关闭。");
        return Receipt(command, ChatRoomCommandOutcome.Applied, executionId: executionId);
    }

    private void PublishExecutionEventLocked(ChatRoomExecutionEvent executionEvent)
    {
        foreach (ChatRoomCoordinatorSubscription subscription in _subscriptions.ToArray())
        {
            subscription.PublishExecutionEvent(executionEvent);
        }
    }

    private void CancelPendingApprovals(Guid executionId)
    {
        foreach (KeyValuePair<(Guid ExecutionId, string ApprovalId), PendingApproval> pair in _pendingApprovals
                     .Where(pair => pair.Key.ExecutionId == executionId)
                     .ToArray())
        {
            _pendingApprovals.Remove(pair.Key);
            pair.Value.Completion.TrySetCanceled();
        }
    }

    private async Task RunRoleExecutionAsync(
        ChatRoomRoleRuntimeLease runtimeLease,
        ChatRoomRoleExecutionRequest request,
        Guid roomInstanceId,
        long workspaceVersion,
        IChatRoomRoleExecutionEventSink eventSink,
        CancellationTokenSource executionCancellation)
    {
        try
        {
            ChatRoomRoleExecutionCandidate candidate = await runtimeLease
                .ExecuteAsync(request, eventSink, executionCancellation.Token)
                .ConfigureAwait(false);
            await EnqueueRuntimeCallbackAsync(new CompleteRoleExecutionCommand(
                Guid.NewGuid(),
                roomInstanceId,
                workspaceVersion,
                candidate)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (executionCancellation.IsCancellationRequested)
        {
            await EnqueueRuntimeCallbackAsync(new FailRoleExecutionCommand(
                Guid.NewGuid(),
                roomInstanceId,
                request.ExecutionId,
                request.Definition.Identity,
                request.Definition.RuntimeVersion,
                workspaceVersion,
                wasCanceled: true)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await EnqueueRuntimeCallbackAsync(new FailRoleExecutionCommand(
                Guid.NewGuid(),
                roomInstanceId,
                request.ExecutionId,
                request.Definition.Identity,
                request.Definition.RuntimeVersion,
                workspaceVersion,
                wasCanceled: false,
                ex.Message)).ConfigureAwait(false);
        }
        finally
        {
            await runtimeLease.DisposeAsync().ConfigureAwait(false);
            executionCancellation.Dispose();
        }
    }

    private async Task EnqueueRuntimeCallbackAsync(ChatRoomCommand command)
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            return;
        }

        try
        {
            await ExecuteAsync(command).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (ChannelClosedException)
        {
        }
    }

    private void TrackBackgroundExecution(Task executionTask)
    {
        lock (_backgroundSync)
        {
            _backgroundExecutions.Add(executionTask);
        }

        _ = ObserveBackgroundExecutionAsync(executionTask);
    }

    private async Task ObserveBackgroundExecutionAsync(Task executionTask)
    {
        try
        {
            await executionTask.ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            lock (_backgroundSync)
            {
                _backgroundExecutions.Remove(executionTask);
            }
        }
    }

    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref _isDisposed, 1);
        CancelActiveExecution();

        while (true)
        {
            Task[] backgroundExecutions;
            lock (_backgroundSync)
            {
                backgroundExecutions = _backgroundExecutions.ToArray();
            }
            if (backgroundExecutions.Length == 0)
            {
                break;
            }

            await Task.WhenAll(backgroundExecutions).ConfigureAwait(false);
        }

        _commands.Writer.TryComplete();
        await _commandLoop.ConfigureAwait(false);
        await _runtimeRegistry.DisposeAsync().ConfigureAwait(false);

        ChatRoomCoordinatorSubscription[] subscriptions;
        lock (_stateSync)
        {
            subscriptions = _subscriptions.ToArray();
            _subscriptions.Clear();
        }
        foreach (ChatRoomCoordinatorSubscription subscription in subscriptions)
        {
            subscription.Complete();
        }
    }

    private bool CanMutate(ChatRoomCommand command, out ChatRoomCommandReceipt rejection)
    {
        if (_state.LifecycleStatus != ChatRoomLifecycleStatus.Open)
        {
            rejection = Receipt(command, ChatRoomCommandOutcome.Rejected, "房间不再接受新命令。");
            return false;
        }

        rejection = null!;
        return true;
    }

    private ChatRoomState CreateState(
        string? title = null,
        DateTimeOffset? lastActivityAt = null,
        long? revision = null,
        long? durableRevision = null,
        long? nextMessageSequence = null,
        long? workspaceVersion = null,
        IReadOnlyList<ChatRoomRoleDefinition>? roles = null,
        IReadOnlyList<ChatRoomMessage>? messages = null,
        IReadOnlyDictionary<string, long>? consumedThroughSequenceByRole = null,
        ChatRoomExecutionState? currentExecution = null,
        ChatRoomPersistenceHealth? persistenceHealth = null,
        ChatRoomLifecycleStatus? lifecycleStatus = null,
        string? lastPersistenceError = null,
        bool preserveLastPersistenceError = false,
        bool preserveCurrentExecution = true,
        ChatRoomAutoLoopState? autoLoop = null)
    {
        return new ChatRoomState(
            _state.RoomId,
            _state.RoomInstanceId,
            title ?? _state.Title,
            _state.CreatedAt,
            lastActivityAt ?? _state.LastActivityAt,
            revision ?? _state.Revision,
            durableRevision ?? _state.DurableRevision,
            nextMessageSequence ?? _state.NextMessageSequence,
            workspaceVersion ?? _state.WorkspaceVersion,
            roles ?? _state.Roles,
            messages ?? _state.Messages,
            consumedThroughSequenceByRole ?? _state.ConsumedThroughSequenceByRole,
            preserveCurrentExecution ? _state.CurrentExecution : currentExecution,
            persistenceHealth ?? _state.PersistenceHealth,
            lifecycleStatus ?? _state.LifecycleStatus,
            preserveLastPersistenceError ? _state.LastPersistenceError : lastPersistenceError,
            autoLoop ?? _state.AutoLoop);
    }

    private void ContinueAutoLoopAfterExecution(string roleId, string? publicContent)
    {
        if (_state.AutoLoop.Status == ChatRoomAutoLoopStatus.Stopping)
        {
            TransitionAutoLoopToIdle("自动循环已停止。");
            return;
        }
        if (_state.AutoLoop.Status != ChatRoomAutoLoopStatus.Running)
        {
            return;
        }

        int completedTurns = _state.AutoLoop.CompletedTurns + 1;
        _autoLoopTurnsByRole[roleId] = _autoLoopTurnsByRole.TryGetValue(roleId, out int roleTurns)
            ? roleTurns + 1
            : 1;
        _lastAutoLoopRoleId = roleId;
        if (!string.IsNullOrWhiteSpace(publicContent))
        {
            EnqueueMentionedRoles(publicContent);
        }
        if (completedTurns >= _state.AutoLoop.MaxTurns)
        {
            TransitionAutoLoopToIdle("自动循环达到最大轮次。", completedTurns);
            return;
        }

        ChatRoomState next = CreateState(
            autoLoop: new ChatRoomAutoLoopState(
                ChatRoomAutoLoopStatus.Running,
                completedTurns,
                _state.AutoLoop.MaxTurns,
                _state.AutoLoop.MaxTurnsPerRole));
        PublishState(next, ChatRoomChangeKind.AutoLoopChanged, detail: "自动循环继续调度。");
        TryStartNextAutoLoopRole();
    }

    private void TryStartNextAutoLoopRole()
    {
        if (_state.AutoLoop.Status != ChatRoomAutoLoopStatus.Running
            || _state.CurrentExecution is not null)
        {
            return;
        }

        ChatRoomRoleDefinition? nextRole = DequeueNextAutoLoopRole();
        if (nextRole is null)
        {
            ChatRoomRoleDefinition? manager = _state.Roles.FirstOrDefault(role =>
                !role.IsHuman
                && role.IsManagerRole
                && !string.Equals(role.Identity.RoleId, _lastAutoLoopRoleId, StringComparison.Ordinal)
                && GetAutoLoopTurnCount(role.Identity.RoleId) < _state.AutoLoop.MaxTurnsPerRole);
            nextRole = manager;
        }
        if (nextRole is null)
        {
            TransitionAutoLoopToIdle("自动循环没有可继续发言的角色。");
            return;
        }

        var command = new StartRoleExecutionCommand(Guid.NewGuid(), nextRole.Identity.RoleId);
        ChatRoomCommandReceipt receipt = StartExecutionCore(command, nextRole.Identity.RoleId, autoLoopExecution: true);
        if (receipt.Outcome != ChatRoomCommandOutcome.Applied)
        {
            TryStartNextAutoLoopRole();
        }
    }

    private ChatRoomRoleDefinition? DequeueNextAutoLoopRole()
    {
        while (_autoLoopQueue.First is not null)
        {
            string roleId = _autoLoopQueue.First.Value;
            _autoLoopQueue.RemoveFirst();
            ChatRoomRoleDefinition? role = _state.Roles.FirstOrDefault(item =>
                string.Equals(item.Identity.RoleId, roleId, StringComparison.Ordinal));
            if (role is null
                || role.IsHuman
                || string.Equals(role.Identity.RoleId, _lastAutoLoopRoleId, StringComparison.Ordinal)
                || GetAutoLoopTurnCount(role.Identity.RoleId) >= _state.AutoLoop.MaxTurnsPerRole)
            {
                continue;
            }

            return role;
        }

        return null;
    }

    private void EnqueueMentionedRolesFromLatestMessage()
    {
        ChatRoomMessage? message = _state.Messages.LastOrDefault();
        if (message is null)
        {
            return;
        }

        for (int i = message.MentionedRoleIds.Count - 1; i >= 0; i--)
        {
            EnqueueAutoLoopRole(message.MentionedRoleIds[i], prioritize: true);
        }
    }

    private void EnqueueMentionedRoles(string content)
    {
        IReadOnlyList<string> roleIds = ParseMentions(content, _state.Roles);
        for (int i = roleIds.Count - 1; i >= 0; i--)
        {
            EnqueueAutoLoopRole(roleIds[i], prioritize: true);
        }
    }

    private void EnqueueDefaultRoles()
    {
        foreach (ChatRoomRoleDefinition role in _state.Roles)
        {
            if (!role.IsHuman && role.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate)
            {
                EnqueueAutoLoopRole(role.Identity.RoleId, prioritize: false);
            }
        }
    }

    private void EnqueueAutoLoopRole(string roleId, bool prioritize)
    {
        if (_autoLoopQueue.Contains(roleId))
        {
            return;
        }
        if (prioritize)
        {
            _autoLoopQueue.AddFirst(roleId);
        }
        else
        {
            _autoLoopQueue.AddLast(roleId);
        }
    }

    private int GetAutoLoopTurnCount(string roleId) =>
        _autoLoopTurnsByRole.TryGetValue(roleId, out int count) ? count : 0;

    private void TransitionAutoLoopToIdle(string detail, int? completedTurns = null)
    {
        _autoLoopQueue.Clear();
        ChatRoomState idle = CreateState(
            revision: _state.Revision + 1,
            autoLoop: new ChatRoomAutoLoopState(
                ChatRoomAutoLoopStatus.Idle,
                completedTurns ?? _state.AutoLoop.CompletedTurns,
                _state.AutoLoop.MaxTurns,
                _state.AutoLoop.MaxTurnsPerRole));
        PublishState(idle, ChatRoomChangeKind.AutoLoopChanged, detail: detail);
    }

    private ChatRoomAutoLoopState CreateIdleAutoLoopState() => new(
        ChatRoomAutoLoopStatus.Idle,
        _state.AutoLoop.CompletedTurns,
        _state.AutoLoop.MaxTurns,
        _state.AutoLoop.MaxTurnsPerRole);

    private async Task WaitForStateAsync(
        Func<ChatRoomState, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (predicate(State))
        {
            return;
        }

        await using ChatRoomCoordinatorSubscription subscription = Subscribe();
        if (predicate(subscription.InitialSnapshot.State))
        {
            return;
        }

        using CancellationTokenSource timeoutCancellation = timeout == Timeout.InfiniteTimeSpan
            ? new CancellationTokenSource()
            : new CancellationTokenSource(timeout);
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);
        try
        {
            await foreach (ChatRoomChange change in subscription
                               .ReadAllAsync(linkedCancellation.Token)
                               .ConfigureAwait(false))
            {
                if (predicate(change.State))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (
            timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("等待房间状态变化超时。");
        }
    }

    private void PublishState(
        ChatRoomState next,
        ChatRoomChangeKind kind,
        Guid? executionId = null,
        Guid? messageId = null,
        string? roleId = null,
        string? detail = null)
    {
        lock (_stateSync)
        {
            _state = next;
            var change = new ChatRoomChange(
                kind,
                next,
                _nextEventSequence++,
                _utcNow(),
                executionId,
                messageId,
                roleId,
                detail);
            foreach (ChatRoomCoordinatorSubscription subscription in _subscriptions.ToArray())
            {
                subscription.Publish(change);
            }
        }
    }

    private ChatRoomSnapshot CreateSnapshotLocked() => new(
        _state,
        _checkpoints.Values.OrderBy(checkpoint => checkpoint.RoleIdentity.RoleId, StringComparer.Ordinal));

    private ChatRoomCommandReceipt Receipt(
        ChatRoomCommand command,
        ChatRoomCommandOutcome outcome,
        string? detail = null,
        Guid? executionId = null) => new(
        command.CommandId,
        outcome,
        _state,
        detail,
        executionId);

    private ChatRoomRoleDefinition FindRole(string roleId) =>
        _state.Roles.First(role => string.Equals(role.Identity.RoleId, roleId, StringComparison.Ordinal));

    private int FindRoleIndex(string roleId)
    {
        for (int i = 0; i < _state.Roles.Count; i++)
        {
            if (string.Equals(_state.Roles[i].Identity.RoleId, roleId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private bool MatchesCurrentExecution(
        Guid roomInstanceId,
        Guid executionId,
        ChatRoomRoleIdentity roleIdentity,
        long roleRuntimeVersion,
        long workspaceVersion,
        ChatRoomExecutionState? current)
    {
        return current is not null
            && roomInstanceId == _state.RoomInstanceId
            && current.ExecutionId == executionId
            && current.RoleIdentity == roleIdentity
            && current.RoleRuntimeVersion == roleRuntimeVersion
            && current.WorkspaceVersion == workspaceVersion;
    }

    private void SetActiveExecutionCancellation(CancellationTokenSource cancellation)
    {
        lock (_executionSync)
        {
            _activeExecutionCancellation = cancellation;
        }
    }

    private void CancelActiveExecution()
    {
        lock (_executionSync)
        {
            try
            {
                _activeExecutionCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private void ClearActiveExecutionCancellation()
    {
        lock (_executionSync)
        {
            _activeExecutionCancellation = null;
        }
    }

    private static IReadOnlyList<string> ParseMentions(
        string content,
        IReadOnlyList<ChatRoomRoleDefinition> roles)
    {
        if (string.IsNullOrEmpty(content) || roles.Count == 0)
        {
            return Array.Empty<string>();
        }

        var matches = new List<(int Index, string RoleId)>();
        foreach (ChatRoomRoleDefinition role in roles)
        {
            int searchIndex = 0;
            string token = $"@{role.RoleName}";
            while (searchIndex < content.Length)
            {
                int index = content.IndexOf(token, searchIndex, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                matches.Add((index, role.Identity.RoleId));
                searchIndex = index + token.Length;
            }
        }

        return matches
            .OrderBy(match => match.Index)
            .Select(match => match.RoleId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool AreSameWorkspace(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        string normalizedLeft = Path.GetFullPath(left);
        string normalizedRight = Path.GetFullPath(right);
        return string.Equals(
            normalizedLeft,
            normalizedRight,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static DateTimeOffset Later(DateTimeOffset left, DateTimeOffset right) =>
        left >= right ? left : right;

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            throw new ObjectDisposedException(nameof(ChatRoomCoordinator));
        }
    }

    private sealed class CommandEnvelope(ChatRoomCommand command)
    {
        internal ChatRoomCommand Command { get; } = command;

        internal TaskCompletionSource<ChatRoomCommandReceipt> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record PendingApproval(
        TaskCompletionSource<ChatRoomApprovalDecision> Completion);

    private sealed class CoordinatorExecutionEventSink(
        ChatRoomCoordinator owner,
        Guid roomId,
        Guid roomInstanceId,
        Guid executionId,
        ChatRoomRoleIdentity roleIdentity,
        long roleRuntimeVersion,
        long workspaceVersion) : IChatRoomRoleExecutionEventSink
    {
        public async ValueTask ReportDeltaAsync(
            ChatRoomStreamDeltaKind kind,
            string content,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            ChatRoomCommandReceipt receipt = await owner.ExecuteAsync(new PublishStreamDeltaCommand(
                Guid.NewGuid(),
                roomInstanceId,
                executionId,
                roleIdentity,
                roleRuntimeVersion,
                workspaceVersion,
                kind,
                content), cancellationToken).ConfigureAwait(false);
            if (receipt.Outcome == ChatRoomCommandOutcome.Stale)
            {
                throw new OperationCanceledException("执行已失效。", cancellationToken);
            }
        }

        public async Task<ChatRoomApprovalDecision> RequestApprovalAsync(
            string approvalId,
            string title,
            string? detail = null,
            CancellationToken cancellationToken = default)
        {
            _ = roomId;
            var completion = new TaskCompletionSource<ChatRoomApprovalDecision>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            ChatRoomCommandReceipt receipt = await owner.ExecuteAsync(new RequestApprovalCommand(
                Guid.NewGuid(),
                roomInstanceId,
                executionId,
                roleIdentity,
                roleRuntimeVersion,
                workspaceVersion,
                approvalId,
                title,
                detail,
                completion), cancellationToken).ConfigureAwait(false);
            if (receipt.Outcome != ChatRoomCommandOutcome.Applied)
            {
                throw new OperationCanceledException("审批请求未被接受。", cancellationToken);
            }

            return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed record PublishStreamDeltaCommand : ChatRoomCommand
    {
        internal PublishStreamDeltaCommand(
            Guid commandId,
            Guid roomInstanceId,
            Guid executionId,
            ChatRoomRoleIdentity roleIdentity,
            long roleRuntimeVersion,
            long workspaceVersion,
            ChatRoomStreamDeltaKind kind,
            string content)
            : base(commandId)
        {
            RoomInstanceId = roomInstanceId;
            ExecutionId = executionId;
            RoleIdentity = roleIdentity;
            RoleRuntimeVersion = roleRuntimeVersion;
            WorkspaceVersion = workspaceVersion;
            Kind = kind;
            Content = content;
        }

        internal Guid RoomInstanceId { get; }

        internal Guid ExecutionId { get; }

        internal ChatRoomRoleIdentity RoleIdentity { get; }

        internal long RoleRuntimeVersion { get; }

        internal long WorkspaceVersion { get; }

        internal ChatRoomStreamDeltaKind Kind { get; }

        internal string Content { get; }
    }

    private sealed record RequestApprovalCommand : ChatRoomCommand
    {
        internal RequestApprovalCommand(
            Guid commandId,
            Guid roomInstanceId,
            Guid executionId,
            ChatRoomRoleIdentity roleIdentity,
            long roleRuntimeVersion,
            long workspaceVersion,
            string approvalId,
            string title,
            string? detail,
            TaskCompletionSource<ChatRoomApprovalDecision> completion)
            : base(commandId)
        {
            RoomInstanceId = roomInstanceId;
            ExecutionId = executionId;
            RoleIdentity = roleIdentity;
            RoleRuntimeVersion = roleRuntimeVersion;
            WorkspaceVersion = workspaceVersion;
            ApprovalId = approvalId;
            Title = title;
            Detail = detail;
            Completion = completion;
        }

        internal Guid RoomInstanceId { get; }

        internal Guid ExecutionId { get; }

        internal ChatRoomRoleIdentity RoleIdentity { get; }

        internal long RoleRuntimeVersion { get; }

        internal long WorkspaceVersion { get; }

        internal string ApprovalId { get; }

        internal string Title { get; }

        internal string? Detail { get; }

        internal TaskCompletionSource<ChatRoomApprovalDecision> Completion { get; }
    }

    private sealed record MarkExecutionStuckCommand : ChatRoomCommand
    {
        internal MarkExecutionStuckCommand(Guid commandId, Guid executionId)
            : base(commandId)
        {
            ExecutionId = executionId;
        }

        internal Guid ExecutionId { get; }
    }
}
