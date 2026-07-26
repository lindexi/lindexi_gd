using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;
using DeepSeekWpf.ViewModels;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public Task ReloadAndSave_SelectedModelPersistsAndUpdatesAgentService() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var model = CreateModel("fake/alpha");
        var modelService = new FakeAgentModelService { RegisteredModels = [model] };
        var settings = new FakeSettingsService(temp.Path);
        var workspace = CreateWorkspace(temp, modelService);
        var viewModel = CreateViewModel(settings, workspace, modelService);

        await viewModel.ReloadAgentConfigurationCommand.ExecuteAsync();
        viewModel.SelectedModel = model;
        await viewModel.SaveSettingsCommand.ExecuteAsync();

        Assert.AreEqual("fake/alpha", settings.CurrentSettings.SelectedModelSpecifier);
        Assert.AreEqual("fake/alpha", modelService.SelectedModel?.Specifier);
    });

    [TestMethod]
    public Task TestConnectionCommand_UpdatesBusyStateAndStatus()
        => StaTest.RunAsync(async () =>
        {
            using var temp = new TempDirectory();
            var model = CreateModel("fake/alpha");
            var modelService = new FakeAgentModelService { RegisteredModels = [model] };
            var connection = new FakeModelConnectionTestService
            {
                Result = new ModelConnectionTestResult(false, AiChatErrorCategory.Network, "网络失败"),
            };
            var viewModel = CreateViewModel(
                new FakeSettingsService(temp.Path),
                CreateWorkspace(temp, modelService),
                modelService,
                connection);
            viewModel.SelectedModel = model;

            await viewModel.TestConnectionCommand.ExecuteAsync();

            Assert.AreEqual("网络失败", viewModel.StatusMessage);
            Assert.AreEqual("fake/alpha", modelService.SelectedModel?.Specifier);
        });

    [TestMethod]
    public Task DiagnosticsCommands_CopySummaryAndClearLogsOnConfirmation() => StaTest.RunAsync(async () =>
    {
        using var temp = new TempDirectory();
        var modelService = new FakeAgentModelService();
        var interaction = new FakeUserInteractionService { ConfirmationResult = true };
        var diagnostics = new FakeDiagnosticsService { Summary = "safe summary" };
        var viewModel = CreateViewModel(
            new FakeSettingsService(temp.Path),
            CreateWorkspace(temp, modelService),
            modelService,
            diagnostics: diagnostics,
            interaction: interaction);

        await viewModel.CopyDiagnosticsCommand.ExecuteAsync();
        await viewModel.ClearLogsCommand.ExecuteAsync();

        Assert.AreEqual("safe summary", interaction.CopiedText);
        Assert.AreEqual("日志已清理", viewModel.StatusMessage);
    });

    private static SettingsViewModel CreateViewModel(
        FakeSettingsService settings,
        ChatWorkspaceViewModel workspace,
        FakeAgentModelService modelService,
        FakeModelConnectionTestService? connection = null,
        FakeDiagnosticsService? diagnostics = null,
        FakeUserInteractionService? interaction = null) =>
        new(
            settings,
            workspace,
            modelService,
            connection ?? new FakeModelConnectionTestService(),
            diagnostics ?? new FakeDiagnosticsService(),
            interaction ?? new FakeUserInteractionService(),
            new FakeAppLogger());

    private static ChatWorkspaceViewModel CreateWorkspace(TempDirectory temp, FakeAgentModelService modelService) =>
        new(
            new FakeAiChatService(),
            new FakeChatRepository(),
            new FakeSettingsService(temp.Path),
            modelService,
            new FakeUserInteractionService(),
            new FakeChatExportService(),
            new FakeAppLogger());

    private static AgentModelDescriptor CreateModel(string specifier) =>
        new(specifier, "fake", specifier.Split('/').Last(), null, null, null, null);
}