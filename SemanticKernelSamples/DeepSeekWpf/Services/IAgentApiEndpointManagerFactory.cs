using AgentLib.Core;

namespace DeepSeekWpf.Services;

public interface IAgentApiEndpointManagerFactory
{
    AgentApiEndpointManager Create();
}