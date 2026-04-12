using System.Windows;
using DeepSeekWpf.Services;
using DeepSeekWpf.ViewModels;

namespace DeepSeekWpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var logger = new FileAppLogger(settingsService);
        var chatRepository = new FileChatRepository(settingsService);
        var aiChatService = new MockAiChatService();

        var chatWorkspaceViewModel = new ChatWorkspaceViewModel(
            aiChatService,
            chatRepository,
            settingsService,
            logger);

        var settingsViewModel = new SettingsViewModel(
            settingsService,
            chatWorkspaceViewModel,
            logger);

        var mainWindowViewModel = new MainWindowViewModel(
            chatWorkspaceViewModel,
            settingsViewModel);

        var mainWindow = new MainWindow(mainWindowViewModel);
        mainWindow.Show();
    }
}

