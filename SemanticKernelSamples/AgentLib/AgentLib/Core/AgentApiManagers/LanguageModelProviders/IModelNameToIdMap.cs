namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public interface IModelNameToIdMap
{
    string GetModelId(string modelName);
}