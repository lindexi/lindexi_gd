using AgentLib.Core.AgentApiManagers.Contexts;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// OpenAI 协议语言模型配置，包含终结点、密钥和模型定义列表。
/// </summary>
/// <param name="EndPoint">API 终结点地址。</param>
/// <param name="Key">API 密钥。</param>
public record OpenAIProtocolLanguageModelConfiguration(string EndPoint, string Key)
{
    /// <summary>
    /// 该终结点下支持的模型定义列表。
    /// </summary>
    public IReadOnlyList<ModelDefinition>? ModelDefinitions { get; init; }
}