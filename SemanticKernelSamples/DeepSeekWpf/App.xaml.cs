using System.Windows;
using System.Windows.Threading;
using System.IO;
using AgentLib;
using AgentLib.Logging;
using DeepSeekWpf.Infrastructure;
using DeepSeekWpf.Services;
using DeepSeekWpf.ViewModels;

namespace DeepSeekWpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IAppLogger? _logger;
    private CopilotChatManager? _chatManager;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var logger = new FileAppLogger(settingsService);
        _logger = logger;
        RegisterGlobalExceptionHandlers();

        try
        {
            var agentConfigurationService = new AgentConfigurationService(logger);
            var configurationResult = await agentConfigurationService.LoadAsync();
            var chatLogger = new FileCopilotChatLogger(
                settingsService.CurrentSettings.LogPath,
                Path.Combine(settingsService.CurrentSettings.DataPath, "ChatHistory"));
            var chatManager = new CopilotChatManager(chatLogger)
            {
                MainThreadDispatcher = new WpfMainThreadDispatcher(Dispatcher),
            };
            chatManager.AgentApiEndpointManager.LoadConfiguration(configurationResult.Configuration);
            _chatManager = chatManager;

            var chatWorkspaceViewModel = new ChatWorkspaceViewModel(chatManager, logger);
            chatWorkspaceViewModel.SetConfigurationStatus(configurationResult);

            var settingsViewModel = new SettingsViewModel(
                settingsService,
                agentConfigurationService,
                chatWorkspaceViewModel,
                logger);

            var mainWindowViewModel = new MainWindowViewModel(
                chatWorkspaceViewModel,
                settingsViewModel);

            var mainWindow = new MainWindow(mainWindowViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();
            logger.Log($"应用启动完成，Agent 配置来源：{configurationResult.SourceDescription}");
        }
        catch (Exception exception)
        {
            logger.Log($"应用启动失败：{exception}");
            MessageBox.Show(
                $"DeepSeekWpf 启动失败。\n\n{exception.Message}\n\n详细信息已写入日志目录：\n{settingsService.CurrentSettings.LogPath}",
                "DeepSeekWpf",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _chatManager?.CancelCurrentChat();
        base.OnExit(e);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Log($"UI 未处理异常：{e.Exception}");
        MessageBox.Show(
            $"应用发生未处理错误：\n{e.Exception.Message}",
            "DeepSeekWpf",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger?.Log($"进程未处理异常：{e.ExceptionObject}");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.Log($"未观察到的任务异常：{e.Exception}");
        e.SetObserved();
    }
}

