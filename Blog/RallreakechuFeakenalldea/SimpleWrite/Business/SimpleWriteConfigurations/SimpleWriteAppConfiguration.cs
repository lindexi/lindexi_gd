using dotnetCampus.Configurations;

namespace SimpleWrite.Business.SimpleWriteConfigurations;

/// <summary>
/// 表示整个 SimpleWrite 应用的配置。
/// </summary>
public class SimpleWriteAppConfiguration : Configuration
{
    public ConfigurationString? CopilotAbilityDirectory
    {
        get => GetString();
        set => SetValue(value);
    }
}
