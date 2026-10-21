using AgentLib.Coding.Sandboxes;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.Settings;
using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class DeepSeekSettingsTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow(null, "https://api.deepseek.com/v1", "official-key")]
    [DataRow("explicit-key", "https://api.deepseek.com/v1", "explicit-key")]
    [DataRow(null, "https://third-party.example/v1", null)]
    public async Task WhenLoadingSettingsThenOnlyOfficialServiceCanFillMissingKey(string? savedKey, string endpoint, string? expected)
    {
        var service = CreateService();
        await service.SaveAsync(Configuration(endpoint), new CodingChatShellSettings { DeepSeekApiKey = savedKey });
        var viewModel = new SettingsViewModel(service, static () => { });

        await viewModel.LoadAsync();

        Assert.AreEqual(expected, viewModel.DeepSeekApiKey);
    }

    [TestMethod]
    [DataRow(" explicit-key ", "explicit-key")]
    [DataRow(null, "official-key")]
    [DataRow(" ", "official-key")]
    public async Task WhenSavingKeyThenNewViewModelLoadsPersistedResolvedKey(string? input, string expected)
    {
        var service = CreateService();
        await service.SaveAsync(Configuration("https://api.deepseek.com/v1"), new CodingChatShellSettings());
        var viewModel = new SettingsViewModel(service, static () => { });
        await viewModel.LoadAsync();
        viewModel.DeepSeekApiKey = input;

        await ((SimpleAsyncCommand)viewModel.SaveCommand).ExecuteAsync();
        var reloaded = new SettingsViewModel(service, static () => { });
        await reloaded.LoadAsync();

        Assert.AreEqual(expected, reloaded.DeepSeekApiKey);
    }

    [TestMethod]
    public async Task WhenSavingExplicitKeyThenShellSettingsFileContainsKey()
    {
        var service = CreateService();
        var viewModel = new SettingsViewModel(service, static () => { });
        await viewModel.LoadAsync();
        viewModel.DeepSeekApiKey = "persisted-key";

        await ((SimpleAsyncCommand)viewModel.SaveCommand).ExecuteAsync();

        Assert.AreEqual("persisted-key", (await service.LoadShellSettingsAsync()).DeepSeekApiKey);
    }

    [TestMethod]
    public void WhenKeyChangesThenBindingIsNotified()
    {
        var viewModel = new SettingsViewModel();
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        viewModel.DeepSeekApiKey = "new-key";

        CollectionAssert.Contains(notifications, nameof(SettingsViewModel.DeepSeekApiKey));
    }

    [TestMethod]
    public async Task WhenSavingAutomaticKeyThenProviderChangesStillUpdateIt()
    {
        var service = CreateService();
        await service.SaveAsync(Configuration("https://api.deepseek.com/v1"), new CodingChatShellSettings());
        using var settings = new SettingsViewModel(service, static () => { });
        await settings.LoadAsync();
        await ((SimpleAsyncCommand)settings.SaveCommand).ExecuteAsync();
        settings.Providers[0].ApiKey = "updated-test-key";
        Assert.AreEqual("updated-test-key", settings.DeepSeekApiKey);
    }

    private CodingChatSettingsService CreateService()
    {
        string root = Path.Join(Path.GetTempPath(), "PrismDeepSeekTests", Guid.NewGuid().ToString("N"));
        TestContext.WriteLine(root);
        return new CodingChatSettingsService(CodingChatRoomPaths.Create(root), new WindowsSandboxToolSource(false, string.Empty, string.Empty));
    }

    private static AgentApiManagerConfiguration Configuration(string endpoint) => new()
    {
        OpenAIConfigurationList = [new OpenAIProtocolLanguageModelConfiguration(endpoint, "official-key")
        {
            ModelDefinitions = [new ModelDefinition { Provider = "test", ModelName = "deepseek-chat" }],
        }],
    };
}
