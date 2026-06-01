using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Core;

public class AgentApiEndpointManager
{
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

    public void RegisterLanguageModelProvider(ILanguageModelProvider languageModelProvider)
    {
        var languageModels = languageModelProvider.GetSupportedModels();
        SupportedModels.AddRange(languageModels);
        // 有更新内容了，需要重新评估自动选择首选模型
        _autoSetPrimaryLanguageModel = null;
    }

    public IReadOnlyList<ILanguageModel> GetSupportedModels() => SupportedModels;

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