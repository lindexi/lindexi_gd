namespace AgentLib.ChatRoom.Persistence;

internal sealed class StoredChatRoomSnapshot
{
    public int SchemaVersion { get; set; }

    public Guid RoomId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastActivityAt { get; set; }

    public long Revision { get; set; }

    public long NextMessageSequence { get; set; }

    public List<StoredChatRoomRoleDefinition> Roles { get; set; } = [];

    public List<StoredChatRoomMessage> Messages { get; set; } = [];

    public Dictionary<string, long> ConsumedThroughSequenceByRole { get; set; } = [];

    public List<StoredChatRoomRoleCheckpoint> RoleCheckpoints { get; set; } = [];
}

internal sealed class StoredChatRoomRoleDefinition
{
    public string RoleId { get; set; } = string.Empty;

    public long Incarnation { get; set; }

    public int ExecutionKind { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string SystemPrompt { get; set; } = string.Empty;

    public bool IsHuman { get; set; }

    public string? ModelProviderId { get; set; }

    public string? ModelId { get; set; }

    public List<string> SkillFolders { get; set; } = [];

    public string? MemoryContent { get; set; }

    public int ParticipationMode { get; set; }

    public bool IsManagerRole { get; set; }

    public long RuntimeVersion { get; set; }
}

internal sealed class StoredChatRoomMessage
{
    public long MessageSequence { get; set; }

    public Guid MessageId { get; set; }

    public int Kind { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public string? SenderRoleId { get; set; }

    public string? SenderRoleName { get; set; }

    public List<string> MentionedRoleIds { get; set; } = [];

    public string? ModelDisplayName { get; set; }
}

internal sealed class StoredChatRoomRoleCheckpoint
{
    public string RoleId { get; set; } = string.Empty;

    public long Incarnation { get; set; }

    public long RoleRuntimeVersion { get; set; }

    public int ExecutionKind { get; set; }

    public long CheckpointRevision { get; set; }

    public long SessionRevision { get; set; }

    public long ConsumedThroughSequence { get; set; }

    public int SerializerVersion { get; set; }

    public string Format { get; set; } = string.Empty;

    public byte[] Payload { get; set; } = [];
}
