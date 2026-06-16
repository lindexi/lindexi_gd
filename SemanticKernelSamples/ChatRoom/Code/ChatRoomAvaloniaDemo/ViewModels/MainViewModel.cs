using AgentLib;
using AgentLib.Model;

using ChatRoomAvaloniaDemo.Services;

using System;
using System.Threading.Tasks;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 聊天室界面导航视图枚举。
/// </summary>
public enum ChatRoomView
{
    /// <summary>主聊天界面。</summary>
    ChatRoom,

    /// <summary>设置界面。</summary>
    Settings,

    /// <summary>角色编辑界面。</summary>
    RoleEdit,
}

/// <summary>
/// 主 ViewModel。持有所有子 ViewModel 和 <see cref="ChatRoomService"/>，
/// 并管理界面导航状态。
/// </summary>
public sealed class MainViewModel : NotifyBase
{
    private readonly ChatRoomService _chatRoomService;
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private ChatRoomView _currentView = ChatRoomView.ChatRoom;
    private bool _isInitialized;

    /// <summary>
    /// 创建主 ViewModel。
    /// </summary>
    /// <param name="chatRoomService">聊天室服务。</param>
    /// <param name="mainThreadDispatcher">主线程调度器（可选）。</param>
    public MainViewModel(ChatRoomService chatRoomService, IMainThreadDispatcher? mainThreadDispatcher = null)
    {
        _chatRoomService = chatRoomService ?? throw new ArgumentNullException(nameof(chatRoomService));
        _mainThreadDispatcher = mainThreadDispatcher;

        SessionListViewModel = new SessionListViewModel(chatRoomService);
        ChatMessagesViewModel = new ChatMessagesViewModel(chatRoomService);
        RoleListViewModel = new RoleListViewModel(this, chatRoomService);

        NavigateToSettingsCommand = new DelegateCommand(NavigateToSettings);
        NavigateBackCommand = new DelegateCommand(NavigateBack);
        NavigateToRoleEditCommand = new DelegateCommand<RoleEditViewModel?>(NavigateToRoleEdit);
    }

    /// <summary>
    /// 历史会话列表 ViewModel。
    /// </summary>
    public SessionListViewModel SessionListViewModel { get; }

    /// <summary>
    /// 聊天消息 ViewModel。
    /// </summary>
    public ChatMessagesViewModel ChatMessagesViewModel { get; }

    /// <summary>
    /// 角色列表 ViewModel。
    /// </summary>
    public RoleListViewModel RoleListViewModel { get; }

    /// <summary>
    /// 设置页 ViewModel。延迟初始化。
    /// </summary>
    public SettingsViewModel? SettingsViewModel { get; private set; }

    /// <summary>
    /// 角色编辑 ViewModel。每次编辑时重新创建。
    /// </summary>
    public RoleEditViewModel? RoleEditViewModel { get; private set; }

    /// <summary>
    /// 当前显示的子视图。
    /// </summary>
    public ChatRoomView CurrentView
    {
        get => _currentView;
        private set
        {
            if (SetField(ref _currentView, value))
            {
                OnPropertyChanged(nameof(IsChatRoomVisible));
                OnPropertyChanged(nameof(IsSettingsVisible));
                OnPropertyChanged(nameof(IsRoleEditVisible));
            }
        }
    }

    /// <summary>主聊天界面是否可见。</summary>
    public bool IsChatRoomVisible => CurrentView == ChatRoomView.ChatRoom;

    /// <summary>设置界面是否可见。</summary>
    public bool IsSettingsVisible => CurrentView == ChatRoomView.Settings;

    /// <summary>角色编辑界面是否可见。</summary>
    public bool IsRoleEditVisible => CurrentView == ChatRoomView.RoleEdit;

    /// <summary>导航到设置界面。</summary>
    public DelegateCommand NavigateToSettingsCommand { get; }

    /// <summary>导航回主聊天界面。</summary>
    public DelegateCommand NavigateBackCommand { get; }

    /// <summary>导航到角色编辑界面。</summary>
    public DelegateCommand<RoleEditViewModel?> NavigateToRoleEditCommand { get; }

    /// <summary>
    /// 初始化主 ViewModel。加载历史会话列表，并创建一个新的空会话。
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await SessionListViewModel.LoadSessionsAsync();
    }

    private void NavigateToSettings()
    {
        SettingsViewModel ??= new SettingsViewModel(_chatRoomService.AppConfig);
        CurrentView = ChatRoomView.Settings;
    }

    /// <summary>
    /// 导航回主聊天界面。由 SettingsView / RoleEditView 的返回操作调用。
    /// </summary>
    public async void NavigateBack()
    {
        if (CurrentView == ChatRoomView.Settings && SettingsViewModel is not null)
        {
            SettingsViewModel.ApplyToConfig();

            // 保存配置到文件
            var config = _chatRoomService.AppConfig;
            if (!string.IsNullOrEmpty(config?.ConfigFilePath))
            {
                try
                {
                    await config.SaveAsync(config.ConfigFilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
                }
            }
        }

        CurrentView = ChatRoomView.ChatRoom;
    }

    private void NavigateToRoleEdit(RoleEditViewModel? roleEditVm)
    {
        RoleEditViewModel = roleEditVm;
        if (roleEditVm is not null)
        {
            CurrentView = ChatRoomView.RoleEdit;
        }
    }
}