using DeepSeekWpf.Infrastructure;

namespace DeepSeekWpf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly RelayCommand _showChatCommand;
    private readonly RelayCommand _showSettingsCommand;
    private object _currentPage;

    public MainWindowViewModel(ChatWorkspaceViewModel chatPage, SettingsViewModel settingsPage)
    {
        ChatPage = chatPage;
        SettingsPage = settingsPage;
        _currentPage = chatPage;
        _showChatCommand = new RelayCommand(ShowChat);
        _showSettingsCommand = new RelayCommand(ShowSettings);
        SettingsPage.SaveCompleted += OnSettingsSaved;
    }

    public ChatWorkspaceViewModel ChatPage { get; }

    public SettingsViewModel SettingsPage { get; }

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public RelayCommand ShowChatCommand => _showChatCommand;

    public RelayCommand ShowSettingsCommand => _showSettingsCommand;

    private void ShowChat()
    {
        CurrentPage = ChatPage;
    }

    private void ShowSettings()
    {
        SettingsPage.Reload();
        CurrentPage = SettingsPage;
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        ShowChat();
    }
}
