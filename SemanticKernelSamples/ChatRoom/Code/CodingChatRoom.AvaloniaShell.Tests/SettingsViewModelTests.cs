using AgentLib.Coding.Sandboxes;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod(DisplayName = "保存设置后应立即更新下一轮使用的沙盒工具配置")]
    public async Task SaveSettingsShouldUpdateRuntimeSandboxToolSource()
    {
        string rootDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string workspacePath = Path.Join(rootDirectory, "workspace");
        Directory.CreateDirectory(workspacePath);
        var sandboxToolSource = new WindowsSandboxToolSource("OldShell.exe", "127.0.0.1:12399");
        var settingsService = new CodingChatSettingsService(
            CodingChatRoomPaths.Create(rootDirectory),
            sandboxToolSource);
        var modelConfiguration = new AgentApiManagerConfiguration();
        var shellSettings = new CodingChatShellSettings
        {
            IsWindowsSandboxEnabled = false,
            WindowsSandboxToolPath = string.Empty,
            WindowsSandboxServerAddress = string.Empty,
        };

        await settingsService.SaveAsync(modelConfiguration, shellSettings);

        Assert.IsEmpty(sandboxToolSource.CreateTools(workspacePath));
    }

    [TestMethod(DisplayName = "任一工作任务保存设置后所有任务应使用最新沙箱配置")]
    public async Task SaveSettingsShouldUpdateSandboxConfigurationSharedByAllWorkTasks()
    {
        string rootDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string workspacePath = Path.Join(rootDirectory, "workspace");
        Directory.CreateDirectory(workspacePath);
        var sandboxToolSource = new WindowsSandboxToolSource("OldShell.exe", "127.0.0.1:12399");
        var firstTaskSettingsService = new CodingChatSettingsService(
            CodingChatRoomPaths.Create(rootDirectory),
            sandboxToolSource);
        _ = new CodingChatSettingsService(
            CodingChatRoomPaths.Create(rootDirectory),
            sandboxToolSource);
        var shellSettings = new CodingChatShellSettings
        {
            IsWindowsSandboxEnabled = false,
            WindowsSandboxToolPath = string.Empty,
            WindowsSandboxServerAddress = string.Empty,
        };

        await firstTaskSettingsService.SaveAsync(new AgentApiManagerConfiguration(), shellSettings);

        Assert.IsEmpty(sandboxToolSource.CreateTools(workspacePath));
    }

    [TestMethod(DisplayName = "模型配置损坏时应清空上次加载的模型并显示错误")]
    public async Task InvalidModelConfigurationShouldClearPreviouslyLoadedModels()
    {
        string rootDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(rootDirectory);
        paths.EnsureDirectories();
        var configuration = new AgentApiManagerConfiguration
        {
            PrimaryModel = "test/model",
            OpenAIConfigurationList =
            [
                new OpenAIProtocolLanguageModelConfiguration("https://example.com", "key")
                {
                    ModelDefinitions =
                    [
                        new AgentLib.Core.AgentApiManagers.Contexts.ModelDefinition
                        {
                            Provider = "test",
                            ModelName = "model",
                        },
                    ],
                },
            ],
        };
        await configuration.SaveToFileAsync(paths.ConfigurationFile);
        var settingsService = new CodingChatSettingsService(
            paths,
            new WindowsSandboxToolSource(false, string.Empty, string.Empty));
        var viewModel = new SettingsViewModel(settingsService, static () => { });
        await viewModel.LoadAsync();
        await File.WriteAllTextAsync(paths.ConfigurationFile.FullName, "{ invalid json");

        await viewModel.LoadAsync();

        Assert.IsNull(viewModel.PrimaryModel);
        Assert.HasCount(1, viewModel.Providers);
        Assert.AreNotEqual("https://example.com", viewModel.Providers[0].EndPoint);
        Assert.IsTrue(viewModel.IsStatusError);
        StringAssert.StartsWith(viewModel.StatusMessage, "现有模型配置无法读取：");
    }

    [TestMethod(DisplayName = "测试沙箱连接时应立即显示连接中提示")]
    [Timeout(5000)]
    public async Task WhenTestingSandboxConnectionThenConnectingMessageIsShownImmediately()
    {
        var tester = new ControllableWindowsSandboxConnectionTester();
        SettingsViewModel viewModel = CreateViewModel(tester);

        viewModel.TestWindowsSandboxConnectionCommand.Execute(null);
        await tester.Started.Task;

        Assert.AreEqual("尝试连接沙箱中…", viewModel.SandboxConnectionStatusMessage);
    }

    [TestMethod(DisplayName = "沙箱连接测试完成后应显示测试结果")]
    [Timeout(5000)]
    public async Task WhenSandboxConnectionTestCompletesThenResultIsShown()
    {
        var tester = new ControllableWindowsSandboxConnectionTester();
        SettingsViewModel viewModel = CreateViewModel(tester);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.SandboxConnectionStatusMessage)
                && viewModel.SandboxConnectionStatusMessage == "沙箱连接测试成功。")
            {
                completed.TrySetResult();
            }
        };

        viewModel.TestWindowsSandboxConnectionCommand.Execute(null);
        await tester.Started.Task;
        tester.Complete(new WindowsSandboxConnectionTestResult(true, "沙箱连接测试成功。"));
        await completed.Task;

        Assert.AreEqual("沙箱连接测试成功。", viewModel.SandboxConnectionStatusMessage);
    }

    private static SettingsViewModel CreateViewModel(IWindowsSandboxConnectionTester tester)
    {
        string rootDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sandboxToolSource = new WindowsSandboxToolSource(false, string.Empty, string.Empty);
        var settingsService = new CodingChatSettingsService(
            CodingChatRoomPaths.Create(rootDirectory),
            sandboxToolSource);
        return new SettingsViewModel(settingsService, tester, static () => { });
    }

    private sealed class ControllableWindowsSandboxConnectionTester : IWindowsSandboxConnectionTester
    {
        private readonly TaskCompletionSource<WindowsSandboxConnectionTestResult> _result =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<WindowsSandboxConnectionTestResult> TestAsync(
            string toolPath,
            string serverAddress,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return _result.Task.WaitAsync(cancellationToken);
        }

        public void Complete(WindowsSandboxConnectionTestResult result) => _result.TrySetResult(result);
    }
}
