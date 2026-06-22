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
/// <see cref="SettingsService"/> 的单元测试。
/// </summary>
[TestClass]
public class SettingsServiceTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ChatRoomTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    /// <summary>
    /// 保存后加载应返回相同的设置。
    /// </summary>
    [TestMethod]
    public async Task SaveThenLoad_ShouldReturnSameSettings()
    {
        var settingsFile = new FileInfo(Path.Join(_tempDir, "settings.json"));
        var service = new SettingsService(settingsFile);

        var settings = new AppSettings
        {
            PersistencePath = "/test/path",
            DefaultMaxRounds = 20,
            PrimaryModel = "deepseek/deepseek-v4-pro",
            Providers =
            [
                new ProviderSetting
                {
                    Name = "deepseek",
                    Endpoint = "https://api.deepseek.com",
                    Key = "test-key",
                    Models =
                    [
                        new ModelSetting { ModelName = "deepseek-v4-pro", IsFlash = false },
                        new ModelSetting { ModelName = "deepseek-v4-flash", IsFlash = true },
                    ],
                },
            ],
        };

        await service.SaveAsync(settings);

        AppSettings loaded = await service.LoadAsync();

        Assert.AreEqual("/test/path", loaded.PersistencePath);
        Assert.AreEqual(20, loaded.DefaultMaxRounds);
        Assert.AreEqual("deepseek/deepseek-v4-pro", loaded.PrimaryModel);
        Assert.AreEqual(1, loaded.Providers.Count);
        Assert.AreEqual("deepseek", loaded.Providers[0].Name);
        Assert.AreEqual("https://api.deepseek.com", loaded.Providers[0].Endpoint);
        Assert.AreEqual(2, loaded.Providers[0].Models.Count);
        Assert.AreEqual("deepseek-v4-pro", loaded.Providers[0].Models[0].ModelName);
        Assert.IsFalse(loaded.Providers[0].Models[0].IsFlash);
        Assert.IsTrue(loaded.Providers[0].Models[1].IsFlash);
    }

    /// <summary>
    /// 文件不存在时加载应返回默认设置。
    /// </summary>
    [TestMethod]
    public async Task Load_WhenFileNotExists_ShouldReturnDefault()
    {
        var settingsFile = new FileInfo(Path.Join(_tempDir, "nonexistent.json"));
        var service = new SettingsService(settingsFile);

        AppSettings settings = await service.LoadAsync();

        Assert.IsNotNull(settings);
        Assert.AreEqual(10, settings.DefaultMaxRounds);
        Assert.IsNotNull(settings.PersistencePath);
        // 文件不存在时应写入默认配置
        settingsFile.Refresh();
        Assert.IsTrue(settingsFile.Exists);
    }

    /// <summary>
    /// AppSettings 转 AgentApiManagerConfiguration 应保持兼容。
    /// </summary>
    [TestMethod]
    public void ToApiConfiguration_ShouldPreserveProviderInfo()
    {
        var settings = new AppSettings
        {
            PrimaryModel = "deepseek/deepseek-v4-pro",
            Providers =
            [
                new ProviderSetting
                {
                    Name = "deepseek",
                    Endpoint = "https://api.deepseek.com",
                    Key = "test-key",
                    Models = [new ModelSetting { ModelName = "deepseek-v4-pro" }],
                },
            ],
        };

        var config = SettingsService.ToApiConfiguration(settings);

        Assert.AreEqual("deepseek/deepseek-v4-pro", config.PrimaryModel);
        Assert.IsNotNull(config.OpenAIConfigurationList);
        Assert.AreEqual(1, config.OpenAIConfigurationList!.Count);

        var openAiConfig = config.OpenAIConfigurationList[0];
        Assert.AreEqual("https://api.deepseek.com", openAiConfig.EndPoint);
        Assert.AreEqual("test-key", openAiConfig.Key);
        Assert.IsNotNull(openAiConfig.ModelDefinitions);
        Assert.AreEqual(1, openAiConfig.ModelDefinitions!.Count);
        Assert.AreEqual("deepseek", openAiConfig.ModelDefinitions[0].Provider);
        Assert.AreEqual("deepseek-v4-pro", openAiConfig.ModelDefinitions[0].ModelName);
    }

    /// <summary>
    /// AgentApiManagerConfiguration 转 AppSettings 应保持兼容。
    /// </summary>
    [TestMethod]
    public void FromApiConfiguration_ShouldPreserveProviderInfo()
    {
        var settings = new AppSettings
        {
            PrimaryModel = "test/model",
            Providers =
            [
                new ProviderSetting
                {
                    Name = "test",
                    Endpoint = "https://test.api",
                    Key = "key123",
                    Models =
                    [
                        new ModelSetting { ModelName = "model-a", IsFlash = true },
                    ],
                },
            ],
        };

        var config = SettingsService.ToApiConfiguration(settings);
        AppSettings roundTripped = SettingsService.FromApiConfiguration(config);

        Assert.AreEqual("test/model", roundTripped.PrimaryModel);
        Assert.AreEqual(1, roundTripped.Providers.Count);
        Assert.AreEqual("test", roundTripped.Providers[0].Name);
        Assert.AreEqual("https://test.api", roundTripped.Providers[0].Endpoint);
        Assert.AreEqual("key123", roundTripped.Providers[0].Key);
        Assert.AreEqual(1, roundTripped.Providers[0].Models.Count);
        Assert.AreEqual("model-a", roundTripped.Providers[0].Models[0].ModelName);
        Assert.IsTrue(roundTripped.Providers[0].Models[0].IsFlash);
    }
}
