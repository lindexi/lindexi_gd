using AgentLib.Core.AgentApiManagers.Contexts;
using Microsoft.Extensions.AI;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

record OpenAILanguageModel(ModelDefinition ModelDefinition, ApiEndpoint ApiEndpoint) : ILanguageModel
{
    public Task<IChatClient> GetChatClientAsync()
    {
        var chatClient = ChatClientCreator.CreateChatClient(ApiEndpoint);
        return Task.FromResult(chatClient);
    }
}