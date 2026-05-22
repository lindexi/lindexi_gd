using AgentLib.Core.AgentApiManagers.Contexts;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public record OpenAIProtocolLanguageModelConfiguration(string EndPoint, string Key)
{
    public IReadOnlyList<ModelDefinition>? ModelDefinitions { get; init; }
}