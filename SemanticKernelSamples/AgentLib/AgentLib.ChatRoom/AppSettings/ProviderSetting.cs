using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentLib.ChatRoom.Configuration;

/// <summary>
/// 模型提供商配置项。
/// </summary>
public sealed class ProviderSetting
{
    /// <summary>
    /// 提供商名称（对应 <see cref="AgentLib.Core.AgentApiManagers.Contexts.ModelDefinition.Provider"/>）。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API 终结点地址。
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API 密钥。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 该提供商下的模型列表。
    /// </summary>
    public List<ModelSetting> Models { get; init; } = [];
}
