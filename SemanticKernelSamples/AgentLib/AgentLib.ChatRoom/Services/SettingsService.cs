using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.ChatRoom.Configuration;
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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _settingsFilePath;

    /// <summary>
    /// 使用指定的设置文件路径创建设置服务。
    /// </summary>
    /// <param name="settingsFilePath">settings.json 的完整路径。</param>
    public SettingsService(string settingsFilePath)
    {
        if (string.IsNullOrWhiteSpace(settingsFilePath))
        {
            throw new ArgumentException("设置文件路径不能为空。", nameof(settingsFilePath));
        }
        _settingsFilePath = settingsFilePath;
    }

    /// <summary>
    /// 加载设置。文件不存在时返回默认设置。
    /// </summary>
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return CreateDefault();
        }

        string json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken).ConfigureAwait(false);
        AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, s_jsonOptions);
        return settings ?? CreateDefault();
    }

    /// <summary>
    /// 保存设置到 JSON 文件。
    /// </summary>
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings, s_jsonOptions);
        await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken).ConfigureAwait(false);
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
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new AppSettings
        {
            PersistencePath = Path.Join(appData, "ChatRoom", "Sessions"),
            DefaultMaxRounds = 10,
        };
    }
}
