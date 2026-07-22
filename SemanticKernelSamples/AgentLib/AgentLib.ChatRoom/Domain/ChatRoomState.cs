using System.Collections.ObjectModel;

namespace AgentLib.ChatRoom.Domain;

/// <summary>
/// 当前执行租约的领域快照。
/// </summary>
public sealed record ChatRoomExecutionState
{
    /// <summary>
    /// 创建执行状态。
    /// </summary>
    public ChatRoomExecutionState(
        Guid executionId,
        ChatRoomRoleIdentity roleIdentity,
        long roleRuntimeVersion,
        long workspaceVersion,
        long inputThroughSequence,
        ChatRoomExecutionStatus status,
        DateTimeOffset startedAt,
        string? approvalId = null,
        string? failureMessage = null)
    {
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException("执行标识不能为空。", nameof(executionId));
        }

        ArgumentNullException.ThrowIfNull(roleIdentity);
        if (roleRuntimeVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleRuntimeVersion));
        }
        if (workspaceVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workspaceVersion));
        }
        if (inputThroughSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputThroughSequence));
        }
        if (!Enum.IsDefined(typeof(ChatRoomExecutionStatus), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ExecutionId = executionId;
        RoleIdentity = roleIdentity;
        RoleRuntimeVersion = roleRuntimeVersion;
        WorkspaceVersion = workspaceVersion;
        InputThroughSequence = inputThroughSequence;
        Status = status;
        StartedAt = startedAt;
        ApprovalId = NormalizeOptionalValue(approvalId);
        FailureMessage = NormalizeOptionalValue(failureMessage);
    }

    /// <summary>
    /// 执行标识。
    /// </summary>
    public Guid ExecutionId { get; }

    /// <summary>
    /// 执行角色身份。
    /// </summary>
    public ChatRoomRoleIdentity RoleIdentity { get; }

    /// <summary>
    /// 执行绑定的角色运行时版本。
    /// </summary>
    public long RoleRuntimeVersion { get; }

    /// <summary>
    /// 执行绑定的瞬态工作区版本。
    /// </summary>
    public long WorkspaceVersion { get; }

    /// <summary>
    /// 本轮输入覆盖到的消息序号。
    /// </summary>
    public long InputThroughSequence { get; }

    /// <summary>
    /// 执行状态。
    /// </summary>
    public ChatRoomExecutionStatus Status { get; }

    /// <summary>
    /// 开始时间。
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// 当前审批标识。
    /// </summary>
    public string? ApprovalId { get; }

    /// <summary>
    /// 失败摘要。
    /// </summary>
    public string? FailureMessage { get; }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// 自动循环的瞬态领域状态。
/// </summary>
public sealed record ChatRoomAutoLoopState
{
    /// <summary>
    /// 创建自动循环状态。
    /// </summary>
    public ChatRoomAutoLoopState(
        ChatRoomAutoLoopStatus status,
        int completedTurns,
        int maxTurns,
        int maxTurnsPerRole)
    {
        if (!Enum.IsDefined(typeof(ChatRoomAutoLoopStatus), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }
        if (completedTurns < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedTurns));
        }
        if (maxTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTurns));
        }
        if (maxTurnsPerRole <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTurnsPerRole));
        }
        if (completedTurns > maxTurns)
        {
            throw new ArgumentOutOfRangeException(nameof(completedTurns));
        }

        Status = status;
        CompletedTurns = completedTurns;
        MaxTurns = maxTurns;
        MaxTurnsPerRole = maxTurnsPerRole;
    }

    public ChatRoomAutoLoopStatus Status { get; }

    public int CompletedTurns { get; }

    public int MaxTurns { get; }

    public int MaxTurnsPerRole { get; }
}

/// <summary>
/// 聊天室唯一权威的不可变领域状态。
/// </summary>
public sealed record ChatRoomState
{
    /// <summary>
    /// 创建领域状态。
    /// </summary>
    public ChatRoomState(
        Guid roomId,
        Guid roomInstanceId,
        string title,
        DateTimeOffset createdAt,
        DateTimeOffset lastActivityAt,
        long revision,
        long durableRevision,
        long nextMessageSequence,
        long workspaceVersion,
        IEnumerable<ChatRoomRoleDefinition>? roles = null,
        IEnumerable<ChatRoomMessage>? messages = null,
        IReadOnlyDictionary<string, long>? consumedThroughSequenceByRole = null,
        ChatRoomExecutionState? currentExecution = null,
        ChatRoomPersistenceHealth persistenceHealth = ChatRoomPersistenceHealth.Clean,
        ChatRoomLifecycleStatus lifecycleStatus = ChatRoomLifecycleStatus.Open,
        string? lastPersistenceError = null,
        ChatRoomAutoLoopState? autoLoop = null)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("房间标识不能为空。", nameof(roomId));
        }
        if (roomInstanceId == Guid.Empty)
        {
            throw new ArgumentException("房间实例标识不能为空。", nameof(roomInstanceId));
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("房间标题不能为空或空白。", nameof(title));
        }
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }
        if (durableRevision < -1 || durableRevision > revision)
        {
            throw new ArgumentOutOfRangeException(nameof(durableRevision));
        }
        if (nextMessageSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextMessageSequence));
        }
        if (workspaceVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workspaceVersion));
        }
        if (!Enum.IsDefined(typeof(ChatRoomPersistenceHealth), persistenceHealth))
        {
            throw new ArgumentOutOfRangeException(nameof(persistenceHealth));
        }
        if (!Enum.IsDefined(typeof(ChatRoomLifecycleStatus), lifecycleStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(lifecycleStatus));
        }
        if (lastActivityAt < createdAt)
        {
            throw new ArgumentException("最后活动时间不能早于创建时间。", nameof(lastActivityAt));
        }

        ChatRoomRoleDefinition[] roleValues = roles?.ToArray() ?? Array.Empty<ChatRoomRoleDefinition>();
        ChatRoomMessage[] messageValues = messages?.ToArray() ?? Array.Empty<ChatRoomMessage>();
        ValidateRoles(roleValues);
        ValidateMessages(messageValues, nextMessageSequence);
        if (messageValues.Length > 0 && lastActivityAt < messageValues[^1].Timestamp)
        {
            throw new ArgumentException("最后活动时间不能早于最后一条消息。", nameof(lastActivityAt));
        }
        IReadOnlyDictionary<string, long> consumedValues = CopyConsumedSequences(
            consumedThroughSequenceByRole,
            roleValues,
            nextMessageSequence - 1);
        ValidateCurrentExecution(currentExecution, roleValues, nextMessageSequence - 1, workspaceVersion);

        RoomId = roomId;
        RoomInstanceId = roomInstanceId;
        Title = title.Trim();
        CreatedAt = createdAt;
        LastActivityAt = lastActivityAt;
        Revision = revision;
        DurableRevision = durableRevision;
        NextMessageSequence = nextMessageSequence;
        WorkspaceVersion = workspaceVersion;
        Roles = roleValues.Length == 0
            ? Array.Empty<ChatRoomRoleDefinition>()
            : new ReadOnlyCollection<ChatRoomRoleDefinition>(roleValues);
        Messages = messageValues.Length == 0
            ? Array.Empty<ChatRoomMessage>()
            : new ReadOnlyCollection<ChatRoomMessage>(messageValues);
        ConsumedThroughSequenceByRole = consumedValues;
        CurrentExecution = currentExecution;
        AutoLoop = autoLoop ?? new ChatRoomAutoLoopState(
            ChatRoomAutoLoopStatus.Idle,
            completedTurns: 0,
            maxTurns: 1,
            maxTurnsPerRole: 1);
        PersistenceHealth = persistenceHealth;
        LifecycleStatus = lifecycleStatus;
        LastPersistenceError = NormalizeOptionalValue(lastPersistenceError);
    }

    /// <summary>
    /// 房间标识。
    /// </summary>
    public Guid RoomId { get; }

    /// <summary>
    /// 当前进程内房间实例标识；每次装配或恢复都会变化。
    /// </summary>
    public Guid RoomInstanceId { get; }

    /// <summary>
    /// 房间标题。
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// 最近一次业务活动时间。
    /// </summary>
    public DateTimeOffset LastActivityAt { get; }

    /// <summary>
    /// 内存状态修订号。
    /// </summary>
    public long Revision { get; }

    /// <summary>
    /// 已确认持久化的修订号；尚未首次保存时为 -1。
    /// </summary>
    public long DurableRevision { get; }

    /// <summary>
    /// 下一条公开消息应使用的序号。
    /// </summary>
    public long NextMessageSequence { get; }

    /// <summary>
    /// 当前瞬态工作区授权版本。
    /// </summary>
    public long WorkspaceVersion { get; }

    /// <summary>
    /// 角色定义快照。
    /// </summary>
    public IReadOnlyList<ChatRoomRoleDefinition> Roles { get; }

    /// <summary>
    /// 公开消息快照。
    /// </summary>
    public IReadOnlyList<ChatRoomMessage> Messages { get; }

    /// <summary>
    /// 每个角色已消费到的消息序号。
    /// </summary>
    public IReadOnlyDictionary<string, long> ConsumedThroughSequenceByRole { get; }

    /// <summary>
    /// 当前执行状态。
    /// </summary>
    public ChatRoomExecutionState? CurrentExecution { get; }

    /// <summary>
    /// 自动循环瞬态状态。
    /// </summary>
    public ChatRoomAutoLoopState AutoLoop { get; }

    /// <summary>
    /// 持久化健康状态。
    /// </summary>
    public ChatRoomPersistenceHealth PersistenceHealth { get; }

    /// <summary>
    /// 生命周期状态。
    /// </summary>
    public ChatRoomLifecycleStatus LifecycleStatus { get; }

    /// <summary>
    /// 最近一次持久化失败摘要。
    /// </summary>
    public string? LastPersistenceError { get; }

    private static void ValidateRoles(IReadOnlyList<ChatRoomRoleDefinition> roles)
    {
        var identities = new HashSet<ChatRoomRoleIdentity>();
        var roleIds = new HashSet<string>(StringComparer.Ordinal);
        var roleNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (ChatRoomRoleDefinition role in roles)
        {
            ArgumentNullException.ThrowIfNull(role);
            if (!identities.Add(role.Identity))
            {
                throw new ArgumentException($"角色身份重复：{role.Identity.RoleId}/{role.Identity.Incarnation}。", nameof(roles));
            }
            if (!roleIds.Add(role.Identity.RoleId))
            {
                throw new ArgumentException($"角色逻辑标识重复：{role.Identity.RoleId}。", nameof(roles));
            }
            if (!roleNames.Add(role.RoleName))
            {
                throw new ArgumentException($"角色名称重复：{role.RoleName}。", nameof(roles));
            }
        }
    }

    private static void ValidateMessages(IReadOnlyList<ChatRoomMessage> messages, long nextMessageSequence)
    {
        long expectedSequence = 1;
        var messageIds = new HashSet<Guid>();
        foreach (ChatRoomMessage message in messages)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.MessageSequence != expectedSequence)
            {
                throw new ArgumentException(
                    $"消息序号必须从 1 连续递增；期望 {expectedSequence}，实际 {message.MessageSequence}。",
                    nameof(messages));
            }
            if (!messageIds.Add(message.MessageId))
            {
                throw new ArgumentException($"消息标识重复：{message.MessageId}。", nameof(messages));
            }

            expectedSequence++;
        }

        if (expectedSequence != nextMessageSequence)
        {
            throw new ArgumentException(
                $"下一消息序号必须为 {expectedSequence}，实际为 {nextMessageSequence}。",
                nameof(nextMessageSequence));
        }
    }

    private static IReadOnlyDictionary<string, long> CopyConsumedSequences(
        IReadOnlyDictionary<string, long>? values,
        IReadOnlyList<ChatRoomRoleDefinition> roles,
        long maxSequence)
    {
        if (values is null || values.Count == 0)
        {
            return new ReadOnlyDictionary<string, long>(new Dictionary<string, long>(StringComparer.Ordinal));
        }

        var roleIds = new HashSet<string>(roles.Select(role => role.Identity.RoleId), StringComparer.Ordinal);
        var copy = new Dictionary<string, long>(values.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, long> pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("消费进度的角色标识不能为空。", nameof(values));
            }
            if (!roleIds.Contains(pair.Key))
            {
                throw new ArgumentException($"消费进度引用了未知角色：{pair.Key}。", nameof(values));
            }
            if (pair.Value < 0 || pair.Value > maxSequence)
            {
                throw new ArgumentOutOfRangeException(nameof(values), pair.Value, "消费进度超出消息序号范围。");
            }

            copy.Add(pair.Key, pair.Value);
        }

        return new ReadOnlyDictionary<string, long>(copy);
    }

    private static void ValidateCurrentExecution(
        ChatRoomExecutionState? execution,
        IReadOnlyList<ChatRoomRoleDefinition> roles,
        long maxSequence,
        long workspaceVersion)
    {
        if (execution is null)
        {
            return;
        }
        ChatRoomRoleDefinition? role = roles.FirstOrDefault(role => role.Identity == execution.RoleIdentity);
        if (role is null)
        {
            throw new ArgumentException("当前执行引用了未知角色身份。", nameof(execution));
        }
        if (role.RuntimeVersion != execution.RoleRuntimeVersion)
        {
            throw new ArgumentException("当前执行引用了过期的角色运行时版本。", nameof(execution));
        }
        if (workspaceVersion != execution.WorkspaceVersion)
        {
            throw new ArgumentException("当前执行引用了过期的工作区版本。", nameof(execution));
        }
        if (execution.InputThroughSequence > maxSequence)
        {
            throw new ArgumentException("当前执行输入水位超出消息序号范围。", nameof(execution));
        }
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
