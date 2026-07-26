using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class DiagnosticsTests
{
    [TestMethod]
    public async Task CreateSummaryAsync_ContainsEnvironmentMetadataButNotChatBodyOrApiKey()
    {
        using var temp = new TempDirectory();
        var settings = new FakeSettingsService(temp.Path);
        var model = new DeepSeekWpf.Models.AgentModelDescriptor("fake/model", "fake", "model", null, null, null, null);
        await settings.SaveAsync(settings.CurrentSettings with { SelectedModelSpecifier = model.Specifier });
        var modelService = new FakeAgentModelService
        {
            ConfigurationFilePath = temp.GetPath("AgentConfiguration.json"),
            RegisteredModels = [model],
            SelectedModel = model,
        };
        var service = new DiagnosticsService(settings, modelService, new FakeAppLogger());

        var summary = await service.CreateSummaryAsync();

        StringAssert.Contains(summary, "DeepSeekWpf 本地诊断摘要");
        StringAssert.Contains(summary, "已注册模型数: 1");
        StringAssert.Contains(summary, "fake/model");
        Assert.IsFalse(summary.Contains("聊天正文", StringComparison.Ordinal));
        Assert.IsFalse(summary.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(summary.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }
}