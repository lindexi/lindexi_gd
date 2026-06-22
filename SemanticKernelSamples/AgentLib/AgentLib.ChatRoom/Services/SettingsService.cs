using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.ChatRoom.Configuration;
using AgentLib.Core.AgentApiManagers;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 应用设置服务。负责 <see cref="AppSettings"/> 的 JSON 持久化，
/// 以及 <see cref="AppSettings"/> ↔ <see cref="AgentApiManagerConfiguration"/> 的双向转换。
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FileInfo _settingsFile;

    /// <summary>
    /// 使用指定的设置文件创建设置服务。
    /// </summary>
    /// <param name="settingsFile">settings.json 的文件信息。</param>
    public SettingsService(FileInfo settingsFile)
    {
        ArgumentNullException.ThrowIfNull(settingsFile);
        _settingsFile = settingsFile;
    }

    /// <summary>
    /// 加载设置。文件不存在时写入内置默认配置以便用户直接编辑；
    /// 文件中没有模型提供商时，调用 <see cref="LindexiAgentConfiguration.LoadDefault"/> 加载默认模型配置。
    /// </summary>
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_settingsFile.Exists)
        {
            AppSettings defaultSettings = CreateDefaultWithBuiltinProviders();
            await SaveAsync(defaultSettings, cancellationToken).ConfigureAwait(false);
            return defaultSettings;
        }

        string json = await File.ReadAllTextAsync(_settingsFile.FullName, cancellationToken).ConfigureAwait(false);
        AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, s_jsonOptions);

        if (settings is null)
        {
            return CreateDefaultWithBuiltinProviders();
        }

        // 文件中没有模型提供商时，回退到内置默认配置
        if (settings.Providers.Count == 0)
        {
            AppSettings defaultSettings = CreateDefaultWithBuiltinProviders();
            defaultSettings.PersistencePath = string.IsNullOrWhiteSpace(settings.PersistencePath)
                ? defaultSettings.PersistencePath
                : settings.PersistencePath;
            defaultSettings.DefaultMaxRounds = settings.DefaultMaxRounds;
            defaultSettings.PrimaryModel = settings.PrimaryModel ?? defaultSettings.PrimaryModel;
            return defaultSettings;
        }

        return settings;
    }

    /// <summary>
    /// 保存设置到 JSON 文件。
    /// </summary>
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (_settingsFile.Directory is not null)
        {
            _settingsFile.Directory.Create();
        }

        string json = JsonSerializer.Serialize(settings, s_jsonOptions);
        await File.WriteAllTextAsync(_settingsFile.FullName, json, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 将 <see cref="AppSettings"/> 转换为 <see cref="AgentApiManagerConfiguration"/>。
    /// </summary>
    public static AgentApiManagerConfiguration ToApiConfiguration(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        List<OpenAIProtocolLanguageModelConfiguration>? openAIConfigs = null;

        if (settings.Providers.Count > 0)
        {
            openAIConfigs = settings.Providers
                .Where(p => !string.IsNullOrWhiteSpace(p.Endpoint))
                .Select(p => new OpenAIProtocolLanguageModelConfiguration(p.Endpoint, p.Key)
                {
                    ModelDefinitions = p.Models.Select(m => new ModelDefinition
                    {
                        Provider = p.Name,
                        ModelName = m.ModelName,
                        ModelId = m.ModelId,
                        Capabilities = BuildDefaultCapabilities(m.IsFlash),
                    }).ToList(),
                })
                .ToList();
        }

        return new AgentApiManagerConfiguration
        {
            PrimaryModel = settings.PrimaryModel,
            OpenAIConfigurationList = openAIConfigs,
        };
    }

    /// <summary>
    /// 将 <see cref="AgentApiManagerConfiguration"/> 转换为 <see cref="AppSettings"/>。
    /// </summary>
    public static AppSettings FromApiConfiguration(AgentApiManagerConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        List<ProviderSetting> providers = [];

        if (config.OpenAIConfigurationList is not null)
        {
            foreach (OpenAIProtocolLanguageModelConfiguration oc in config.OpenAIConfigurationList)
            {
                string providerName = oc.ModelDefinitions?.FirstOrDefault()?.Provider ?? "未知提供商";
                providers.Add(new ProviderSetting
                {
                    Name = providerName,
                    Endpoint = oc.EndPoint,
                    Key = oc.Key,
                    Models = oc.ModelDefinitions?.Select(md => new ModelSetting
                    {
                        ModelName = md.ModelName,
                        ModelId = md.ModelId,
                        IsFlash = md.Capabilities?.IsFlash ?? false,
                    }).ToList() ?? [],
                });
            }
        }

        return new AppSettings
        {
            PrimaryModel = config.PrimaryModel,
            Providers = providers,
        };
    }

    /// <summary>
    /// 基于轻量标记生成默认的模型能力画像。
    /// </summary>
    private static LlmModelCapabilities BuildDefaultCapabilities(bool isFlash)
    {
        return new LlmModelCapabilities
        {
            Temperature = true,
            Reasoning = !isFlash,
            ToolCall = true,
            Input = new LlmModalityCapability { Text = true },
            Output = new LlmModalityCapability { Text = true },
            IsFlash = isFlash,
        };
    }

    private static AppSettings CreateDefault()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new AppSettings
        {
            PersistencePath = Path.Join(appData, "AgentRoundtable", "Sessions"),
            DefaultMaxRounds = 10,
        };
    }

    /// <summary>
    /// 创建包含内置默认模型提供商的默认设置。
    /// 调用 <see cref="LindexiAgentConfiguration.LoadDefault"/> 获取预置模型配置。
    /// </summary>
    private static AppSettings CreateDefaultWithBuiltinProviders()
    {
        AppSettings settings = CreateDefault();

        try
        {
            AgentApiManagerConfiguration defaultConfig = LindexiAgentConfiguration.LoadDefault();
            settings = FromApiConfiguration(defaultConfig);
            settings.PersistencePath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AgentRoundtable", "Sessions");
            settings.DefaultMaxRounds = 10;
        }
        catch
        {
            // 内置配置加载失败时使用空配置
        }

        return settings;
    }
}
