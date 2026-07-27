using AgentLib.Core;

namespace DeepSeekWpf.Services;

public sealed class AgentApiEndpointManagerFactory : IAgentApiEndpointManagerFactory
{
    public AgentApiEndpointManager Create()
    {
        return new AgentApiEndpointManager();
    }
}