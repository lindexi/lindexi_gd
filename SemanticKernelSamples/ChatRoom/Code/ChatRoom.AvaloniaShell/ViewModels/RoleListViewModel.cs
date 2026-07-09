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
    /// 角色运行时对象。
    /// </summary>
    public ChatRoomRole Role { get; }

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
    public RoleItemViewModel(ChatRoomRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        Role = role;
        ChatRoomRoleDefinition definition = role.Definition;
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
    /// 打开角色大厅命令。
    /// </summary>
    public ICommand OpenLobbyCommand { get; }

    /// <summary>
    /// 提升角色到大厅命令。参数为角色项 ViewModel。
    /// </summary>
    public ICommand PromoteToLobbyCommand { get; }

    /// <summary>
    /// 在聊天输入框插入角色提及命令。参数为角色项 ViewModel。
    /// </summary>
    public ICommand InsertMentionCommand { get; }

    /// <summary>
    /// 压缩角色内部对话命令。参数为角色项 ViewModel。
    /// </summary>
    public ICommand ReduceRoleSessionCommand { get; }

    /// <summary>
    /// 清空角色内部记忆命令。参数为角色项 ViewModel。
    /// </summary>
    public ICommand ClearRoleSessionMemoryCommand { get; }

    /// <summary>
    /// 添加角色请求事件。
    /// </summary>
    public event EventHandler? AddRoleRequested;

    /// <summary>
    /// 编辑角色请求事件。参数为角色 ID。
    /// </summary>
    public event EventHandler<string>? EditRoleRequested;

    /// <summary>
    /// 打开角色大厅请求事件。
    /// </summary>
    public event EventHandler? OpenLobbyRequested;

    /// <summary>
    /// 提升角色到大厅请求事件。参数为角色 ID。
    /// </summary>
    public event EventHandler<string>? PromoteToLobbyRequested;

    /// <summary>
    /// 插入角色提及请求事件。参数为角色显示名。
    /// </summary>
    public event EventHandler<string>? InsertMentionRequested;

    /// <summary>
    /// 使用指定的服务创建角色列表 ViewModel。
    /// </summary>
    public RoleListViewModel(ChatRoomService chatRoomService, ModelProviderService modelProviderService)
    {
        _chatRoomService = chatRoomService;
        _modelProviderService = modelProviderService;

        AddRoleCommand = new SimpleCommand(() => AddRoleRequested?.Invoke(this, EventArgs.Empty));
        EditRoleCommand = new SimpleCommand<RoleItemViewModel>(role =>
        {
            if (role is not null)
            {
                EditRoleRequested?.Invoke(this, role.RoleId);
            }
        });
        DeleteRoleCommand = new SimpleCommand<RoleItemViewModel>(DeleteRole);
        OpenLobbyCommand = new SimpleCommand(() => OpenLobbyRequested?.Invoke(this, EventArgs.Empty));
        PromoteToLobbyCommand = new SimpleCommand<RoleItemViewModel>(role =>
        {
            if (role is not null)
            {
                PromoteToLobbyRequested?.Invoke(this, role.RoleId);
            }
        });
        InsertMentionCommand = new SimpleCommand<RoleItemViewModel>(role =>
        {
            if (role is not null)
            {
                InsertMentionRequested?.Invoke(this, role.RoleName);
            }
        });
        ReduceRoleSessionCommand = new SimpleAsyncCommand<RoleItemViewModel>(ReduceRoleSessionAsync, role => role is { IsHuman: false });
        ClearRoleSessionMemoryCommand = new SimpleAsyncCommand<RoleItemViewModel>(ClearRoleSessionMemoryAsync);

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
            Roles.Add(new RoleItemViewModel(role));
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
            Roles.Add(new RoleItemViewModel(role));
        }
    }

    private void DeleteRole(RoleItemViewModel? role)
    {
        if (role is null)
        {
            return;
        }

        _chatRoomService.RemoveRole(role.RoleId);

        // 确保持久化到磁盘
        _ = _chatRoomService.SaveAsync();
    }

    private async System.Threading.Tasks.Task ReduceRoleSessionAsync(RoleItemViewModel? role)
    {
        if (role is null)
        {
            return;
        }

        await _chatRoomService.ReduceRoleSessionAsync(role.RoleId).ConfigureAwait(false);
    }

    private async System.Threading.Tasks.Task ClearRoleSessionMemoryAsync(RoleItemViewModel? role)
    {
        if (role is null)
        {
            return;
        }

        await _chatRoomService.ClearRoleSessionMemoryAsync(role.RoleId).ConfigureAwait(false);
    }
}

/// <summary>
/// 简单的带参数同步命令。
/// </summary>
public sealed class SimpleCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private EventHandler? _canExecuteChanged;

    public SimpleCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => parameter is T or null && (_canExecute?.Invoke((T?)parameter) ?? true);

    public void Execute(object? parameter) => _execute((T?)parameter);

    /// <summary>
    /// 通知 UI 重新查询 <see cref="CanExecute"/> 状态。
    /// </summary>
    public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);

    event EventHandler? ICommand.CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }
}
