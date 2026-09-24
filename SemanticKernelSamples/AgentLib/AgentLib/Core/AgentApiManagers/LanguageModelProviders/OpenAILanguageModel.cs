using AgentLib.Core.AgentApiManagers.Contexts;
using Microsoft.Extensions.AI;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

record OpenAILanguageModel
(
    ModelDefinition ModelDefinition,
    ApiEndpoint ApiEndpoint,
    IHttpClientProvider? HttpClientProvider = null
) : ILanguageModel
{
    public Task<IChatClient> GetChatClientAsync()
    {
        IChatClient chatClient = ChatClientCreator.CreateChatClient(ApiEndpoint, HttpClientProvider);
        return Task.FromResult(chatClient);
    }
}