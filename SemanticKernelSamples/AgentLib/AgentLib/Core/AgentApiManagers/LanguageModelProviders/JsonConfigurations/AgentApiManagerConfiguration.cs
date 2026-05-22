namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public record AgentApiManagerConfiguration
{
    public string? PrimaryModel { get; init; }

    public IReadOnlyList<OpenAIProtocolLanguageModelConfiguration>? OpenAIConfigurationList { get; init; }
}