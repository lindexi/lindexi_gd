using System;

using AvaloniaAgentLib.Core;

using dotnetCampus.Configurations;

using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.Views.Components;

namespace SimpleWrite.Business.AgentConnectors;

sealed class AgentApiConfigurationApiEndpointProvider(IAppConfigurator appConfigurator) : IApiEndpointProvider
{
    /// <summary>
    /// 从当前配置读取 API 终结点。
    /// </summary>
    public ApiEndpoint GetApiEndpoint()
    {
        ArgumentNullException.ThrowIfNull(appConfigurator);

        AgentApiConfiguration agentApiConfiguration = appConfigurator.Of<AgentApiConfiguration>();
        if (RightSlideBar.IsInvalidAgentApiConfiguration(agentApiConfiguration))
        {
            return default;
        }

        return new ApiEndpoint(agentApiConfiguration.EndPoint, agentApiConfiguration.Key, agentApiConfiguration.ModelName);
    }
}