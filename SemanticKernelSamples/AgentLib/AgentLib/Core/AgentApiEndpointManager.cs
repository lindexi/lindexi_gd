using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.DeepSeek;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;

namespace AgentLib.Core;

public class AgentApiEndpointManager
{
    public IApiEndpointProvider? ApiEndpointProvider { get; set; }

    /// <summary>
    /// 根据当前配置创建聊天客户端。
    /// </summary>
    public IChatClient CreateChatClient()
    {
        var apiEndpoint = ApiEndpointProvider?.GetApiEndpoint();
        if (string.IsNullOrWhiteSpace(apiEndpoint?.EndPoint))
        {
            throw new InvalidOperationException("必须先设置才能创建");
        }

        return CreateChatClient(apiEndpoint.Value);
    }

    /// <summary>
    /// 根据 API 终结点配置创建聊天客户端。
    /// </summary>
    public static IChatClient CreateChatClient(ApiEndpoint apiEndpoint)
    {
        if (string.IsNullOrWhiteSpace(apiEndpoint.EndPoint))
        {
            throw new ArgumentException("API 终结点不能为空。", nameof(apiEndpoint));
        }

        if (string.IsNullOrWhiteSpace(apiEndpoint.Key))
        {
            throw new ArgumentException("API Key 不能为空。", nameof(apiEndpoint));
        }

        if (string.IsNullOrWhiteSpace(apiEndpoint.ModelName))
        {
            throw new ArgumentException("模型名称不能为空。", nameof(apiEndpoint));
        }

        if (!Uri.TryCreate(apiEndpoint.EndPoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("API 终结点必须是绝对地址。", nameof(apiEndpoint));
        }

        if (IsDeepSeekEndpoint(endpointUri))
        {
            return new DeepSeekChatClient(apiEndpoint.Key, apiEndpoint.ModelName, apiEndpoint.EndPoint);
        }

        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiEndpoint.Key), new OpenAIClientOptions()
        {
            Endpoint = endpointUri
        });

        ChatClient chatClient = openAiClient.GetChatClient(apiEndpoint.ModelName);
        return chatClient.AsIChatClient();
    }

    private static bool IsDeepSeekEndpoint(Uri endpointUri)
    {
        return endpointUri.Host.Equals("api.deepseek.com", StringComparison.OrdinalIgnoreCase)
               || endpointUri.Host.Contains(".deepseek.com", StringComparison.OrdinalIgnoreCase);
    }
}
