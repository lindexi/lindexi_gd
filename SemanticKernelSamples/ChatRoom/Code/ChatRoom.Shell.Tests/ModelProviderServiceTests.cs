using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.ChatRoom.Configuration;
using AgentLib.ChatRoom.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatRoom.Shell.Tests;

/// <summary>
/// <see cref="ModelProviderService"/> 的单元测试。
/// </summary>
[TestClass]
public class ModelProviderServiceTests
{
    /// <summary>
    /// GetProviders 应返回按提供商名索引的字典。
    /// </summary>
    [TestMethod]
    public void GetProviders_ShouldReturnDictionaryKeyedByName()
    {
        var settings = new AppSettings
        {
            Providers =
            [
                new ProviderSetting
                {
                    Name = "deepseek",
                    Endpoint = "https://api.deepseek.com",
                    Key = "key1",
                    Models = [new ModelSetting { ModelName = "deepseek-v4-pro" }],
                },
                new ProviderSetting
                {
                    Name = "doubao",
                    Endpoint = "https://ark.cn-beijing.volces.com/api/v3",
                    Key = "key2",
                    Models = [new ModelSetting { ModelName = "Doubao-Seed-2.0-pro" }],
                },
            ],
        };

        var service = new ModelProviderService(settings);
        var providers = service.GetProviders();

        Assert.AreEqual(2, providers.Count);
        Assert.IsTrue(providers.ContainsKey("deepseek"));
        Assert.IsTrue(providers.ContainsKey("doubao"));

        var deepseekModels = providers["deepseek"].GetSupportedModels();
        Assert.AreEqual(1, deepseekModels.Count);
        Assert.AreEqual("deepseek-v4-pro", deepseekModels[0].ModelDefinition.ModelName);

        var doubaoModels = providers["doubao"].GetSupportedModels();
        Assert.AreEqual(1, doubaoModels.Count);
        Assert.AreEqual("Doubao-Seed-2.0-pro", doubaoModels[0].ModelDefinition.ModelName);
    }

    /// <summary>
    /// 名称或地址为空的提供商应被跳过。
    /// </summary>
    [TestMethod]
    public void GetProviders_ShouldSkipInvalidProviders()
    {
        var settings = new AppSettings
        {
            Providers =
            [
                new ProviderSetting
                {
                    Name = "",
                    Endpoint = "https://api.test.com",
                    Key = "key",
                    Models = [new ModelSetting { ModelName = "model-a" }],
                },
                new ProviderSetting
                {
                    Name = "valid",
                    Endpoint = "",
                    Key = "key",
                    Models = [new ModelSetting { ModelName = "model-b" }],
                },
                new ProviderSetting
                {
                    Name = "ok",
                    Endpoint = "https://api.ok.com",
                    Key = "key",
                    Models = [new ModelSetting { ModelName = "model-c" }],
                },
            ],
        };

        var service = new ModelProviderService(settings);
        var providers = service.GetProviders();

        Assert.AreEqual(1, providers.Count);
        Assert.IsTrue(providers.ContainsKey("ok"));
    }

    /// <summary>
    /// GetAvailableModelDisplayNames 应返回 "Provider / ModelName" 格式的列表。
    /// </summary>
    [TestMethod]
    public void GetAvailableModelDisplayNames_ShouldReturnFormattedNames()
    {
        var settings = new AppSettings
        {
            Providers =
            [
                new ProviderSetting
                {
                    Name = "deepseek",
                    Endpoint = "https://api.deepseek.com",
                    Key = "key",
                    Models =
                    [
                        new ModelSetting { ModelName = "deepseek-v4-pro" },
                        new ModelSetting { ModelName = "deepseek-v4-flash" },
                    ],
                },
            ],
        };

        var service = new ModelProviderService(settings);
        var names = service.GetAvailableModelDisplayNames();

        Assert.AreEqual(2, names.Count);
        Assert.AreEqual("deepseek / deepseek-v4-pro", names[0]);
        Assert.AreEqual("deepseek / deepseek-v4-flash", names[1]);
    }

    /// <summary>
    /// UpdateSettings 后再次获取应反映新配置。
    /// </summary>
    [TestMethod]
    public void UpdateSettings_ShouldReflectNewConfig()
    {
        var settings = new AppSettings
        {
            Providers =
            [
                new ProviderSetting
                {
                    Name = "provider-a",
                    Endpoint = "https://a.api",
                    Key = "key",
                    Models = [new ModelSetting { ModelName = "model-a" }],
                },
            ],
        };

        var service = new ModelProviderService(settings);
        Assert.AreEqual(1, service.GetProviders().Count);

        var newSettings = new AppSettings
        {
            Providers =
            [
                new ProviderSetting
                {
                    Name = "provider-b",
                    Endpoint = "https://b.api",
                    Key = "key",
                    Models = [new ModelSetting { ModelName = "model-b" }],
                },
                new ProviderSetting
                {
                    Name = "provider-c",
                    Endpoint = "https://c.api",
                    Key = "key",
                    Models = [new ModelSetting { ModelName = "model-c" }],
                },
            ],
        };

        service.UpdateSettings(newSettings);

        var providers = service.GetProviders();
        Assert.AreEqual(2, providers.Count);
        Assert.IsTrue(providers.ContainsKey("provider-b"));
        Assert.IsTrue(providers.ContainsKey("provider-c"));
    }
}
