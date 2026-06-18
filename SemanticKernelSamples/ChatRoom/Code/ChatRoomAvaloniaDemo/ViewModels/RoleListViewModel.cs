using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using ChatRoomAvaloniaDemo.Services;

using System;
using System.Collections.ObjectModel;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 角色列表 ViewModel。管理当前聊天室中角色的添加、删除和编辑操作。
/// </summary>
public sealed class RoleListViewModel : NotifyBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly ChatRoomService _chatRoomService;
    private ChatRoomRole? _selectedRole;

    /// <summary>
    /// 创建角色列表 ViewModel。
    /// </summary>
    /// <param name="mainViewModel">主 ViewModel，用于导航到角色编辑。</param>
    /// <param name="chatRoomService">聊天室服务。</param>
    public RoleListViewModel(MainViewModel mainViewModel, ChatRoomService chatRoomService)
    {
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _chatRoomService = chatRoomService ?? throw new ArgumentNullException(nameof(chatRoomService));

        AddRoleCommand = new DelegateCommand(AddRole);
        RemoveRoleCommand = new DelegateCommand<ChatRoomRole?>(RemoveRole);
        EditRoleCommand = new DelegateCommand<ChatRoomRole?>(EditRole);
    }

    /// <summary>
    /// 角色列表。直接绑定到 <see cref="ChatRoomManager.Roles"/>。
    /// </summary>
    public ObservableCollection<ChatRoomRole> Roles =>
        _chatRoomService.ChatRoomManager?.Roles ?? new ObservableCollection<ChatRoomRole>();

    /// <summary>
    /// 当前选中的角色。
    /// </summary>
    public ChatRoomRole? SelectedRole
    {
        get => _selectedRole;
        set => SetField(ref _selectedRole, value);
    }

    /// <summary>添加角色命令。</summary>
    public DelegateCommand AddRoleCommand { get; }

    /// <summary>删除角色命令。</summary>
    public DelegateCommand<ChatRoomRole?> RemoveRoleCommand { get; }

    /// <summary>编辑角色命令。</summary>
    public DelegateCommand<ChatRoomRole?> EditRoleCommand { get; }

    /// <summary>
    /// 绑定到活跃的 <see cref="ChatRoomManager"/>。
    /// </summary>
    public void BindToManager()
    {
        OnPropertyChanged(nameof(Roles));
    }

    private void AddRole()
    {
        var definition = new ChatRoomRoleDefinition
        {
            RoleId = Guid.NewGuid().ToString("N")[..8],
            RoleName = "新角色",
            SystemPrompt = string.Empty,
            ModelProviderId = _chatRoomService.AppConfig?.DefaultModelProviderName ?? string.Empty,
            ModelId = _chatRoomService.AppConfig?.PrimaryModelId ?? string.Empty,
        };

        var role = new ChatRoomRole(definition, _chatRoomService.EndpointManager);
        _chatRoomService.RegisterProviders(role.EndpointManager);

        // 诊断日志：确认 EndpointManager 的注册状态
        var sharedEm = _chatRoomService.EndpointManager;
        var roleEm = role.EndpointManager;
        System.Diagnostics.Debug.WriteLine(
            $"[AddRole] EndpointManager 同一实例: {ReferenceEquals(sharedEm, roleEm)}, " +
            $"共享 SupportedModels 数量: {sharedEm.GetSupportedModels().Count}, " +
            $"角色 SupportedModels 数量: {roleEm.GetSupportedModels().Count}");

        _chatRoomService.ChatRoomManager?.Roles.Add(role);

        OnPropertyChanged(nameof(Roles));

        // 自动打开编辑界面
        var roleEditVm = new RoleEditViewModel(role)
        {
            IsNewRole = true
        };
        roleEditVm.Saved += (_, _) =>
        {
            OnPropertyChanged(nameof(Roles));
            _mainViewModel.NavigateBack();
        };
        roleEditVm.Cancelled += (_, _) =>
        {
            _chatRoomService.ChatRoomManager?.Roles.Remove(role);
            OnPropertyChanged(nameof(Roles));
            _mainViewModel.NavigateBack();
        };
        _mainViewModel.NavigateToRoleEditCommand.Execute(roleEditVm);
    }

    private void RemoveRole(ChatRoomRole? role)
    {
        if (role is null)
        {
            return;
        }

        _chatRoomService.ChatRoomManager?.Roles.Remove(role);
        OnPropertyChanged(nameof(Roles));
    }

    private void EditRole(ChatRoomRole? role)
    {
        if (role is null)
        {
            return;
        }

        var roleEditVm = new RoleEditViewModel(role);
        roleEditVm.Saved += (_, _) =>
        {
            OnPropertyChanged(nameof(Roles));
            _mainViewModel.NavigateBack();
        };
        roleEditVm.Cancelled += (_, _) => _mainViewModel.NavigateBack();
        _mainViewModel.NavigateToRoleEditCommand.Execute(roleEditVm);
    }
}