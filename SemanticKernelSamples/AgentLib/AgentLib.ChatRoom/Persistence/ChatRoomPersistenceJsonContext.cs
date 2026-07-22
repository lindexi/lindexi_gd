#if !NET6_0
using System.Text.Json.Serialization;

namespace AgentLib.ChatRoom.Persistence;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(StoredChatRoomSnapshot))]
[JsonSerializable(typeof(StoredChatRoomRoleDefinition))]
[JsonSerializable(typeof(StoredChatRoomMessage))]
[JsonSerializable(typeof(StoredChatRoomRoleCheckpoint))]
[JsonSerializable(typeof(List<StoredChatRoomRoleDefinition>))]
[JsonSerializable(typeof(List<StoredChatRoomMessage>))]
[JsonSerializable(typeof(List<StoredChatRoomRoleCheckpoint>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, long>))]
internal partial class ChatRoomPersistenceJsonContext : JsonSerializerContext
{
}
#endif
