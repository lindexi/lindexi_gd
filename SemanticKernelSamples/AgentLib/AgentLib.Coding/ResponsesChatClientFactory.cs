using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Coding;

/// <summary>
/// 创建使用 OpenAI Responses API 的 <see cref="IChatClient"/>。
/// </summary>
public static class ResponsesChatClientFactory
{
    /// <summary>
    /// 使用指定 OpenAI 兼容终结点创建 Responses API 聊天客户端。
    /// </summary>
    /// <param name="endpoint">API 服务终结点。</param>
    /// <param name="apiKey">API 密钥。</param>
    /// <param name="model">模型 ID。</param>
    /// <returns>基于 Responses API 的聊天客户端。</returns>
    public static IChatClient Create(string endpoint, string apiKey, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var client = new ResponsesClient(
            new ApiKeyCredential(apiKey),
            new ResponsesClientOptions { Endpoint = new Uri(endpoint) });
        return client.AsIChatClient();
    }
}
