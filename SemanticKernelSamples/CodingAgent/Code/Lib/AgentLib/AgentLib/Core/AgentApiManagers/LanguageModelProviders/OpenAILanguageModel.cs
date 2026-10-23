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
    public Task<ResponsesClientContext> GetResponsesClientAsync()
    {
        var client = OpenAIClientCreator.CreateResponsesClient(ApiEndpoint, HttpClientProvider);
        return Task.FromResult(new ResponsesClientContext(client, ApiEndpoint.ModelId));
    }

    public Task<IChatClient> GetChatClientAsync()
    {
        IChatClient chatClient = ChatClientCreator.CreateChatClient(ApiEndpoint, HttpClientProvider);
        return Task.FromResult(chatClient);
    }
}