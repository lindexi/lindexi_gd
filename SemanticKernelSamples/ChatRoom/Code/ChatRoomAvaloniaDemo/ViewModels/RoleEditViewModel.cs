using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;

using System;
using System.Collections.ObjectModel;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 角色编辑 ViewModel。提供对 <see cref="ChatRoomRoleDefinition"/> 各字段的可编辑绑定和保存/取消操作。
/// </summary>
public sealed class RoleEditViewModel
{
    private readonly ChatRoomRole _role;
    private readonly string _originalRoleName;
    private readonly string _originalSystemPrompt;
    private readonly string? _originalModelProviderId;
    private readonly string? _originalModelId;
    private readonly string? _originalMemoryContent;
    private readonly bool _originalIsHuman;

    /// <summary>
    /// 创建角色编辑 ViewModel。
    /// </summary>
    /// <param name="role">要编辑的角色。</param>
    public RoleEditViewModel(ChatRoomRole role)
    {
        _role = role ?? throw new ArgumentNullException(nameof(role));
        var def = role.Definition;

        _originalRoleName = def.RoleName;
        _originalSystemPrompt = def.SystemPrompt;
        _originalModelProviderId = def.ModelProviderId;
        _originalModelId = def.ModelId;
        _originalMemoryContent = def.MemoryContent;
        _originalIsHuman = def.IsHuman;

        RoleName = def.RoleName;
        SystemPrompt = def.SystemPrompt;
        ModelProviderId = def.ModelProviderId;
        ModelId = def.ModelId;
        MemoryContent = def.MemoryContent;
        IsHuman = def.IsHuman;

        SaveCommand = new DelegateCommand(Save);
        CancelCommand = new DelegateCommand(Cancel);
    }

    /// <summary>角色 ID（只读）。</summary>
    public string RoleId => _role.Definition.RoleId;

    /// <summary>可编辑的角色显示名。</summary>
    public string RoleName { get; set; }

    /// <summary>可编辑的系统提示词。</summary>
    public string SystemPrompt { get; set; }

    /// <summary>可编辑的模型提供商 ID。</summary>
    public string? ModelProviderId { get; set; }

    /// <summary>可编辑的模型 ID。</summary>
    public string? ModelId { get; set; }

    /// <summary>可编辑的角色记忆内容。</summary>
    public string? MemoryContent { get; set; }

    /// <summary>是否人类角色。</summary>
    public bool IsHuman { get; set; }

    /// <summary>
    /// 是否为新创建的角色（而非编辑已有角色）。取消编辑时需要从角色列表中移除。
    /// </summary>
    public bool IsNewRole { get; set; }

    /// <summary>保存命令。</summary>
    public DelegateCommand SaveCommand { get; }

    /// <summary>取消命令。</summary>
    public DelegateCommand CancelCommand { get; }

    /// <summary>
    /// 保存完成事件。外部订阅者（如 <see cref="MainViewModel"/>）可据此导航回主界面。
    /// </summary>
    public event EventHandler? Saved;

    /// <summary>
    /// 取消编辑事件。外部订阅者可据此导航回主界面。
    /// </summary>
    public event EventHandler? Cancelled;

    private void Save()
    {
        var def = _role.Definition;
        def.RoleName = RoleName;
        def.SystemPrompt = SystemPrompt;
        def.ModelProviderId = ModelProviderId;
        def.ModelId = ModelId;
        def.MemoryContent = MemoryContent;
        def.IsHuman = IsHuman;

        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        var def = _role.Definition;
        def.RoleName = _originalRoleName;
        def.SystemPrompt = _originalSystemPrompt;
        def.ModelProviderId = _originalModelProviderId;
        def.ModelId = _originalModelId;
        def.MemoryContent = _originalMemoryContent;
        def.IsHuman = _originalIsHuman;

        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}