using System;

namespace SimpleWrite.Business.SimpleWriteConfigurations;

static class AgentApiConfigurationVerifier
{
    public static bool IsInvalidAgentApiConfiguration(this AgentApiConfiguration agentApiConfiguration)
    {
        ArgumentNullException.ThrowIfNull(agentApiConfiguration);

        if (string.IsNullOrEmpty(agentApiConfiguration.EndPoint)
            || string.IsNullOrEmpty(agentApiConfiguration.Key)
            || string.IsNullOrEmpty(agentApiConfiguration.ModelName))
        {
            return true;
        }

        if (agentApiConfiguration.EndPoint == EndPointHelpText)
        {
            return true;
        }

        string endPoint = agentApiConfiguration.EndPoint;
        if (!endPoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (agentApiConfiguration.Key == KeyHelpText)
        {
            return true;
        }

        if (agentApiConfiguration.ModelName == ModelNameHelpText)
        {
            return true;
        }

        return false;
    }

    public const string EndPointHelpText = "填充 OpenAI 兼容 API 的地址，如  https://ark.cn-beijing.volces.com/api/v3";
    public const string KeyHelpText = "请填充密码";
    public const string ModelNameHelpText = "请填充模型名";
}