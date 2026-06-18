using System;
using System.Threading.Tasks;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Model;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 页面类型枚举，用于主界面页面导航。
/// </summary>
public enum AppPage
{
    /// <summary>
    /// 主聊天界面。
    /// </summary>
    Chat,

    /// <summary>
    /// 设置界面。
    /// </summary>
    Settings,

    /// <summary>
    /// 角色编辑界面。
    /// </summary>
    RoleEdit,
}

/// <summary>
/// 主窗口 ViewModel。负责三栏协调和页面导航。
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly ChatRoomService _chatRoomService;
    private readonly SettingsService _settingsService;
    private AppPage _currentPage = AppPage.Chat;
    private RoleEditViewModel? _roleEditViewModel;
    private SettingsViewModel? _settingsViewModel;

    /// <summary>
    /// 聊天室服务。
    /// </summary>
    public ChatRoomService ChatRoomService => _chatRoomService;

    /// <summary>
    /// 设置服务。
    /// </summary>
    public SettingsService SettingsService => _settingsService;

    /// <summary>
    /// 会话列表 ViewModel。
    /// </summary>
    public SessionListViewModel SessionListViewModel { get; }

    /// <summary>
    /// 聊天 ViewModel。
    /// </summary>
    public ChatViewModel ChatViewModel { get; }

    /// <summary>
    /// 角色列表 ViewModel。
    /// </summary>
    public RoleListViewModel RoleListViewModel { get; }

    /// <summary>
    /// 当前显示的页面。
    /// </summary>
    public AppPage CurrentPage
    {
        get => _currentPage;
        private set => SetField(ref _currentPage, value);
    }

    /// <summary>
    /// 角色编辑 ViewModel。仅在 <see cref="CurrentPage"/> 为 <see cref="AppPage.RoleEdit"/> 时有效。
    /// </summary>
    public RoleEditViewModel? RoleEditViewModel
    {
        get => _roleEditViewModel;
        private set => SetField(ref _roleEditViewModel, value);
    }

    /// <summary>
    /// 设置 ViewModel。仅在 <see cref="CurrentPage"/> 为 <see cref="AppPage.Settings"/> 时有效。
    /// </summary>
    public SettingsViewModel? SettingsViewModel
    {
        get => _settingsViewModel;
        private set => SetField(ref _settingsViewModel, value);
    }

    /// <summary>
    /// 是否显示主聊天界面。
    /// </summary>
    public bool IsChatPage => CurrentPage == AppPage.Chat;

    /// <summary>
    /// 是否显示设置界面。
    /// </summary>
    public bool IsSettingsPage => CurrentPage == AppPage.Settings;

    /// <summary>
    /// 是否显示角色编辑界面。
    /// </summary>
    public bool IsRoleEditPage => CurrentPage == AppPage.RoleEdit;

    /// <summary>
    /// 使用指定的服务创建主 ViewModel。
    /// </summary>
    public MainViewModel(
        ChatRoomService chatRoomService,
        SettingsService settingsService,
        ModelProviderService modelProviderService,
        SessionService sessionService)
    {
        _chatRoomService = chatRoomService;
        _settingsService = settingsService;

        SessionListViewModel = new SessionListViewModel(chatRoomService, sessionService);
        ChatViewModel = new ChatViewModel(chatRoomService);
        RoleListViewModel = new RoleListViewModel(chatRoomService, modelProviderService);

        // 连接子 ViewModel 的事件
        SessionListViewModel.SettingsRequested += OnSettingsRequested;
        SessionListViewModel.SessionOpened += OnSessionOpened;
        SessionListViewModel.NewSessionCreated += OnNewSessionCreated;
        RoleListViewModel.AddRoleRequested += OnAddRoleRequested;
        RoleListViewModel.EditRoleRequested += OnEditRoleRequested;
    }

    /// <summary>
    /// 初始化主 ViewModel。创建默认会话并添加一个名为"助手"的默认角色。
    /// </summary>
    public async Task InitializeAsync()
    {
        await _chatRoomService.CreateNewSessionAsync().ConfigureAwait(false);

        var assistantDefinition = new ChatRoomRoleDefinition
        {
            RoleId = Guid.NewGuid().ToString("N"),
            RoleName = "助手",
            SystemPrompt = "你是一个乐于助人的 AI 助手，请根据用户的提问提供准确、有用的回答。",
        };
        await _chatRoomService.AddRoleAsync(assistantDefinition).ConfigureAwait(false);
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        NavigateToSettings();
    }

    private void OnSessionOpened(object? sender, string sessionId)
    {
        NavigateToChat();
    }

    private async void OnNewSessionCreated(object? sender, EventArgs e)
    {
        var assistantDefinition = new ChatRoomRoleDefinition
        {
            RoleId = Guid.NewGuid().ToString("N"),
            RoleName = "助手",
            SystemPrompt = "你是一个乐于助人的 AI 助手，请根据用户的提问提供准确、有用的回答。",
        };
        await _chatRoomService.AddRoleAsync(assistantDefinition).ConfigureAwait(false);
        NavigateToChat();
    }

    private void OnAddRoleRequested(object? sender, EventArgs e)
    {
        NavigateToCreateRole();
    }

    private void OnEditRoleRequested(object? sender, string roleId)
    {
        NavigateToEditRole(roleId);
    }

    /// <summary>
    /// 导航到设置页面。
    /// </summary>
    public void NavigateToSettings()
    {
        SettingsViewModel = new SettingsViewModel(_settingsService, _chatRoomService);
        SettingsViewModel.BackRequested += OnSettingsBack;
        CurrentPage = AppPage.Settings;
        RaisePageChanged();
    }

    /// <summary>
    /// 导航到角色编辑页面（新建角色）。
    /// </summary>
    public void NavigateToCreateRole()
    {
        RoleEditViewModel = new RoleEditViewModel(_chatRoomService, null);
        RoleEditViewModel.SaveCompleted += OnRoleEditCompleted;
        RoleEditViewModel.CancelRequested += OnRoleEditCompleted;
        CurrentPage = AppPage.RoleEdit;
        RaisePageChanged();
    }

    /// <summary>
    /// 导航到角色编辑页面（编辑已有角色）。
    /// </summary>
    /// <param name="roleId">要编辑的角色 ID。</param>
    public void NavigateToEditRole(string roleId)
    {
        RoleEditViewModel = new RoleEditViewModel(_chatRoomService, roleId);
        RoleEditViewModel.SaveCompleted += OnRoleEditCompleted;
        RoleEditViewModel.CancelRequested += OnRoleEditCompleted;
        CurrentPage = AppPage.RoleEdit;
        RaisePageChanged();
    }

    /// <summary>
    /// 导航回主聊天界面。
    /// </summary>
    public void NavigateToChat()
    {
        CurrentPage = AppPage.Chat;
        RoleEditViewModel = null;
        SettingsViewModel = null;
        RaisePageChanged();
    }

    private void OnSettingsBack(object? sender, EventArgs e)
    {
        if (SettingsViewModel is not null)
        {
            SettingsViewModel.BackRequested -= OnSettingsBack;
        }

        NavigateToChat();
    }

    private void OnRoleEditCompleted(object? sender, EventArgs e)
    {
        if (RoleEditViewModel is not null)
        {
            RoleEditViewModel.SaveCompleted -= OnRoleEditCompleted;
            RoleEditViewModel.CancelRequested -= OnRoleEditCompleted;
        }

        NavigateToChat();
    }

    private void RaisePageChanged()
    {
        OnPropertyChanged(nameof(IsChatPage));
        OnPropertyChanged(nameof(IsSettingsPage));
        OnPropertyChanged(nameof(IsRoleEditPage));
    }
}
