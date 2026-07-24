using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell;

public partial class App : Application
{
    private CodingChatRuntime? _runtime;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            InitializeApp(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void InitializeApp(IClassicDesktopStyleApplicationLifetime desktop)
    {
        CodingChatRoomPaths paths = CodingChatRoomPaths.CreateForCurrentUser();
        try
        {
            _runtime = await CodingChatStartup.InitializeAsync(
                paths,
                new AvaloniaMainThreadDispatcher()).ConfigureAwait(true);

            var mainViewModel = new MainViewModel(
                new SessionListViewModel(_runtime.Application),
                new ChatViewModel(
                    _runtime.ChatManager,
                    _runtime.Application,
                    _runtime.WorkspaceController,
                    $"当前模型：{_runtime.ModelDisplayName}"));
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            Trace.TraceError($"CodingChatRoom 应用初始化失败：{exception}");
            var failureWindow = new StartupFailureWindow
            {
                DataContext = new StartupFailureViewModel(
                    paths.ConfigurationFile.FullName,
                    exception,
                    () => desktop.TryShutdown(1)),
            };
            desktop.MainWindow = failureWindow;
            failureWindow.Show();
        }
    }
}