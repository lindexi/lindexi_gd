using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Core;

public static class AgentApiManagerConfigurationExtension
{
    public static async Task LoadConfigurationFromJsonFileAsync(this AgentApiEndpointManager agentApiEndpointManager, FileInfo file)
    {
        var configuration = await AgentApiManagerConfiguration.FromJsonFileAsync(file);
        agentApiEndpointManager.LoadConfiguration(configuration);
    }
}