using System.Collections.ObjectModel;

namespace AgentLib.ChatRoom.Domain;

/// <summary>
/// 角色 committed checkpoint。
/// </summary>
public sealed record ChatRoomRoleCheckpoint
{
    private readonly byte[] _payload;

    /// <summary>
    /// 创建角色 checkpoint。
    /// </summary>
    public ChatRoomRoleCheckpoint(
        ChatRoomRoleIdentity roleIdentity,
        long roleRuntimeVersion,
        ChatRoomRoleExecutionKind executionKind,
        long checkpointRevision,
        long sessionRevision,
        long consumedThroughSequence,
        int serializerVersion,
        string format,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(roleIdentity);
        if (roleRuntimeVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleRuntimeVersion));
        }
        if (!Enum.IsDefined(typeof(ChatRoomRoleExecutionKind), executionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(executionKind));
        }
        if (checkpointRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpointRevision));
        }
        if (sessionRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionRevision));
        }
        if (consumedThroughSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consumedThroughSequence));
        }
        if (serializerVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(serializerVersion));
        }
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Checkpoint 格式不能为空或空白。", nameof(format));
        }

        RoleIdentity = roleIdentity;
        RoleRuntimeVersion = roleRuntimeVersion;
        ExecutionKind = executionKind;
        CheckpointRevision = checkpointRevision;
        SessionRevision = sessionRevision;
        ConsumedThroughSequence = consumedThroughSequence;
        SerializerVersion = serializerVersion;
        Format = format.Trim();
        _payload = payload.ToArray();
    }

    /// <summary>
    /// 角色身份。
    /// </summary>
    public ChatRoomRoleIdentity RoleIdentity { get; }

    /// <summary>
    /// 生成 checkpoint 的角色运行时版本。
    /// </summary>
    public long RoleRuntimeVersion { get; }

    /// <summary>
    /// 执行种类。
    /// </summary>
    public ChatRoomRoleExecutionKind ExecutionKind { get; }

    /// <summary>
    /// checkpoint 所对应的房间修订号。
    /// </summary>
    public long CheckpointRevision { get; }

    /// <summary>
    /// 角色私有会话修订号。
    /// </summary>
    public long SessionRevision { get; }

    /// <summary>
    /// 该 checkpoint 已消费到的公开消息序号。
    /// </summary>
    public long ConsumedThroughSequence { get; }

    /// <summary>
    /// checkpoint 载荷序列化器版本。
    /// </summary>
    public int SerializerVersion { get; }

    /// <summary>
    /// checkpoint 编码格式。
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// 返回 checkpoint 载荷的隔离副本。
    /// </summary>
    public ReadOnlyMemory<byte> Payload => _payload.ToArray();
}

/// <summary>
/// 尚未被协调器接受的角色 checkpoint 候选。
/// </summary>
public sealed record ChatRoomRoleCheckpointCandidate
{
    private readonly byte[] _payload;

    /// <summary>
    /// 创建 checkpoint 候选。
    /// </summary>
    public ChatRoomRoleCheckpointCandidate(
        ChatRoomRoleIdentity roleIdentity,
        long roleRuntimeVersion,
        ChatRoomRoleExecutionKind executionKind,
        long sessionRevision,
        long consumedThroughSequence,
        int serializerVersion,
        string format,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(roleIdentity);
        if (roleRuntimeVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleRuntimeVersion));
        }
        if (!Enum.IsDefined(typeof(ChatRoomRoleExecutionKind), executionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(executionKind));
        }
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Checkpoint 格式不能为空或空白。", nameof(format));
        }
        if (sessionRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionRevision));
        }
        if (consumedThroughSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consumedThroughSequence));
        }
        if (serializerVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(serializerVersion));
        }

        RoleIdentity = roleIdentity;
        RoleRuntimeVersion = roleRuntimeVersion;
        ExecutionKind = executionKind;
        SessionRevision = sessionRevision;
        ConsumedThroughSequence = consumedThroughSequence;
        SerializerVersion = serializerVersion;
        Format = format.Trim();
        _payload = payload.ToArray();
    }

    /// <summary>
    /// 角色身份。
    /// </summary>
    public ChatRoomRoleIdentity RoleIdentity { get; }

    /// <summary>
    /// 生成候选的角色运行时版本。
    /// </summary>
    public long RoleRuntimeVersion { get; }

    /// <summary>
    /// 执行种类。
    /// </summary>
    public ChatRoomRoleExecutionKind ExecutionKind { get; }

    /// <summary>
    /// 候选角色私有会话修订号。
    /// </summary>
    public long SessionRevision { get; }

    /// <summary>
    /// 候选已消费到的公开消息序号。
    /// </summary>
    public long ConsumedThroughSequence { get; }

    /// <summary>
    /// 候选载荷序列化器版本。
    /// </summary>
    public int SerializerVersion { get; }

    /// <summary>
    /// checkpoint 编码格式。
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// 返回候选载荷的隔离副本。
    /// </summary>
    public ReadOnlyMemory<byte> Payload => _payload.ToArray();

    /// <summary>
    /// 在协调器接受候选后创建 committed checkpoint。
    /// </summary>
    public ChatRoomRoleCheckpoint Commit(long checkpointRevision) => new(
        RoleIdentity,
        RoleRuntimeVersion,
        ExecutionKind,
        checkpointRevision,
        SessionRevision,
        ConsumedThroughSequence,
        SerializerVersion,
        Format,
        _payload);
}

/// <summary>
/// 房间公开状态与全部 committed checkpoint 的一致快照。
/// </summary>
public sealed record ChatRoomSnapshot
{
    /// <summary>
    /// 创建一致快照。
    /// </summary>
    public ChatRoomSnapshot(
        ChatRoomState state,
        IEnumerable<ChatRoomRoleCheckpoint>? roleCheckpoints = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ChatRoomRoleCheckpoint[] checkpointValues = roleCheckpoints?.ToArray()
            ?? Array.Empty<ChatRoomRoleCheckpoint>();
        ValidateCheckpoints(state, checkpointValues);

        State = state;
        RoleCheckpoints = checkpointValues.Length == 0
            ? Array.Empty<ChatRoomRoleCheckpoint>()
            : new ReadOnlyCollection<ChatRoomRoleCheckpoint>(checkpointValues);
    }

    /// <summary>
    /// 公开领域状态。
    /// </summary>
    public ChatRoomState State { get; }

    /// <summary>
    /// 角色 committed checkpoint 集合。
    /// </summary>
    public IReadOnlyList<ChatRoomRoleCheckpoint> RoleCheckpoints { get; }

    /// <summary>
    /// 创建与当前快照完全隔离的副本。
    /// </summary>
    public ChatRoomSnapshot DeepClone()
    {
        var state = new ChatRoomState(
            State.RoomId,
            State.RoomInstanceId,
            State.Title,
            State.CreatedAt,
            State.LastActivityAt,
            State.Revision,
            State.DurableRevision,
            State.NextMessageSequence,
            State.WorkspaceVersion,
            State.Roles.Select(CloneRole),
            State.Messages.Select(CloneMessage),
            new Dictionary<string, long>(State.ConsumedThroughSequenceByRole, StringComparer.Ordinal),
            CloneExecution(State.CurrentExecution),
            State.PersistenceHealth,
            State.LifecycleStatus,
            State.LastPersistenceError,
            new ChatRoomAutoLoopState(
                State.AutoLoop.Status,
                State.AutoLoop.CompletedTurns,
                State.AutoLoop.MaxTurns,
                State.AutoLoop.MaxTurnsPerRole));
        return new ChatRoomSnapshot(
            state,
            RoleCheckpoints.Select(checkpoint => new ChatRoomRoleCheckpoint(
                new ChatRoomRoleIdentity(
                    checkpoint.RoleIdentity.RoleId,
                    checkpoint.RoleIdentity.Incarnation),
                checkpoint.RoleRuntimeVersion,
                checkpoint.ExecutionKind,
                checkpoint.CheckpointRevision,
                checkpoint.SessionRevision,
                checkpoint.ConsumedThroughSequence,
                checkpoint.SerializerVersion,
                checkpoint.Format,
                checkpoint.Payload)));
    }

    private static ChatRoomRoleDefinition CloneRole(ChatRoomRoleDefinition role) => new(
        new ChatRoomRoleIdentity(role.Identity.RoleId, role.Identity.Incarnation),
        role.ExecutionKind,
        role.RoleName,
        role.SystemPrompt,
        role.IsHuman,
        role.ModelProviderId,
        role.ModelId,
        role.SkillFolders,
        role.MemoryContent,
        role.ParticipationMode,
        role.IsManagerRole,
        role.RuntimeVersion);

    private static ChatRoomMessage CloneMessage(ChatRoomMessage message) => new(
        message.MessageSequence,
        message.MessageId,
        message.Kind,
        message.Content,
        message.Timestamp,
        message.SenderRoleId,
        message.SenderRoleName,
        message.MentionedRoleIds,
        message.ModelDisplayName);

    private static ChatRoomExecutionState? CloneExecution(ChatRoomExecutionState? execution) =>
        execution is null
            ? null
            : new ChatRoomExecutionState(
                execution.ExecutionId,
                new ChatRoomRoleIdentity(
                    execution.RoleIdentity.RoleId,
                    execution.RoleIdentity.Incarnation),
                execution.RoleRuntimeVersion,
                execution.WorkspaceVersion,
                execution.InputThroughSequence,
                execution.Status,
                execution.StartedAt,
                execution.ApprovalId,
                execution.FailureMessage);

    private static void ValidateCheckpoints(
        ChatRoomState state,
        IReadOnlyList<ChatRoomRoleCheckpoint> checkpoints)
    {
        var definitions = state.Roles.ToDictionary(role => role.Identity);
        var identities = new HashSet<ChatRoomRoleIdentity>();
        foreach (ChatRoomRoleCheckpoint checkpoint in checkpoints)
        {
            ArgumentNullException.ThrowIfNull(checkpoint);
            if (!identities.Add(checkpoint.RoleIdentity))
            {
                throw new ArgumentException(
                    $"角色 checkpoint 重复：{checkpoint.RoleIdentity.RoleId}/{checkpoint.RoleIdentity.Incarnation}。",
                    nameof(checkpoints));
            }
            if (!definitions.TryGetValue(checkpoint.RoleIdentity, out ChatRoomRoleDefinition? definition))
            {
                throw new ArgumentException(
                    $"角色 checkpoint 引用了未知身份：{checkpoint.RoleIdentity.RoleId}/{checkpoint.RoleIdentity.Incarnation}。",
                    nameof(checkpoints));
            }
            if (checkpoint.ExecutionKind != definition.ExecutionKind)
            {
                throw new ArgumentException(
                    $"角色 {checkpoint.RoleIdentity.RoleId} 的定义与 checkpoint 执行种类不一致。",
                    nameof(checkpoints));
            }
            if (checkpoint.RoleRuntimeVersion != definition.RuntimeVersion)
            {
                throw new ArgumentException(
                    $"角色 {checkpoint.RoleIdentity.RoleId} 的定义与 checkpoint 运行时版本不一致。",
                    nameof(checkpoints));
            }
            if (checkpoint.CheckpointRevision > state.Revision)
            {
                throw new ArgumentException(
                    $"角色 {checkpoint.RoleIdentity.RoleId} 的 checkpoint 修订号超出房间修订号。",
                    nameof(checkpoints));
            }
            if (checkpoint.ConsumedThroughSequence > state.NextMessageSequence - 1)
            {
                throw new ArgumentException(
                    $"角色 {checkpoint.RoleIdentity.RoleId} 的 checkpoint 消费水位超出消息范围。",
                    nameof(checkpoints));
            }
            long expectedConsumedThroughSequence = state.ConsumedThroughSequenceByRole.TryGetValue(
                checkpoint.RoleIdentity.RoleId,
                out long consumedThroughSequence)
                ? consumedThroughSequence
                : 0;
            if (checkpoint.ConsumedThroughSequence != expectedConsumedThroughSequence)
            {
                throw new ArgumentException(
                    $"角色 {checkpoint.RoleIdentity.RoleId} 的 checkpoint 与公开消费水位不一致。",
                    nameof(checkpoints));
            }
        }
    }
}
