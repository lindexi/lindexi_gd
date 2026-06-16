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