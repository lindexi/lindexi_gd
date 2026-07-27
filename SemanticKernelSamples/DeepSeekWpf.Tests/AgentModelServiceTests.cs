using System.Text.Json;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class AgentModelServiceTests
{
    [TestMethod]
    public async Task ReloadAsync_MissingConfiguration_CreatesValidEmptyJson()
    {
        using var temp = new TempDirectory();
        var path = temp.GetPath("AgentConfiguration.json");
        var service = new AgentModelService(new FakeAgentApiEndpointManagerFactory(), new FakeAppLogger(), path);

        await service.ReloadAsync();

        Assert.IsTrue(File.Exists(path));
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.AreEqual(string.Empty, document.RootElement.GetProperty("PrimaryModel").GetString());
        Assert.AreEqual(0, document.RootElement.GetProperty("OpenAIConfigurationList").GetArrayLength());
        Assert.AreEqual(0, service.RegisteredModels.Count);
        Assert.IsNull(service.SelectedModel);
    }

    [TestMethod]
    public async Task ReloadAsync_FakeManager_EnumeratesAndSelectsModels()
    {
        using var temp = new TempDirectory();
        var path = temp.GetPath("AgentConfiguration.json");
        await File.WriteAllTextAsync(path, "{\"PrimaryModel\":\"\",\"OpenAIConfigurationList\":[]}");
        var manager = AgentLibFakes.CreateManager(("fake", "alpha", "a"), ("fake", "beta", "b"));
        var service = new AgentModelService(new FakeAgentApiEndpointManagerFactory(manager), new FakeAppLogger(), path);

        await service.ReloadAsync();
        service.SelectModel("fake/beta");

        CollectionAssert.AreEquivalent(
            new[] { "fake/alpha", "fake/beta" },
            service.RegisteredModels.Select(model => model.Specifier).ToArray());
        Assert.AreEqual("fake/beta", service.SelectedModel?.Specifier);
    }

    [TestMethod]
    public async Task ReloadAsync_ReplacesOldModels()
    {
        using var temp = new TempDirectory();
        var path = temp.GetPath("AgentConfiguration.json");
        await File.WriteAllTextAsync(path, "{\"PrimaryModel\":\"\",\"OpenAIConfigurationList\":[]}");
        var service = new AgentModelService(
            new FakeAgentApiEndpointManagerFactory(
                AgentLibFakes.CreateManager(("fake", "old", null)),
                AgentLibFakes.CreateManager(("fake", "new", null))),
            new FakeAppLogger(),
            path);

        await service.ReloadAsync();
        await service.ReloadAsync();

        Assert.AreEqual(1, service.RegisteredModels.Count);
        Assert.AreEqual("fake/new", service.RegisteredModels[0].Specifier);
    }

    [TestMethod]
    public void ResolveConfigurationFilePath_EnvironmentVariable_IsIsolatedAndRestored()
    {
        using var temp = new TempDirectory();
        var original = Environment.GetEnvironmentVariable(AgentModelService.ConfigurationPathEnvironmentVariable);
        try
        {
            var configured = temp.GetPath("custom.json");
            Environment.SetEnvironmentVariable(AgentModelService.ConfigurationPathEnvironmentVariable, configured);

            Assert.AreEqual(configured, AgentModelService.ResolveConfigurationFilePath());
        }
        finally
        {
            Environment.SetEnvironmentVariable(AgentModelService.ConfigurationPathEnvironmentVariable, original);
        }
    }

    [TestMethod]
    public void ResolveConfigurationFilePath_Default_UsesAgentConfigurationFileName()
    {
        var original = Environment.GetEnvironmentVariable(AgentModelService.ConfigurationPathEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(AgentModelService.ConfigurationPathEnvironmentVariable, null);

            Assert.AreEqual("AgentConfiguration.json", Path.GetFileName(AgentModelService.ResolveConfigurationFilePath()));
        }
        finally
        {
            Environment.SetEnvironmentVariable(AgentModelService.ConfigurationPathEnvironmentVariable, original);
        }
    }
}