using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Fakes;

internal sealed record FakeLanguageModel(ModelDefinition ModelDefinition, IChatClient ChatClient) : ILanguageModel
{
    public Task<IChatClient> GetChatClientAsync()
    {
        return Task.FromResult(ChatClient);
    }
}
