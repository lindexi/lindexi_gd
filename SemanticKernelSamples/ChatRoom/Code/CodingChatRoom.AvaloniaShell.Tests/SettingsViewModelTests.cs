using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class SettingsViewModelTests
{
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
        var settingsService = new CodingChatSettingsService(CodingChatRoomPaths.Create(rootDirectory));
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
