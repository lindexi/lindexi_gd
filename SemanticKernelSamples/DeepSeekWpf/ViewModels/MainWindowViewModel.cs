using DeepSeekWpf.Infrastructure;

namespace DeepSeekWpf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly RelayCommand _showChatCommand;
    private readonly RelayCommand _showSettingsCommand;
    private readonly RelayCommand _newChatCommand;
    private readonly RelayCommand _focusSearchCommand;
    private object _currentPage;

    public MainWindowViewModel(ChatWorkspaceViewModel chatPage, SettingsViewModel settingsPage)
    {
        ChatPage = chatPage;
        SettingsPage = settingsPage;
        _currentPage = chatPage;
        _showChatCommand = new RelayCommand(ShowChat);
        _showSettingsCommand = new RelayCommand(ShowSettings);
        _newChatCommand = new RelayCommand(CreateNewChat, () => ChatPage.NewChatCommand.CanExecute(null));
        _focusSearchCommand = new RelayCommand(FocusSearch);
        ChatPage.NewChatCommand.CanExecuteChanged += (_, _) => _newChatCommand.NotifyCanExecuteChanged();
        SettingsPage.SaveCompleted += OnSettingsSaved;
    }

    public event EventHandler? FocusSearchRequested;

    public ChatWorkspaceViewModel ChatPage { get; }

    public SettingsViewModel SettingsPage { get; }

    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public RelayCommand ShowChatCommand => _showChatCommand;

    public RelayCommand ShowSettingsCommand => _showSettingsCommand;

    public RelayCommand NewChatCommand => _newChatCommand;

    public RelayCommand FocusSearchCommand => _focusSearchCommand;

    private void ShowChat()
    {
        CurrentPage = ChatPage;
    }

    private void ShowSettings()
    {
        SettingsPage.ReloadFromServices();
        CurrentPage = SettingsPage;
    }

    private void CreateNewChat()
    {
        ShowChat();
        ChatPage.NewChatCommand.Execute(null);
    }

    private void FocusSearch()
    {
        ShowChat();
        FocusSearchRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        ChatPage.RefreshSettings();
        ShowChat();
    }
}
