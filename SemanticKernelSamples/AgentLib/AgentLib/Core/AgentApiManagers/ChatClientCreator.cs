using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.DeepSeek;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentLib.Core.AgentApiManagers;

internal static class ChatClientCreator
{
    public static IChatClient CreateChatClient(ApiEndpoint apiEndpoint)
    {
        if (apiEndpoint.IsDeepSeek())
        {
            var deepSeekChatClient = new DeepSeekChatClient(apiEndpoint.Key, apiEndpoint.ModelId, apiEndpoint.EndPoint);
            IChatClient chatClient = deepSeekChatClient;
            return chatClient;
        }

        return OpenAIClientCreator.CreateOpenAIClient(apiEndpoint);
    }

    /// <summary>
    /// 是否 DeepSeek 服务，如果是的话，应该走另一条路线
    /// </summary>
    /// <param name="apiEndpoint"></param>
    /// <returns></returns>
    private static bool IsDeepSeek(this ApiEndpoint apiEndpoint)
    {
        return apiEndpoint.EndPoint?.Contains("deepseek.com") is true;
    }
}

file static class OpenAIClientCreator
{
    public static IChatClient CreateOpenAIClient(ApiEndpoint apiEndpoint)
    {
        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiEndpoint.Key), new OpenAIClientOptions()
        {
            Endpoint = new Uri(apiEndpoint.EndPoint)
        });

        ChatClient chatClient = openAiClient.GetChatClient(apiEndpoint.ModelId);
        return chatClient.AsIChatClient();
    }
}
