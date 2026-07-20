using System.IO;
using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace DeepSeekWpf.Services;

public sealed class AgentConfigurationService : IAgentConfigurationService
{
    private readonly IAppLogger _logger;

    public AgentConfigurationService(IAppLogger logger)
    {
        _logger = logger;
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekWpf");
        Directory.CreateDirectory(appDataPath);
        ConfigurationFilePath = Path.Combine(appDataPath, "agent-api.json");
    }

    public string ConfigurationFilePath { get; }

    public async Task<AgentConfigurationLoadResult> LoadAsync()
    {
        EnsureTemplateExists();

        try
        {
            var configuration = await AgentApiManagerConfiguration.FromJsonFileAsync(
                new FileInfo(ConfigurationFilePath));
            Validate(configuration);
            return new AgentConfigurationLoadResult(
                configuration,
                ConfigurationFilePath,
                IsDebugFallback: false);
        }
        catch (Exception exception)
        {
            _logger.Log($"加载 Agent 配置失败：{exception}");
#if DEBUG
            try
            {
                var debugConfiguration = LindexiAgentConfiguration.LoadDefault();
                Validate(debugConfiguration);
                _logger.Log("已使用 LindexiAgentConfiguration 调试配置");
                return new AgentConfigurationLoadResult(
                    debugConfiguration,
                    nameof(LindexiAgentConfiguration),
                    IsDebugFallback: true);
            }
            catch (Exception debugException)
            {
                _logger.Log($"加载本地调试 Agent 配置失败：{debugException}");
                throw new InvalidOperationException(
                    $"无法加载 Agent 配置。请编辑 '{ConfigurationFilePath}'；Debug 回退配置也不可用。",
                    new AggregateException(exception, debugException));
            }
#else
            throw new InvalidOperationException(
                $"无法加载 Agent 配置。请编辑 '{ConfigurationFilePath}' 后重试。",
                exception);
#endif
        }
    }

    public void EnsureTemplateExists()
    {
        if (File.Exists(ConfigurationFilePath))
        {
            return;
        }

        File.WriteAllText(ConfigurationFilePath, AgentApiManagerConfiguration.DefaultTemplateFileContent);
    }

    private static void Validate(AgentApiManagerConfiguration configuration)
    {
        if (configuration.OpenAIConfigurationList is not { Count: > 0 })
        {
            throw new InvalidDataException("Agent 配置不包含任何 OpenAI 协议终结点。");
        }

        var hasUsableProvider = configuration.OpenAIConfigurationList.Any(provider =>
            !string.IsNullOrWhiteSpace(provider.EndPoint) &&
            !string.IsNullOrWhiteSpace(provider.Key) &&
            provider.ModelDefinitions is { Count: > 0 });
        if (!hasUsableProvider)
        {
            throw new InvalidDataException("Agent 配置中没有同时包含终结点、密钥和模型定义的可用提供商。");
        }
    }
}
