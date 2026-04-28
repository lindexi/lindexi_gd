using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;

namespace AvaloniaAgentLib.Core;

public class AgentApiEndpointManager
{
    public ApiEndpoint CurrentEndpoint { get; set; }

    public IChatClient CreateOpenAIClient()
    {
        var apiEndpoint = CurrentEndpoint;
        if (string.IsNullOrEmpty(apiEndpoint.EndPoint))
        {
            throw new InvalidOperationException($"必须先设置才能创建");
        }

        return CreateOpenAIClient(apiEndpoint);
    }

    public static IChatClient CreateOpenAIClient(ApiEndpoint apiEndpoint)
    {
        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiEndpoint.Key), new OpenAIClientOptions()
        {
            Endpoint = new Uri(apiEndpoint.EndPoint),
        });

        ChatClient chatClient = openAiClient.GetChatClient(apiEndpoint.ModelName);
        return chatClient.AsIChatClient();
    }
}