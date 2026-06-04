namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 模型名到模型 Id 的映射
/// </summary>
/// 比如豆包，模型名和模型 Id 是不一样的，模型名是给用户看的，模型 Id 是调用接口时需要的。但深度求索的就是相同的
public class DictionaryModelNameToIdMap : IModelNameToIdMap
{
    /// <summary>
    /// 模型名称到模型 ID 的字典映射。
    /// </summary>
    public required Dictionary<string, string> ModelNameToIdDictionary { get; init; }

    /// <inheritdoc/>
    public string GetModelId(string modelName)
    {
        return ModelNameToIdDictionary.GetValueOrDefault(modelName, modelName);
    }
}