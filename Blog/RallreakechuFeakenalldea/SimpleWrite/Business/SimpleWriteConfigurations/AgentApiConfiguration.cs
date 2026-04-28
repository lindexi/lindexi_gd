using dotnetCampus.Configurations;

namespace SimpleWrite.Business.SimpleWriteConfigurations;

public class DouBaoAgentApiConfiguration() : AgentApiConfiguration("DouBao");
public class DeepSeekAgentApiConfiguration() : AgentApiConfiguration("DeepSeek");

public class AgentApiConfiguration : Configuration
{
    public AgentApiConfiguration()
    {
    }

    public AgentApiConfiguration(string? sectionName) : base(sectionName)
    {
    }

    /// <summary>
    /// 所选的提供商，为 <see cref="ModelVendor"/> 枚举字符串
    /// </summary>
    public ConfigurationString? SelectedVendor
    {
        get => GetString();
        set => SetValue(value);
    }

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

/// <summary>
/// 预设的模型提供商
/// </summary>
public enum ModelVendor
{
    DouBao,
    Deepseek,
}