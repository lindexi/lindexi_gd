#if !NET6_0
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentLib.ChatRoom.Model;

/// <summary>
/// 角色模板的 JSON 源生成上下文。用于 <see cref="RoleTemplate"/> 及其关联类型的
/// 高性能、AOT 兼容的序列化/反序列化。
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(RoleTemplate))]
[JsonSerializable(typeof(List<RoleTemplate>))]
public partial class RoleTemplateJsonSerializerContext : JsonSerializerContext
{
}
#endif
