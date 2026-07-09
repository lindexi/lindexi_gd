#if !NET6_0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentLib.ChatRoom.Model;

/// <summary>
/// 聊天室持久化的 JSON 源生成上下文。用于 <see cref="ChatRoomSessionData"/> 及其关联类型的
/// 高性能、AOT 兼容的序列化/反序列化。
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ChatRoomSessionData))]
[JsonSerializable(typeof(List<ChatRoomMessage>))]
[JsonSerializable(typeof(List<ChatRoomRoleDefinition>))]
[JsonSerializable(typeof(List<ToolDefinition>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(JsonElement))]
public partial class ChatRoomJsonSerializerContext : JsonSerializerContext
{
}
#endif
