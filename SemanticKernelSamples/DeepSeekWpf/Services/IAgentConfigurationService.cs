using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace DeepSeekWpf.Services;

public interface IAgentConfigurationService
{
    string ConfigurationFilePath { get; }

    Task<AgentConfigurationLoadResult> LoadAsync();

    void EnsureTemplateExists();
}

public sealed record AgentConfigurationLoadResult(
    AgentApiManagerConfiguration Configuration,
    string SourceDescription,
    bool IsDebugFallback);
