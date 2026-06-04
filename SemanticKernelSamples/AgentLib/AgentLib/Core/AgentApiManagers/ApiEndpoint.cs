namespace AgentLib.Core.AgentApiManagers;

/// <summary>
/// 表示 API 终结点配置，包含终结点地址、密钥和模型 ID。
/// </summary>
/// <param name="EndPoint">API 终结点地址。</param>
/// <param name="Key">API 密钥。</param>
/// <param name="ModelId">模型 ID。</param>
public readonly record struct ApiEndpoint(string EndPoint, string Key, string ModelId);
