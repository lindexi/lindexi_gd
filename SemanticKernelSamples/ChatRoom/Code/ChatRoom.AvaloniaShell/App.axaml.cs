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
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsFile = new FileInfo(Path.Join(appData, "AgentRoundtable", "AppConfiguration.json"));

        // 1. 主线程调度器
        var dispatcher = new AvaloniaMainThreadDispatcher();

        // 2. 设置服务
        var settingsService = new SettingsService(settingsFile);
        AppSettings appSettings = await settingsService.LoadAsync();

        // 3. 模型提供商服务
        var modelProviderService = new ModelProviderService(appSettings);

        // 4. 会话服务
        string persistencePath = string.IsNullOrWhiteSpace(appSettings.PersistencePath)
            ? Path.Join(appData, "AgentRoundtable", "Sessions")
            : appSettings.PersistencePath;

        var sessionService = new SessionService(
            new ChatRoomPersistence(persistencePath));

        // 5. 聊天室服务
        var chatRoomService = new ChatRoomService(
            dispatcher,
            modelProviderService,
            persistencePath,
            appSettings.DefaultMaxRounds);

        // 5.1 角色模板服务
        string roleTemplatesPath = string.IsNullOrWhiteSpace(appSettings.RoleTemplatesPath)
            ? Path.Join(appData, "AgentRoundtable", "RoleTemplates")
            : appSettings.RoleTemplatesPath;
        var roleTemplateService = new RoleTemplateService(roleTemplatesPath);
        await roleTemplateService.EnsurePresetTemplatesAsync();

        // 6. 主 ViewModel
        var mainViewModel = new MainViewModel(
            chatRoomService,
            settingsService,
            modelProviderService,
            sessionService,
            roleTemplateService);

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