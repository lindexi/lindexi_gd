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

        AgentApiConfiguration agentApiConfiguration = GetAgentApiConfiguration();
        if (agentApiConfiguration.IsInvalidAgentApiConfiguration())
        {
            return default;
        }

        return new ApiEndpoint(agentApiConfiguration.EndPoint, agentApiConfiguration.Key, agentApiConfiguration.ModelName);
    }

    private AgentApiConfiguration GetAgentApiConfiguration()
    {
        AgentApiConfiguration agentApiConfiguration = appConfigurator.Of<AgentApiConfiguration>();

        if (agentApiConfiguration.SelectedVendor is null)
        {
            return agentApiConfiguration;
        }

        if (!Enum.TryParse(agentApiConfiguration.SelectedVendor.ToString(),ignoreCase:true,out ModelVendor modelVendor))
        {
            return agentApiConfiguration;
        }

        return modelVendor switch
        {
            ModelVendor.DouBao => appConfigurator.Of<DouBaoAgentApiConfiguration>(),
            ModelVendor.Deepseek => appConfigurator.Of<DeepSeekAgentApiConfiguration>(),
            _ => agentApiConfiguration
        };
    }
}