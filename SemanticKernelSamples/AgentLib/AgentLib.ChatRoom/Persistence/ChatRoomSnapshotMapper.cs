using AgentLib.ChatRoom.Domain;

namespace AgentLib.ChatRoom.Persistence;

internal static class ChatRoomSnapshotMapper
{
    internal const int CurrentSchemaVersion = 2;

    internal static StoredChatRoomSnapshot ToStored(ChatRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ChatRoomState state = snapshot.State;
        return new StoredChatRoomSnapshot
        {
            SchemaVersion = CurrentSchemaVersion,
            RoomId = state.RoomId,
            Title = state.Title,
            CreatedAt = state.CreatedAt,
            LastActivityAt = state.LastActivityAt,
            Revision = state.Revision,
            NextMessageSequence = state.NextMessageSequence,
            Roles = state.Roles.Select(ToStored).ToList(),
            Messages = state.Messages.Select(ToStored).ToList(),
            ConsumedThroughSequenceByRole = new Dictionary<string, long>(
                state.ConsumedThroughSequenceByRole,
                StringComparer.Ordinal),
            RoleCheckpoints = snapshot.RoleCheckpoints.Select(ToStored).ToList(),
        };
    }

    internal static ChatRoomSnapshot FromStored(StoredChatRoomSnapshot stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        if (stored.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException($"不支持的聊天室快照架构版本：{stored.SchemaVersion}。");
        }

        var state = new ChatRoomState(
            stored.RoomId,
            Guid.NewGuid(),
            stored.Title,
            stored.CreatedAt,
            stored.LastActivityAt,
            stored.Revision,
            stored.Revision,
            stored.NextMessageSequence,
            workspaceVersion: 0,
            stored.Roles.Select(FromStored),
            stored.Messages.Select(FromStored),
            stored.ConsumedThroughSequenceByRole,
            currentExecution: null,
            persistenceHealth: ChatRoomPersistenceHealth.Clean,
            lifecycleStatus: ChatRoomLifecycleStatus.Open,
            lastPersistenceError: null,
            autoLoop: null);
        return new ChatRoomSnapshot(state, stored.RoleCheckpoints.Select(FromStored));
    }

    private static StoredChatRoomRoleDefinition ToStored(ChatRoomRoleDefinition definition) => new()
    {
        RoleId = definition.Identity.RoleId,
        Incarnation = definition.Identity.Incarnation,
        ExecutionKind = (int)definition.ExecutionKind,
        RoleName = definition.RoleName,
        SystemPrompt = definition.SystemPrompt,
        IsHuman = definition.IsHuman,
        ModelProviderId = definition.ModelProviderId,
        ModelId = definition.ModelId,
        SkillFolders = definition.SkillFolders.ToList(),
        MemoryContent = definition.MemoryContent,
        ParticipationMode = (int)definition.ParticipationMode,
        IsManagerRole = definition.IsManagerRole,
        RuntimeVersion = definition.RuntimeVersion,
    };

    private static ChatRoomRoleDefinition FromStored(StoredChatRoomRoleDefinition definition)
    {
        ChatRoomRoleExecutionKind executionKind = ReadEnum<ChatRoomRoleExecutionKind>(
            definition.ExecutionKind,
            nameof(definition.ExecutionKind));
        ChatRoomParticipationMode participationMode = ReadEnum<ChatRoomParticipationMode>(
            definition.ParticipationMode,
            nameof(definition.ParticipationMode));
        return new ChatRoomRoleDefinition(
            new ChatRoomRoleIdentity(definition.RoleId, definition.Incarnation),
            executionKind,
            definition.RoleName,
            definition.SystemPrompt,
            definition.IsHuman,
            definition.ModelProviderId,
            definition.ModelId,
            definition.SkillFolders,
            definition.MemoryContent,
            participationMode,
            definition.IsManagerRole,
            definition.RuntimeVersion);
    }

    private static StoredChatRoomMessage ToStored(ChatRoomMessage message) => new()
    {
        MessageSequence = message.MessageSequence,
        MessageId = message.MessageId,
        Kind = (int)message.Kind,
        Content = message.Content,
        Timestamp = message.Timestamp,
        SenderRoleId = message.SenderRoleId,
        SenderRoleName = message.SenderRoleName,
        MentionedRoleIds = message.MentionedRoleIds.ToList(),
        ModelDisplayName = message.ModelDisplayName,
    };

    private static ChatRoomMessage FromStored(StoredChatRoomMessage message) => new(
        message.MessageSequence,
        message.MessageId,
        ReadEnum<ChatRoomMessageKind>(message.Kind, nameof(message.Kind)),
        message.Content,
        message.Timestamp,
        message.SenderRoleId,
        message.SenderRoleName,
        message.MentionedRoleIds,
        message.ModelDisplayName);

    private static StoredChatRoomRoleCheckpoint ToStored(ChatRoomRoleCheckpoint checkpoint) => new()
    {
        RoleId = checkpoint.RoleIdentity.RoleId,
        Incarnation = checkpoint.RoleIdentity.Incarnation,
        RoleRuntimeVersion = checkpoint.RoleRuntimeVersion,
        ExecutionKind = (int)checkpoint.ExecutionKind,
        CheckpointRevision = checkpoint.CheckpointRevision,
        SessionRevision = checkpoint.SessionRevision,
        ConsumedThroughSequence = checkpoint.ConsumedThroughSequence,
        SerializerVersion = checkpoint.SerializerVersion,
        Format = checkpoint.Format,
        Payload = checkpoint.Payload.ToArray(),
    };

    private static ChatRoomRoleCheckpoint FromStored(StoredChatRoomRoleCheckpoint checkpoint) => new(
        new ChatRoomRoleIdentity(checkpoint.RoleId, checkpoint.Incarnation),
        checkpoint.RoleRuntimeVersion,
        ReadEnum<ChatRoomRoleExecutionKind>(checkpoint.ExecutionKind, nameof(checkpoint.ExecutionKind)),
        checkpoint.CheckpointRevision,
        checkpoint.SessionRevision,
        checkpoint.ConsumedThroughSequence,
        checkpoint.SerializerVersion,
        checkpoint.Format,
        checkpoint.Payload);

    private static TEnum ReadEnum<TEnum>(int value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new InvalidDataException($"字段 {fieldName} 包含未知枚举值 {value}。");
        }

        return (TEnum)(object)value;
    }
}
