using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Core;

/// <summary>
/// 管理 API 终结点和语言模型提供商的注册、查询与选择。
/// </summary>
public class AgentApiEndpointManager
{
    /// <summary>
    /// 从配置加载语言模型提供商并注册。
    /// </summary>
    /// <param name="configuration">API 管理器配置。</param>
    public void LoadConfiguration(AgentApiManagerConfiguration configuration)
    {
        if (configuration.OpenAIConfigurationList is not null)
        {
            foreach (var languageModelConfiguration in configuration.OpenAIConfigurationList)
            {
                var provider = JsonConfigurationOpenAIProtocolLanguageModelProvider
                    .FromConfiguration(languageModelConfiguration);
                if (!string.IsNullOrEmpty(provider.Key))
                {
                    RegisterLanguageModelProvider(provider);
                }
            }
        }

        if (configuration.PrimaryModel is var primaryModel && !string.IsNullOrEmpty(primaryModel))
        {
            var supportedModels = GetSupportedModels();
            var languageModel = supportedModels.FirstOrDefault(t =>
                t.ModelDefinition.ModelName == primaryModel || t.ModelDefinition.ModelId == primaryModel);
            if (languageModel is null)
            {
                throw new ArgumentException($"Can not find PrimaryModel('{primaryModel}') in SupportedModels");
            }

            PrimaryModel = languageModel;
        }
    }

    /// <summary>
        /// 注册一个语言模型提供商。
        /// </summary>
        /// <param name="languageModelProvider">语言模型提供商。</param>
        public void RegisterLanguageModelProvider(ILanguageModelProvider languageModelProvider)
    {
        var languageModels = languageModelProvider.GetSupportedModels();
        SupportedModels.AddRange(languageModels);
        // 有更新内容了，需要重新评估自动选择首选模型
        _autoSetPrimaryLanguageModel = null;
    }

    /// <summary>
    /// 获取所有已注册的受支持模型列表。
    /// </summary>
    /// <returns>受支持的模型列表。</returns>
    public IReadOnlyList<ILanguageModel> GetSupportedModels() => SupportedModels;

    /// <summary>
    /// 根据模型名称或 ID 查找模型，可选择性指定提供商。
    /// </summary>
    /// <param name="modelNameOrId">模型名称或 ID。</param>
    /// <param name="provider">可选的提供商名称。</param>
    /// <returns>匹配的模型，如果未找到则返回 <see langword="null"/>。</returns>
    public ILanguageModel? GetModel(string modelNameOrId, string? provider = null)
    {
        var supportedModels = GetSupportedModels();
        return supportedModels.FirstOrDefault(IsModelMatch);

        bool IsModelMatch(ILanguageModel model)
        {
            var modelDefinition = model.ModelDefinition;

            var isNameOrIdMatch = modelDefinition.ModelName == modelNameOrId
                                  || modelDefinition.ModelId == modelNameOrId;

            var isProviderMatch = provider is null || modelDefinition.Provider == provider;
            return isNameOrIdMatch && isProviderMatch;
        }
    }

    /// <summary>
        /// 根据谓词条件获取最佳匹配的模型。按能力排序，选择能力最强的模型。
        /// </summary>
        /// <param name="predicate">筛选谓词。</param>
        /// <returns>最佳匹配的模型。</returns>
        /// <exception cref="InvalidOperationException">没有符合要求的模型时抛出。</exception>
        public ILanguageModel GetBestModel(Func<ILanguageModel, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var matchedModel = GetSupportedModels().Where(predicate).OrderDescending(new LanguageModelCapabilityComparer())
            .FirstOrDefault();
        if (matchedModel is null)
        {
            throw new InvalidOperationException("当前没有符合要求的模型。请检查模型能力配置或调整子代理类型。");
        }

        return matchedModel;
    }

    private List<ILanguageModel> SupportedModels { get; } = [];

    /// <summary>
        /// 首选语言模型。如果用户未设置，则自动选择能力最强的模型。
        /// </summary>
        public ILanguageModel PrimaryModel
    {
        get
        {
            if (_userSetPrimaryLanguageModel != null)
            {
                return _userSetPrimaryLanguageModel;
            }

            if (_autoSetPrimaryLanguageModel == null)
            {
                var supportedModels = GetSupportedModels();
                if (supportedModels.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"尚未调用 {nameof(RegisterLanguageModelProvider)} 完成任何注册，无法获取到模型列表");
                }
                else if (supportedModels.Count == 1)
                {
                    return supportedModels[0];
                }

                // 全模态优先 Omni
                _autoSetPrimaryLanguageModel = supportedModels.ToList()
                    .OrderDescending(new LanguageModelCapabilityComparer()).First();
            }

            return _autoSetPrimaryLanguageModel;
        }
        set
        {
            var supportedModels = GetSupportedModels();
            if (!supportedModels.Contains(value))
            {
                throw new ArgumentException($"只能设置 {nameof(GetSupportedModels)} 支持的模型");
            }

            _userSetPrimaryLanguageModel = value;
            _autoSetPrimaryLanguageModel = null; // 用户设置后，自动选择的模型不再生效，直到用户取消设置
        }
    }

    /// <summary>
    /// 用户设置的首选模型
    /// </summary>
    private ILanguageModel? _userSetPrimaryLanguageModel;

    /// <summary>
    /// 用户没有设置的前提下，自动决定的首选模型
    /// </summary>
    private ILanguageModel? _autoSetPrimaryLanguageModel;
}