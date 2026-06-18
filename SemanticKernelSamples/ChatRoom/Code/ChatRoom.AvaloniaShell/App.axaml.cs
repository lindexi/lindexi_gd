using System;
using System.IO;
using System.Threading.Tasks;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Configuration;
using AgentLib.ChatRoom.Services;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using ChatRoom.AvaloniaShell.Infrastructure;
using ChatRoom.AvaloniaShell.ViewModels;
using ChatRoom.AvaloniaShell.Views;

namespace ChatRoom.AvaloniaShell;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = InitializeApp(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeApp(IClassicDesktopStyleApplicationLifetime desktop)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string settingsFilePath = Path.Join(appData, "ChatRoom", "settings.json");

        // 1. 主线程调度器
        var dispatcher = new AvaloniaMainThreadDispatcher();

        // 2. 设置服务
        var settingsService = new SettingsService(settingsFilePath);
        AppSettings appSettings = await settingsService.LoadAsync();

        // 3. 模型提供商服务
        var modelProviderService = new ModelProviderService(appSettings);

        // 4. 会话服务
        string persistencePath = string.IsNullOrWhiteSpace(appSettings.PersistencePath)
            ? Path.Join(appData, "ChatRoom", "Sessions")
            : appSettings.PersistencePath;

        var sessionService = new SessionService(
            new ChatRoomPersistence(persistencePath));

        // 5. 聊天室服务
        var chatRoomService = new ChatRoomService(
            dispatcher,
            modelProviderService,
            persistencePath,
            appSettings.DefaultMaxRounds);

        // 6. 主 ViewModel
        var mainViewModel = new MainViewModel(
            chatRoomService,
            settingsService,
            modelProviderService,
            sessionService);

        // 7. 初始化：创建默认会话和助手角色
        await mainViewModel.InitializeAsync().ConfigureAwait(true);

        // 8. 主视图（必须在 UI 线程创建）
        var mainView = new MainView
        {
            DataContext = mainViewModel,
        };

        var mainWindow = new MainWindow
        {
            Content = mainView,
        };
        desktop.MainWindow = mainWindow;

        mainWindow.Show();
    }
}