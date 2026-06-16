using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ChatRoomAvaloniaDemo.Models;
using ChatRoomAvaloniaDemo.Services;
using ChatRoomAvaloniaDemo.ViewModels;
using System;
using System.IO;

namespace ChatRoomAvaloniaDemo;

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
            var appConfig = new AppConfig
            {
                PersistenceBasePath = Path.Join(AppContext.BaseDirectory, "chatroom_data"),
            };

            var chatRoomService = new ChatRoomService();
            chatRoomService.ApplyConfig(appConfig);

            var mainViewModel = new MainViewModel(chatRoomService);

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}