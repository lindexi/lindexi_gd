using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;

namespace AvaloniaAgentLib.Core;

public class AgentApiEndpointManager
{
    public IApiEndpointProvider? ApiEndpointProvider { get; set; }

    public IChatClient CreateOpenAIClient()
    {
        var apiEndpoint = ApiEndpointProvider?.GetApiEndpoint();
        if (apiEndpoint is null)
        {
            throw new InvalidOperationException($"必须先设置才能创建");
        }

        return CreateOpenAIClient(apiEndpoint.Value);
    }

    public static IChatClient CreateOpenAIClient(ApiEndpoint apiEndpoint)
    {
        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiEndpoint.Key), new OpenAIClientOptions()
        {
            Endpoint = new Uri(apiEndpoint.EndPoint)
        });

        ChatClient chatClient = openAiClient.GetChatClient(apiEndpoint.ModelName);
        return chatClient.AsIChatClient();
    }
}