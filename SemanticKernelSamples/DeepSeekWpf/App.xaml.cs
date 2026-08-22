using System.Windows;
using System.Windows.Threading;
using DeepSeekWpf.Services;
using DeepSeekWpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeepSeekWpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private IAppLogger? _logger;
    private Task? _shutdownTask;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegisterGlobalExceptionHandlers();

        try
        {
            var builder = Host.CreateApplicationBuilder(e.Args);
            builder.Logging.ClearProviders();
            ConfigureServices(builder.Services);

            _host = builder.Build();
            _logger = _host.Services.GetRequiredService<IAppLogger>();

            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            await settingsService.InitializeAsync();

            await _logger.InitializeAsync();
            if (settingsService.LastLoadError is not null)
            {
                await _logger.WarningAsync(
                    "设置文件无法解析，已恢复并保存默认设置",
                    settingsService.LastLoadError);
            }

            await _host.StartAsync();

            var modelService = _host.Services.GetRequiredService<IAgentModelService>();
            await modelService.ReloadAsync();
            if (!string.IsNullOrWhiteSpace(settingsService.CurrentSettings.SelectedModelSpecifier))
            {
                try
                {
                    modelService.SelectModel(settingsService.CurrentSettings.SelectedModelSpecifier);
                }
                catch (ArgumentException exception)
                {
                    await _logger.WarningAsync(
                        $"已保存的模型不再可用，将使用 Agent 配置中的主模型：{settingsService.CurrentSettings.SelectedModelSpecifier}",
                        exception);
                }
            }

            var workspaceViewModel = _host.Services.GetRequiredService<ChatWorkspaceViewModel>();
            await workspaceViewModel.InitializeAsync();

            if (modelService.SelectedModel is null)
            {
                workspaceViewModel.StatusMessage = $"尚未配置可用模型，请编辑 {modelService.ConfigurationFilePath}";
                await _logger.WarningAsync($"Agent 配置中没有可用模型：{modelService.ConfigurationFilePath}");
            }

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            LogException("应用启动失败", exception);
            MessageBox.Show(
                $"应用启动失败：{exception.Message}{Environment.NewLine}{Environment.NewLine}" +
                "请检查本地配置文件、目录权限和日志后重试。",
                "DeepSeekWpf 启动失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            await StopHostAsync();
            Shutdown(1);
        }
    }

    public Task RequestShutdownAsync()
    {
        return _shutdownTask ??= ShutdownCoreAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        UnregisterGlobalExceptionHandlers();
        _host?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAppLogger, FileAppLogger>();
        services.AddSingleton<ILoggerProvider, AppLoggerProvider>();
        services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
        services.AddSingleton<IChatRepository, FileChatRepository>();
        services.AddSingleton<IUserInteractionService, WpfUserInteractionService>();
        services.AddSingleton<IModelConnectionTestService, ModelConnectionTestService>();
        services.AddSingleton<IAgentApiEndpointManagerFactory, AgentApiEndpointManagerFactory>();
        services.AddSingleton<IAgentModelService, AgentModelService>();
        services.AddSingleton<IAiChatService, AgentAiChatService>();
        services.AddSingleton<ChatWorkspaceViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private async Task ShutdownCoreAsync()
    {
        try
        {
            if (_host is not null)
            {
                await _host.Services.GetRequiredService<ChatWorkspaceViewModel>().ShutdownAsync();
            }

            await StopHostAsync();

            if (_host is not null)
            {
                await _host.Services.GetRequiredService<IAppLogger>().StopAsync();
            }
        }
        catch (Exception exception)
        {
            LogException("应用关闭失败", exception);
            MessageBox.Show(
                $"应用关闭时保存失败：{exception.Message}{Environment.NewLine}" +
                "请检查日志和数据目录权限。",
                "DeepSeekWpf 关闭失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async Task StopHostAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
        }
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void UnregisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("Dispatcher 未处理异常", e.Exception);
        MessageBox.Show(
            $"发生未处理错误：{e.Exception.Message}{Environment.NewLine}" +
            "请保存当前操作并查看日志。",
            "DeepSeekWpf 错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("未观察到的任务异常", e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogException("AppDomain 未处理异常", exception);
        }
        else
        {
            if (_logger is not null)
            {
                _ = _logger.ErrorAsync($"AppDomain 未处理异常：{e.ExceptionObject}");
            }
        }
    }

    private void LogException(string context, Exception exception)
    {
        if (_logger is not null)
        {
            _ = _logger.ErrorAsync(context, exception);
        }
    }
}

