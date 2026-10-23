#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

using OpenAI.Responses;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 包含原生 Responses 客户端及与 Chat API 相同的已解析模型标识的不可变上下文。
/// </summary>
public sealed record ResponsesClientContext
{
    /// <summary>
    /// 创建客户端上下文，不改变底层 HTTP 传输的所有权和生命周期。
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
    /// 获取当前客户端上下文的 API 模型标识。
    /// </summary>
    public string ModelId { get; }
}
