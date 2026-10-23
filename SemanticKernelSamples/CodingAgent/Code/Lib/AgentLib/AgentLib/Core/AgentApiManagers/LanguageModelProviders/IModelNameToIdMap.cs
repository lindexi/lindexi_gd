namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 提供模型名称到模型 ID 的映射。
/// 某些平台（如豆包）的模型名和模型 ID 不同，模型名是面向用户的，模型 ID 是调用接口时需要的。
/// </summary>
public interface IModelNameToIdMap
{
    /// <summary>
    /// 根据模型名称获取对应的模型 ID。
    /// </summary>
    /// <param name="modelName">模型名称。</param>
    /// <returns>模型 ID。</returns>
    string GetModelId(string modelName);
}