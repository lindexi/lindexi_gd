#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.DeepSeek;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentLib.Core.AgentApiManagers;

internal static class ChatClientCreator
{
    public static IChatClient CreateChatClient
    (
        ApiEndpoint apiEndpoint,
        IHttpClientProvider? httpClientProvider = null
    )
    {
        if (apiEndpoint.IsDeepSeek())
        {
            var deepSeekChatClient = new DeepSeekChatClient(apiEndpoint.Key, apiEndpoint.ModelId, apiEndpoint.EndPoint);
            IChatClient chatClient = deepSeekChatClient;
            return chatClient;
        }

        return OpenAIClientCreator.CreateOpenAIClient(apiEndpoint, httpClientProvider);
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

internal static class OpenAIClientCreator
{
    public static IChatClient CreateOpenAIClient
    (
        ApiEndpoint apiEndpoint,
        IHttpClientProvider? httpClientProvider
    )
    {
        var openAiClient = CreateClient(apiEndpoint, httpClientProvider);
        ChatClient chatClient = openAiClient.GetChatClient(apiEndpoint.ModelId);
        return chatClient.AsIChatClient();
    }

    public static OpenAI.Responses.ResponsesClient CreateResponsesClient
    (
        ApiEndpoint apiEndpoint,
        IHttpClientProvider? httpClientProvider
    )
    {
        return CreateClient(apiEndpoint, httpClientProvider).GetResponsesClient();
    }

    private static OpenAIClient CreateClient(ApiEndpoint apiEndpoint, IHttpClientProvider? httpClientProvider)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(apiEndpoint.EndPoint),
        };
        HttpClient httpClient = httpClientProvider?.HttpClient ?? new HttpClient();
        options.Transport = new HttpClientPipelineTransport(httpClient);

        return new OpenAIClient(new ApiKeyCredential(apiEndpoint.Key), options);
    }
}