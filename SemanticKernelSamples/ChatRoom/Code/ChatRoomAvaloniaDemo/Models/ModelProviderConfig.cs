using AgentLib.Model;

using System.Collections.ObjectModel;

namespace ChatRoomAvaloniaDemo.Models;

/// <summary>
/// 模型提供商配置模型。描述一个 LLM 提供商的连接信息及其下的模型列表。
/// </summary>
public sealed class ModelProviderConfig : NotifyBase
{
    private string _providerName = string.Empty;
    private string _apiEndpoint = string.Empty;
    private string _apiKey = string.Empty;
    private string _primaryModelId = string.Empty;

    /// <summary>
    /// 提供商显示名，如 "OpenAI"、"DeepSeek"。与 <see cref="AgentLib.Core.AgentApiManagers.Contexts.ModelDefinition.Provider"/> 对应。
    /// </summary>
    public string ProviderName
    {
        get => _providerName;
        set => SetField(ref _providerName, value);
    }

    /// <summary>
    /// API 终结点地址。
    /// </summary>
    public string ApiEndpoint
    {
        get => _apiEndpoint;
        set => SetField(ref _apiEndpoint, value);
    }

    /// <summary>
    /// API 密钥。
    /// </summary>
    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    /// <summary>
    /// 主模型 ID，如 "gpt-4o"、"deepseek-v4-pro"。对应 AgentLib 中的 PrimaryModel。
    /// </summary>
    public string PrimaryModelId
    {
        get => _primaryModelId;
        set => SetField(ref _primaryModelId, value);
    }

    /// <summary>
    /// 该提供商下的模型列表。
    /// </summary>
    public ObservableCollection<ModelItemConfig> Models { get; init; } = [];
}
