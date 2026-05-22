using AgentLib.Core.AgentApiManagers.Contexts;
using Microsoft.Extensions.AI;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public interface ILanguageModel
{
    ModelDefinition ModelDefinition { get; }
    Task<IChatClient> GetChatClientAsync();
}