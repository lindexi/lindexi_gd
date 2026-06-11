using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Core;

[TestClass]
public class AgentApiEndpointManagerTests
{
    [TestMethod]
    [Description("PrimaryModel 不包含分隔符时按 ModelName 匹配（向后兼容）")]
    public void LoadConfiguration_WhenPrimaryModelHasNoSeparator_MatchesByModelName()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("pro1", "pro-model", null),
            ("pro2", "other-model", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "pro-model"
        };
        manager.LoadConfiguration(configuration);

        Assert.AreEqual("pro-model", manager.PrimaryModel.ModelDefinition.ModelName);
        Assert.AreEqual("pro1", manager.PrimaryModel.ModelDefinition.Provider);
    }

    [TestMethod]
    [Description("PrimaryModel 不包含分隔符时按 ModelId 匹配（向后兼容）")]
    public void LoadConfiguration_WhenPrimaryModelHasNoSeparator_MatchesByModelId()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("pro1", "display-name", "real-model-id"));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "real-model-id"
        };
        manager.LoadConfiguration(configuration);

        Assert.AreEqual("display-name", manager.PrimaryModel.ModelDefinition.ModelName);
        Assert.AreEqual("real-model-id", manager.PrimaryModel.ModelDefinition.ModelId);
    }

    [TestMethod]
    [Description("PrimaryModel 使用斜杠分隔符时按 Provider/ModelName 精确匹配")]
    public void LoadConfiguration_WhenPrimaryModelHasSlashSeparator_MatchesByProviderAndModelName()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("deepseek", "deepseek-v4-pro", null),
            ("openai", "deepseek-v4-pro", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "deepseek/deepseek-v4-pro"
        };
        manager.LoadConfiguration(configuration);

        Assert.AreEqual("deepseek-v4-pro", manager.PrimaryModel.ModelDefinition.ModelName);
        Assert.AreEqual("deepseek", manager.PrimaryModel.ModelDefinition.Provider);
    }

    [TestMethod]
    [Description("PrimaryModel 使用反斜杠分隔符时按 Provider\\ModelName 精确匹配")]
    public void LoadConfiguration_WhenPrimaryModelHasBackslashSeparator_MatchesByProviderAndModelName()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("deepseek", "deepseek-v4-pro", null),
            ("openai", "deepseek-v4-pro", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "deepseek\\deepseek-v4-pro"
        };
        manager.LoadConfiguration(configuration);

        Assert.AreEqual("deepseek-v4-pro", manager.PrimaryModel.ModelDefinition.ModelName);
        Assert.AreEqual("deepseek", manager.PrimaryModel.ModelDefinition.Provider);
    }

    [TestMethod]
    [Description("多个提供商有同名模型时，Provider/ModelName 只匹配指定提供商的模型")]
    public void LoadConfiguration_WhenMultipleProvidersHaveSameModelName_MatchesExactProvider()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("openai", "gpt-4", null),
            ("azure", "gpt-4", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "openai/gpt-4"
        };
        manager.LoadConfiguration(configuration);

        Assert.AreEqual("gpt-4", manager.PrimaryModel.ModelDefinition.ModelName);
        Assert.AreEqual("openai", manager.PrimaryModel.ModelDefinition.Provider);
    }

    [TestMethod]
    [Description("指定的 Provider/ModelName 不存在时抛出 ArgumentException")]
    public void LoadConfiguration_WhenProviderAndModelNameNotFound_ThrowsArgumentException()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("deepseek", "deepseek-v4-pro", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "nonexistent/model"
        };

        var ex = Assert.Throws<ArgumentException>(() => manager.LoadConfiguration(configuration));
        StringAssert.Contains(ex.Message, "nonexistent/model");
    }

    [TestMethod]
    [Description("PrimaryModel 以分隔符开头时回退到原有匹配逻辑")]
    public void LoadConfiguration_WhenPrimaryModelStartsWithSeparator_FallsBackToOriginalLogic()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("pro1", "/model-name", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "/model-name"
        };
        manager.LoadConfiguration(configuration);

        Assert.AreEqual("/model-name", manager.PrimaryModel.ModelDefinition.ModelName);
    }

    [TestMethod]
    [Description("PrimaryModel 以分隔符结尾时回退到原有匹配逻辑")]
    public void LoadConfiguration_WhenPrimaryModelEndsWithSeparator_FallsBackToOriginalLogic()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("pro1", "provider/", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "provider/"
        };
        manager.LoadConfiguration(configuration);

        Assert.AreEqual("provider/", manager.PrimaryModel.ModelDefinition.ModelName);
    }

    [TestMethod]
    [Description("包含斜杠的 PrimaryModel，Provider 匹配但 ModelName 不匹配时抛出异常")]
    public void LoadConfiguration_WhenProviderMatchesButModelNameDoesNot_ThrowsArgumentException()
    {
        var manager = new AgentApiEndpointManager();
        var fakeProvider = CreateFakeProvider(
            ("deepseek", "deepseek-v4-pro", null),
            ("openai", "gpt-4", null));
        manager.RegisterLanguageModelProvider(fakeProvider);

        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "deepseek/nonexistent"
        };
        
        Assert.Throws<ArgumentException>(() => manager.LoadConfiguration(configuration));
    }

    private static FakeLanguageModelProvider CreateFakeProvider(params (string Provider, string ModelName, string? ModelId)[] models)
    {
        var languageModels = new List<FakeLanguageModel>(models.Length);
        foreach (var (provider, modelName, modelId) in models)
        {
            languageModels.Add(new FakeLanguageModel(
                new FakeChatClient())
            {
                ModelDefinition = new ModelDefinition
                {
                    Provider = provider,
                    ModelName = modelName,
                    ModelId = modelId,
                }
            });
        }

        return new FakeLanguageModelProvider(languageModels);
    }
}