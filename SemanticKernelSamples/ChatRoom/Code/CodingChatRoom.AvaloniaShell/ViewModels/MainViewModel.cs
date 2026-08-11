using System;
using System.Threading.Tasks;
using System.Windows.Input;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 组合历史会话、聊天区域与应用设置。
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly SettingsViewModel? _settingsViewModel;
    private bool _isSettingsOpen;

    /// <summary>
    /// 初始化 Shell 视图模型。
    /// </summary>
    public MainViewModel()
        : this(new SessionListViewModel(), new ChatViewModel())
    {
    }

    /// <summary>
    /// 使用指定子 ViewModel 初始化 Shell。
    /// </summary>
    public MainViewModel(SessionListViewModel sessionListViewModel, ChatViewModel chatViewModel)
    {
        ArgumentNullException.ThrowIfNull(sessionListViewModel);
        ArgumentNullException.ThrowIfNull(chatViewModel);

        SessionListViewModel = sessionListViewModel;
        ChatViewModel = chatViewModel;
        OpenSettingsCommand = new SimpleAsyncCommand(static () => Task.CompletedTask, static () => false);
    }

    internal MainViewModel(
        SessionListViewModel sessionListViewModel,
        ChatViewModel chatViewModel,
        CodingChatSettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(sessionListViewModel);
        ArgumentNullException.ThrowIfNull(chatViewModel);
        ArgumentNullException.ThrowIfNull(settingsService);

        SessionListViewModel = sessionListViewModel;
        ChatViewModel = chatViewModel;
        _settingsViewModel = new SettingsViewModel(settingsService, CloseSettings);
        OpenSettingsCommand = new SimpleAsyncCommand(OpenSettingsAsync, () => !IsSettingsOpen);
    }

    /// <summary>
    /// 获取历史会话列表 ViewModel。
    /// </summary>
    public SessionListViewModel SessionListViewModel { get; }

    /// <summary>
    /// 获取聊天区域 ViewModel。
    /// </summary>
    public ChatViewModel ChatViewModel { get; }

    /// <summary>
    /// 获取设置区域 ViewModel。
    /// </summary>
    public SettingsViewModel? SettingsViewModel => _settingsViewModel;

    /// <summary>
    /// 获取当前是否显示设置页面。
    /// </summary>
    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        private set
        {
            if (SetField(ref _isSettingsOpen, value))
            {
                OnPropertyChanged(nameof(IsChatOpen));
                if (OpenSettingsCommand is SimpleAsyncCommand command)
                {
                    command.RaiseCanExecuteChanged();
                }
            }
        }
    }

    /// <summary>
    /// 获取当前是否显示聊天页面。
    /// </summary>
    public bool IsChatOpen => !IsSettingsOpen;

    /// <summary>
    /// 获取打开设置页面的命令。
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    private async Task OpenSettingsAsync()
    {
        if (_settingsViewModel is null)
        {
            return;
        }

        IsSettingsOpen = true;
        await _settingsViewModel.LoadAsync().ConfigureAwait(true);
    }

    private void CloseSettings() => IsSettingsOpen = false;
}
