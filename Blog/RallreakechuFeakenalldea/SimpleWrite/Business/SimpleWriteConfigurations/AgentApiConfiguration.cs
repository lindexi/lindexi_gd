using dotnetCampus.Configurations;

namespace SimpleWrite.Business.SimpleWriteConfigurations;

public class AgentApiConfiguration : Configuration
{
    public ConfigurationString? EndPoint
    {
        get => GetString();
        set => SetValue(value);
    }
    public ConfigurationString? Key
    {
        get => GetString();
        set => SetValue(value);
    }

    public ConfigurationString? ModelName
    {
        get => GetString();
        set => SetValue(value);
    }
}