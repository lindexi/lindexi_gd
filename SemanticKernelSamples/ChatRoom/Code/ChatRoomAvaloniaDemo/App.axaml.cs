using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ChatRoomAvaloniaDemo.Services;
using ChatRoomAvaloniaDemo.ViewModels;

using System.Threading.Tasks;

namespace ChatRoomAvaloniaDemo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var appConfig = await AppConfigService.LoadOrInitializeAsync();

            var chatRoomService = new ChatRoomService();
            chatRoomService.ApplyConfig(appConfig);

            var mainViewModel = new MainViewModel(chatRoomService);

            // 加载历史会话并创建初始会话（含默认"助手"角色）
            await mainViewModel.InitializeAsync();

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}