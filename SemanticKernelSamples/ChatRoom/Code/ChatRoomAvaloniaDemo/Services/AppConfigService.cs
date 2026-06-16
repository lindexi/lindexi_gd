using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using ChatRoomAvaloniaDemo.Models;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChatRoomAvaloniaDemo.Services;

/// <summary>
/// 应用配置服务。负责配置的加载、保存和默认初始化。
/// </summary>
public sealed class AppConfigService
{
    /// <summary>
    /// 应用数据根目录名称。
    /// </summary>
    private const string AppDataFolderName = "AgentRoundtable";

    /// <summary>
    /// 配置文件名。
    /// </summary>
    private const string ConfigFileName = "AppConfiguration.json";

    /// <summary>
    /// 会话持久化子目录名。
    /// </summary>
    private const string SessionsFolderName = "sessions";

    /// <summary>
    /// 技能文件夹子目录名。
    /// </summary>
    private const string SkillsFolderName = "skills";

    /// <summary>
    /// 获取应用数据根目录路径。
    /// </summary>
    /// <returns>应用数据根目录的完整路径。</returns>
    public static string GetAppDataRootPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Join(localAppData, AppDataFolderName);
    }

    /// <summary>
    /// 获取配置文件路径。
    /// </summary>
    /// <returns>配置文件的完整路径。</returns>
    public static string GetConfigFilePath()
    {
        return Path.Join(GetAppDataRootPath(), ConfigFileName);
    }

    /// <summary>
    /// 获取会话持久化目录路径。
    /// </summary>
    /// <returns>会话持久化目录的完整路径。</returns>
    public static string GetSessionsPath()
    {
        return Path.Join(GetAppDataRootPath(), SessionsFolderName);
    }

    /// <summary>
    /// 获取技能文件夹路径。
    /// </summary>
    /// <returns>技能文件夹的完整路径。</returns>
    public static string GetSkillsPath()
    {
        return Path.Join(GetAppDataRootPath(), SkillsFolderName);
    }

    /// <summary>
    /// 加载或初始化应用配置。优先从本地配置文件加载，如果不存在则使用默认配置并保存。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>加载或初始化后的应用配置。</returns>
    public static async Task<AppConfig> LoadOrInitializeAsync(CancellationToken cancellationToken = default)
    {
        var configFilePath = GetConfigFilePath();

        // 尝试从本地配置文件加载
        var config = await AppConfig.LoadAsync(configFilePath, cancellationToken).ConfigureAwait(false);
        if (config is not null)
        {
            config.ConfigFilePath = configFilePath;
            return config;
        }

        // 配置文件不存在，使用默认配置
        config = CreateDefaultConfig();
        config.ConfigFilePath = configFilePath;

        // 确保目录存在
        var appDataRoot = GetAppDataRootPath();
        Directory.CreateDirectory(appDataRoot);
        Directory.CreateDirectory(GetSessionsPath());
        Directory.CreateDirectory(GetSkillsPath());

        // 保存默认配置
        await config.SaveAsync(configFilePath, cancellationToken).ConfigureAwait(false);

        return config;
    }

    /// <summary>
    /// 创建默认配置。从 <see cref="LindexiAgentConfiguration.LoadDefault()"/> 加载并转换。
    /// </summary>
    /// <returns>默认应用配置。</returns>
    private static AppConfig CreateDefaultConfig()
    {
        var config = new AppConfig
        {
            PersistenceBasePath = GetSessionsPath(),
            SkillFoldersBasePath = GetSkillsPath(),
            DefaultMaxRounds = 10,
        };

        // 从 LindexiAgentConfiguration 加载默认配置
        var agentConfig = LindexiAgentConfiguration.LoadDefault();

        // 设置默认模型
        config.DefaultModelId = agentConfig.PrimaryModel ?? string.Empty;

        // 转换 OpenAI 协议配置
        if (agentConfig.OpenAIConfigurationList is not null)
        {
            foreach (var openAIConfig in agentConfig.OpenAIConfigurationList)
            {
                var providerConfig = ConvertToProviderConfig(openAIConfig);
                config.Providers.Add(providerConfig);

                // 如果默认模型在此提供商下，设置提供商 ID
                if (!string.IsNullOrEmpty(config.DefaultModelId) &&
                    string.IsNullOrEmpty(config.DefaultModelProviderId))
                {
                    if (HasModel(providerConfig, config.DefaultModelId))
                    {
                        config.DefaultModelProviderId = providerConfig.ProviderId;
                    }
                }
            }
        }

        return config;
    }

    /// <summary>
    /// 将 OpenAI 协议配置转换为提供商配置。
    /// </summary>
    /// <param name="openAIConfig">OpenAI 协议配置。</param>
    /// <returns>转换后的提供商配置。</returns>
    private static ModelProviderConfig ConvertToProviderConfig(OpenAIProtocolLanguageModelConfiguration openAIConfig)
    {
        var providerConfig = new ModelProviderConfig
        {
            ProviderId = Guid.NewGuid().ToString("N")[..8],
            ProviderName = ExtractProviderName(openAIConfig.EndPoint),
            ApiEndpoint = openAIConfig.EndPoint ?? string.Empty,
            ApiKey = openAIConfig.Key ?? string.Empty,
        };

        // 转换模型定义
        if (openAIConfig.ModelDefinitions is not null)
        {
            foreach (var modelDef in openAIConfig.ModelDefinitions)
            {
                var modelConfig = ConvertToModelConfig(modelDef);
                providerConfig.Models.Add(modelConfig);
            }

            // 设置默认模型为第一个模型
            if (providerConfig.Models.Count > 0)
            {
                providerConfig.DefaultModelId = providerConfig.Models[0].ModelName;
            }
        }

        return providerConfig;
    }

    /// <summary>
    /// 将模型定义转换为模型配置。
    /// </summary>
    /// <param name="modelDef">模型定义。</param>
    /// <returns>转换后的模型配置。</returns>
    private static ModelItemConfig ConvertToModelConfig(ModelDefinition modelDef)
    {
        return new ModelItemConfig
        {
            ModelName = modelDef.ModelName ?? string.Empty,
            ModelId = modelDef.ModelId ?? modelDef.ModelName ?? string.Empty,
            Provider = modelDef.Provider ?? string.Empty,
            IsFlash = modelDef.Capabilities?.IsFlash ?? false,
        };
    }

    /// <summary>
    /// 从终结点提取提供商名称。
    /// </summary>
    /// <param name="endpoint">API 终结点。</param>
    /// <returns>提取的提供商名称。</returns>
    private static string ExtractProviderName(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            return "Unknown";
        }

        try
        {
            var uri = new Uri(endpoint);
            var host = uri.Host;
            // 提取主机名的第一部分作为提供商名称
            var parts = host.Split('.');
            if (parts.Length > 0)
            {
                return parts[0];
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return endpoint;
    }

    /// <summary>
    /// 检查提供商配置是否包含指定模型。
    /// </summary>
    /// <param name="providerConfig">提供商配置。</param>
    /// <param name="modelName">模型名称。</param>
    /// <returns>是否包含该模型。</returns>
    private static bool HasModel(ModelProviderConfig providerConfig, string modelName)
    {
        foreach (var model in providerConfig.Models)
        {
            if (string.Equals(model.ModelName, modelName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(model.ModelId, modelName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
