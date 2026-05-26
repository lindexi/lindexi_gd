using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace AgentLib.Tests.Core;

[TestClass]
public class DictionaryModelNameToIdMapTests
{
    [TestMethod]
    [Description("模型名存在映射时应返回配置的模型 Id")]
    public void GetModelId_WhenModelNameExists_ReturnsMappedId()
    {
        var map = new DictionaryModelNameToIdMap
        {
            ModelNameToIdDictionary = new Dictionary<string, string>
            {
                ["deepseek-chat"] = "deepseek-v3"
            }
        };

        var result = map.GetModelId("deepseek-chat");

        Assert.AreEqual("deepseek-v3", result);
    }

    [TestMethod]
    [Description("模型名不存在映射时应回退返回原始模型名")]
    public void GetModelId_WhenModelNameDoesNotExist_ReturnsOriginalName()
    {
        var map = new DictionaryModelNameToIdMap
        {
            ModelNameToIdDictionary = new Dictionary<string, string>()
        };

        var result = map.GetModelId("gpt-4.1");

        Assert.AreEqual("gpt-4.1", result);
    }
}
