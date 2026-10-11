#pragma warning disable OPENAI001 // Responses SDK API 当前标记为实验性。

using AgentLib.Core.AgentApiManagers.Contexts;
using Microsoft.Extensions.AI;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

record OpenAILanguageModel
(
    ModelDefinition ModelDefinition,
    ApiEndpoint ApiEndpoint,
    IHttpClientProvider? HttpClientProvider = null
) : ILanguageModel, IResponsesClientProvider
{
    /// <inheritdoc />
    public Task<OpenAI.Responses.ResponsesClient> GetResponsesClientAsync()
    {
        return Task.FromResult(OpenAIClientCreator.CreateResponsesClient(ApiEndpoint, HttpClientProvider));
    }

    public Task<IChatClient> GetChatClientAsync()
    {
        IChatClient chatClient = ChatClientCreator.CreateChatClient(ApiEndpoint, HttpClientProvider);
        return Task.FromResult(chatClient);
    }
}