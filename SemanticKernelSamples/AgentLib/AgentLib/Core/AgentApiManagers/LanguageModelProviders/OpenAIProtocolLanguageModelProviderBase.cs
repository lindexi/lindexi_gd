using AgentLib.Core.AgentApiManagers.Contexts;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// OpenAI 协议语言模型基类，封装两个实现共享的消息构造、工具定义和响应解析逻辑。
/// </summary>
public abstract class OpenAIProtocolLanguageModelProviderBase(string endPoint, string key) : ILanguageModelProvider
{
    /// <summary>
    /// API 终结点地址。
    /// </summary>
    public string EndPoint => endPoint;

    /// <summary>
    /// API 密钥。
    /// </summary>
    public string Key => key;

    /// <summary>
    /// 模型名称到 ID 的映射。当模型名与调用 ID 不同时使用。
    /// </summary>
    public virtual IModelNameToIdMap? ModelNameToIdMap { get; init; }

    /// <inheritdoc/>
    public IReadOnlyList<ILanguageModel> GetSupportedModels()
    {
        var modelDefinitions = GetModelDefinitions();
        var result = new List<ILanguageModel>(modelDefinitions.Count);
        foreach (ModelDefinition modelDefinition in modelDefinitions)
        {
            var apiEndpoint = GetApiEndpointForModel(modelDefinition);
            result.Add(new OpenAILanguageModel(modelDefinition, apiEndpoint));
        }
        return result;
    }

    private ApiEndpoint GetApiEndpointForModel(ModelDefinition modelDefinition)
    {
        var modelId = modelDefinition.ModelId;
        if (modelId is null)
        {
            modelId = ModelNameToIdMap?.GetModelId(modelDefinition.ModelName);
            modelId ??= modelDefinition.ModelName;
        }

        return new ApiEndpoint(endPoint, key, modelId);
    }

    protected abstract IReadOnlyList<ModelDefinition> GetModelDefinitions();
}