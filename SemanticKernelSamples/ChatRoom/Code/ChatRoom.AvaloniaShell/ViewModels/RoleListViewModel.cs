using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Model;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 角色列表项 ViewModel。
/// </summary>
public sealed class RoleItemViewModel : ViewModelBase
{
    private bool _isSelected;

    /// <summary>
    /// 角色 ID。
    /// </summary>
    public string RoleId { get; }

    /// <summary>
    /// 角色显示名。
    /// </summary>
    public string RoleName { get; }

    /// <summary>
    /// 是否人类角色。
    /// </summary>
    public bool IsHuman { get; }

    /// <summary>
    /// 系统提示词摘要。
    /// </summary>
    public string SystemPromptSummary { get; }

    /// <summary>
    /// 模型显示文本。
    /// </summary>
    public string ModelDisplay { get; }

    /// <summary>
    /// 角色名首字（头像显示）。
    /// </summary>
    public string Initial => string.IsNullOrEmpty(RoleName) ? "?" : RoleName[..1].ToUpperInvariant();

    /// <summary>
    /// 参与模式显示文本。
    /// </summary>
    public string ParticipationModeDisplay => IsHuman ? "人类" : "AI 角色";

    /// <summary>
    /// 是否选中。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>
    /// 从角色定义创建角色列表项。
    /// </summary>
    public RoleItemViewModel(ChatRoomRoleDefinition definition)
    {
        RoleId = definition.RoleId;
        RoleName = definition.RoleName;
        IsHuman = definition.IsHuman;
        SystemPromptSummary = string.IsNullOrWhiteSpace(definition.SystemPrompt)
            ? "（未设置人设）"
            : (definition.SystemPrompt.Length > 50
                ? $"{definition.SystemPrompt[..50]}..."
                : definition.SystemPrompt);
        ModelDisplay = string.IsNullOrWhiteSpace(definition.ModelId)
            ? "默认模型"
            : definition.ModelId;
    }
}

/// <summary>
/// 右栏角色列表 ViewModel。
/// </summary>
public sealed class RoleListViewModel : ViewModelBase
{
    private readonly ChatRoomService _chatRoomService;
    private readonly ModelProviderService _modelProviderService;

    /// <summary>
    /// 角色列表。
    /// </summary>
    public ObservableCollection<RoleItemViewModel> Roles { get; } = [];

    /// <summary>
    /// 添加角色命令。
    /// </summary>
    public ICommand AddRoleCommand { get; }

    /// <summary>
    /// 编辑角色命令。
    /// </summary>
    public ICommand EditRoleCommand { get; }

    /// <summary>
    /// 删除角色命令。
    /// </summary>
    public ICommand DeleteRoleCommand { get; }

    /// <summary>
    /// 添加角色请求事件。
    /// </summary>
    public event EventHandler? AddRoleRequested;

    /// <summary>
    /// 编辑角色请求事件。参数为角色 ID。
    /// </summary>
    public event EventHandler<string>? EditRoleRequested;

    /// <summary>
    /// 使用指定的服务创建角色列表 ViewModel。
    /// </summary>
    public RoleListViewModel(ChatRoomService chatRoomService, ModelProviderService modelProviderService)
    {
        _chatRoomService = chatRoomService;
        _modelProviderService = modelProviderService;

        AddRoleCommand = new SimpleCommand(() => AddRoleRequested?.Invoke(this, EventArgs.Empty));
        EditRoleCommand = new SimpleCommand<RoleItemViewModel>(role => EditRoleRequested?.Invoke(this, role.RoleId));
        DeleteRoleCommand = new SimpleCommand<RoleItemViewModel>(DeleteRole);

        _chatRoomService.SessionChanged += OnSessionChanged;
    }

    private void OnSessionChanged(object? sender, ChatRoomManager? manager)
    {
        Roles.Clear();

        if (manager is null)
        {
            return;
        }

        foreach (ChatRoomRole role in manager.Roles)
        {
            Roles.Add(new RoleItemViewModel(role.Definition));
        }

        manager.Roles.CollectionChanged += OnRolesCollectionChanged;
    }

    private void OnRolesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Roles.Clear();

        if (_chatRoomService.CurrentManager is null)
        {
            return;
        }

        foreach (ChatRoomRole role in _chatRoomService.CurrentManager.Roles)
        {
            Roles.Add(new RoleItemViewModel(role.Definition));
        }
    }

    private void DeleteRole(RoleItemViewModel? role)
    {
        if (role is null)
        {
            return;
        }

        _chatRoomService.RemoveRole(role.RoleId);
    }
}

/// <summary>
/// 简单的带参数同步命令。
/// </summary>
public sealed class SimpleCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public SimpleCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => parameter is T or null && (_canExecute?.Invoke((T?)parameter) ?? true);

    public void Execute(object? parameter) => _execute((T?)parameter);

    event EventHandler? ICommand.CanExecuteChanged
    {
        add { }
        remove { }
    }
}
