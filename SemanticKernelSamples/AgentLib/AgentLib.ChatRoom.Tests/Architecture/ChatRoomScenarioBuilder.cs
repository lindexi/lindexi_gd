using AgentLib.ChatRoom.Domain;

namespace AgentLib.ChatRoom.Tests.Architecture;

internal sealed class ChatRoomScenarioBuilder
{
    private readonly Guid _roomId = Guid.NewGuid();
    private readonly Guid _roomInstanceId = Guid.NewGuid();
    private readonly DateTimeOffset _createdAt = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
    private readonly List<ChatRoomRoleDefinition> _roles = [];
    private readonly List<ChatRoomMessage> _messages = [];
    private readonly List<ChatRoomRoleCheckpoint> _checkpoints = [];
    private readonly Dictionary<string, long> _consumedThroughSequenceByRole = new(StringComparer.Ordinal);
    private string _title = "测试聊天室";
    private long _revision;
    private long _durableRevision = -1;
    private long _workspaceVersion;
    private ChatRoomExecutionState? _currentExecution;
    private ChatRoomPersistenceHealth _persistenceHealth = ChatRoomPersistenceHealth.Clean;
    private ChatRoomLifecycleStatus _lifecycleStatus = ChatRoomLifecycleStatus.Open;
    private string? _lastPersistenceError;

    internal ChatRoomScenarioBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    internal ChatRoomScenarioBuilder WithRevision(long revision)
    {
        _revision = revision;
        _durableRevision = revision;
        return this;
    }

    internal ChatRoomScenarioBuilder WithDurableRevision(long durableRevision)
    {
        _durableRevision = durableRevision;
        return this;
    }

    internal ChatRoomScenarioBuilder WithWorkspaceVersion(long workspaceVersion)
    {
        _workspaceVersion = workspaceVersion;
        return this;
    }

    internal ChatRoomScenarioBuilder WithRole(
        string roleId = "assistant",
        string roleName = "助手",
        ChatRoomRoleExecutionKind executionKind = ChatRoomRoleExecutionKind.Standard,
        long incarnation = 0,
        bool isHuman = false,
        long runtimeVersion = 0)
    {
        _roles.Add(new ChatRoomRoleDefinition(
            new ChatRoomRoleIdentity(roleId, incarnation),
            executionKind,
            roleName,
            $"你是{roleName}。",
            isHuman,
            runtimeVersion: runtimeVersion));
        return this;
    }

    internal ChatRoomScenarioBuilder WithMessage(
        string content,
        string senderRoleId = "human",
        string senderRoleName = "用户",
        ChatRoomMessageKind kind = ChatRoomMessageKind.Human)
    {
        long sequence = _messages.Count + 1L;
        _messages.Add(new ChatRoomMessage(
            sequence,
            Guid.NewGuid(),
            kind,
            content,
            _createdAt.AddMinutes(sequence),
            kind == ChatRoomMessageKind.System ? null : senderRoleId,
            kind == ChatRoomMessageKind.System ? null : senderRoleName));
        return this;
    }

    internal ChatRoomScenarioBuilder WithConsumedSequence(string roleId, long sequence)
    {
        _consumedThroughSequenceByRole[roleId] = sequence;
        return this;
    }

    internal ChatRoomScenarioBuilder WithCheckpoint(
        string roleId = "assistant",
        long incarnation = 0,
        ChatRoomRoleExecutionKind executionKind = ChatRoomRoleExecutionKind.Standard,
        byte[]? payload = null,
        long roleRuntimeVersion = 0,
        long sessionRevision = 1,
        int serializerVersion = 1)
    {
        _checkpoints.Add(new ChatRoomRoleCheckpoint(
            new ChatRoomRoleIdentity(roleId, incarnation),
            roleRuntimeVersion,
            executionKind,
            _revision,
            sessionRevision,
            _consumedThroughSequenceByRole.TryGetValue(roleId, out long consumedThroughSequence)
                ? consumedThroughSequence
                : 0,
            serializerVersion,
            "application/json",
            payload ?? [1, 2, 3]));
        return this;
    }

    internal ChatRoomScenarioBuilder WithCurrentExecution(
        string roleId = "assistant",
        long incarnation = 0,
        ChatRoomExecutionStatus status = ChatRoomExecutionStatus.Running,
        long roleRuntimeVersion = 0)
    {
        _currentExecution = new ChatRoomExecutionState(
            Guid.NewGuid(),
            new ChatRoomRoleIdentity(roleId, incarnation),
            roleRuntimeVersion,
            _workspaceVersion,
            _messages.Count,
            status,
            _createdAt.AddMinutes(_messages.Count + 1));
        return this;
    }

    internal ChatRoomScenarioBuilder WithPersistenceFault(string error)
    {
        _persistenceHealth = ChatRoomPersistenceHealth.Faulted;
        _lastPersistenceError = error;
        return this;
    }

    internal ChatRoomScenarioBuilder WithLifecycle(ChatRoomLifecycleStatus status)
    {
        _lifecycleStatus = status;
        return this;
    }

    internal ChatRoomSnapshot Build()
    {
        DateTimeOffset lastActivityAt = _messages.Count == 0
            ? _createdAt
            : _messages[^1].Timestamp;
        var state = new ChatRoomState(
            _roomId,
            _roomInstanceId,
            _title,
            _createdAt,
            lastActivityAt,
            _revision,
            _durableRevision,
            _messages.Count + 1L,
            _workspaceVersion,
            _roles,
            _messages,
            _consumedThroughSequenceByRole,
            _currentExecution,
            _persistenceHealth,
            _lifecycleStatus,
            _lastPersistenceError);
        return new ChatRoomSnapshot(state, _checkpoints);
    }
}
