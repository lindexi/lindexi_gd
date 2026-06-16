using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using ChatRoomAvaloniaDemo.Models;

using System;
using System.IO;
using System.Linq;
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
        config.PrimaryModelId = agentConfig.PrimaryModel ?? string.Empty;

        // 转换 OpenAI 协议配置
        if (agentConfig.OpenAIConfigurationList is not null)
        {
            foreach (var openAIConfig in agentConfig.OpenAIConfigurationList)
            {
                var providerConfig = ConvertToProviderConfig(openAIConfig);
                config.Providers.Add(providerConfig);

                // 如果默认模型在此提供商下，设置提供商名称
                if (!string.IsNullOrEmpty(config.PrimaryModelId) &&
                    string.IsNullOrEmpty(config.DefaultModelProviderName))
                {
                    if (HasModel(providerConfig, config.PrimaryModelId))
                    {
                        config.DefaultModelProviderName = providerConfig.ProviderName;
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
            // ProviderName 从首个模型定义的 Provider 获取，与 ModelDefinition.Provider 一致
            ProviderName = openAIConfig.ModelDefinitions?.FirstOrDefault()?.Provider ?? string.Empty,
            ApiEndpoint = openAIConfig.EndPoint ?? string.Empty,
            ApiKey = openAIConfig.Key ?? string.Empty,
        };

        // 转换模型定义
        if (openAIConfig.ModelDefinitions is not null)
        {
            foreach (var modelDef in openAIConfig.ModelDefinitions)
            {
                var modelConfig = ConvertToModelConfig(modelDef);

                // 如果从首个模型未能获取 ProviderName，尝试后续模型
                if (string.IsNullOrEmpty(providerConfig.ProviderName) &&
                    !string.IsNullOrEmpty(modelDef.Provider))
                {
                    providerConfig.ProviderName = modelDef.Provider;
                }

                providerConfig.Models.Add(modelConfig);
            }

            // 设置主模型为第一个模型
            if (providerConfig.Models.Count > 0)
            {
                providerConfig.PrimaryModelId = providerConfig.Models[0].ModelName;
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
