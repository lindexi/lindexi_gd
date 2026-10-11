#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

using OpenAI.Responses;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 将 Responses 客户端与当前模型已解析的 API 标识作为一个不可变结果返回。
/// </summary>
public sealed record ResponsesClientContext
{
    /// <summary>
    /// 创建客户端上下文；客户端及其 HTTP 传输的生命周期仍由提供商管理。
    /// </summary>
    public ResponsesClientContext(ResponsesClient client, string modelId)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException(null, nameof(modelId));
        }
        Client = client;
        ModelId = modelId;
    }

    /// <summary>
    /// 获取原生 Responses 客户端。
    /// </summary>
    public ResponsesClient Client { get; }

    /// <summary>
    /// 获取与客户端一同解析的 API 模型标识。
    /// </summary>
    public string ModelId { get; }
}
