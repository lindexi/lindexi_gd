using AgentLib.ChatRoom.Domain;

using System.Text.Json;

namespace AgentLib.ChatRoom.Persistence;

internal static class ChatRoomSnapshotSerializer
{
#if NET6_0
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
#endif

    internal static byte[] Serialize(ChatRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StoredChatRoomSnapshot stored = ChatRoomSnapshotMapper.ToStored(snapshot);
#if NET6_0
        return JsonSerializer.SerializeToUtf8Bytes(stored, SerializerOptions);
#else
        return JsonSerializer.SerializeToUtf8Bytes(
            stored,
            ChatRoomPersistenceJsonContext.Default.StoredChatRoomSnapshot);
#endif
    }

    internal static ChatRoomSnapshot Deserialize(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            throw new ArgumentException("快照载荷不能为空。", nameof(payload));
        }

#if NET6_0
        StoredChatRoomSnapshot? stored = JsonSerializer.Deserialize<StoredChatRoomSnapshot>(payload, SerializerOptions);
#else
        StoredChatRoomSnapshot? stored = JsonSerializer.Deserialize(
            payload,
            ChatRoomPersistenceJsonContext.Default.StoredChatRoomSnapshot);
#endif
        if (stored is null)
        {
            throw new InvalidDataException("聊天室快照载荷不包含根对象。");
        }

        return ChatRoomSnapshotMapper.FromStored(stored);
    }
}
