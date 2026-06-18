using System;
using System.Collections.Generic;
using System.Linq;

using AgentLib.ChatRoom.Configuration;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 模型提供商服务。管理提供商配置，生成 <see cref="ILanguageModelProvider"/> 字典供角色注册。
/// </summary>
public sealed class ModelProviderService
{
    private AppSettings _appSettings;

    /// <summary>
    /// 使用指定的应用设置创建模型提供商服务。
    /// </summary>
    /// <param name="appSettings">当前应用设置。</param>
    public ModelProviderService(AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        _appSettings = appSettings;
    }

    /// <summary>
    /// 更新内部设置引用（设置变更后调用）。
    /// </summary>
    public void UpdateSettings(AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        _appSettings = appSettings;
    }

    /// <summary>
    /// 获取所有已注册的模型提供商字典，按提供商名称索引。
    /// </summary>
    public IReadOnlyDictionary<string, ILanguageModelProvider> GetProviders()
    {
        var result = new Dictionary<string, ILanguageModelProvider>(_appSettings.Providers.Count, StringComparer.OrdinalIgnoreCase);

        foreach (ProviderSetting provider in _appSettings.Providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Name) || string.IsNullOrWhiteSpace(provider.Endpoint))
            {
                continue;
            }

            var config = new OpenAIProtocolLanguageModelConfiguration(provider.Endpoint, provider.Key)
            {
                ModelDefinitions = provider.Models.Select(m => new global::AgentLib.Core.AgentApiManagers.Contexts.ModelDefinition
                {
                    Provider = provider.Name,
                    ModelName = m.ModelName,
                    ModelId = m.ModelId,
                    Capabilities = BuildDefaultCapabilities(m.IsFlash),
                }).ToList(),
            };

            var providerInstance = new JsonConfigurationOpenAIProtocolLanguageModelProvider(config);
            result[provider.Name] = providerInstance;
        }

        return result;
    }

    /// <summary>
    /// 创建一个已注册所有提供商的 <see cref="AgentApiEndpointManager"/> 实例。
    /// </summary>
    public AgentApiEndpointManager CreateEndpointManager()
    {
        var endpointManager = new AgentApiEndpointManager();
        var apiConfig = SettingsService.ToApiConfiguration(_appSettings);
        endpointManager.LoadConfiguration(apiConfig);
        return endpointManager;
    }

    /// <summary>
    /// 获取所有可用模型的显示名称列表，格式为 "Provider / ModelName"。
    /// </summary>
    public IReadOnlyList<string> GetAvailableModelDisplayNames()
    {
        var result = new List<string>();

        foreach (ProviderSetting provider in _appSettings.Providers)
        {
            foreach (ModelSetting model in provider.Models)
            {
                result.Add($"{provider.Name} / {model.ModelName}");
            }
        }

        return result;
    }

    private static global::AgentLib.Core.AgentApiManagers.Contexts.LlmModelCapabilities BuildDefaultCapabilities(bool isFlash)
    {
        return new global::AgentLib.Core.AgentApiManagers.Contexts.LlmModelCapabilities
        {
            Temperature = true,
            Reasoning = !isFlash,
            ToolCall = true,
            Input = new global::AgentLib.Core.AgentApiManagers.Contexts.LlmModalityCapability { Text = true },
            Output = new global::AgentLib.Core.AgentApiManagers.Contexts.LlmModalityCapability { Text = true },
            IsFlash = isFlash,
        };
    }
}
