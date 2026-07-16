using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 角色编辑 ViewModel。
/// </summary>
public sealed class RoleEditViewModel : ViewModelBase
{
    private readonly ChatRoomService _chatRoomService;
    private readonly string? _editingRoleId;

    private string _roleName = string.Empty;
    private string _systemPrompt = string.Empty;
    private string _memoryContent = string.Empty;
    private bool _isHuman;
    private string? _modelProviderId;
    private string? _modelId;
    private string? _participationMode;
    private string? _currentEffectiveModel;

    /// <summary>
    /// 是否为编辑模式（而非新建）。
    /// </summary>
    public bool IsEditing => _editingRoleId is not null;

    /// <summary>
    /// 页面标题文本。
    /// </summary>
    public string TitleText => IsEditing ? "编辑角色" : "新建角色";

    /// <summary>
    /// 角色显示名。
    /// </summary>
    public string RoleName
    {
        get => _roleName;
        set => SetField(ref _roleName, value);
    }

    /// <summary>
    /// 系统提示词。
    /// </summary>
    public string SystemPrompt
    {
        get => _systemPrompt;
        set => SetField(ref _systemPrompt, value);
    }

    /// <summary>
    /// 角色记忆内容。
    /// </summary>
    public string MemoryContent
    {
        get => _memoryContent;
        set => SetField(ref _memoryContent, value);
    }

    /// <summary>
    /// 是否人类角色。
    /// </summary>
    public bool IsHuman
    {
        get => _isHuman;
        set => SetField(ref _isHuman, value);
    }

    /// <summary>
    /// 模型提供商 ID。
    /// </summary>
    public string? ModelProviderId
    {
        get => _modelProviderId;
        set => SetField(ref _modelProviderId, value);
    }

    /// <summary>
    /// 模型 ID。
    /// </summary>
    public string? ModelId
    {
        get => _modelId;
        set => SetField(ref _modelId, value);
    }

    /// <summary>
    /// 参与模式（"AlwaysParticipate" 或 "MentionOnly"）。
    /// </summary>
    public string? ParticipationMode
    {
        get => _participationMode;
        set => SetField(ref _participationMode, value);
    }

    /// <summary>
    /// 当前实际生效的模型显示文本，格式为 "Provider / ModelName"。
    /// 编辑模式时从角色的 <see cref="AgentLib.Core.AgentApiEndpointManager.PrimaryModel"/> 读取；
    /// 新建模式时为 <see langword="null"/>。
    /// </summary>
    public string? CurrentEffectiveModel
    {
        get => _currentEffectiveModel;
        private set => SetField(ref _currentEffectiveModel, value);
    }

    /// <summary>
    /// 可选的参与模式列表。
    /// </summary>
    public string[] ParticipationModeOptions { get; } = ["始终参与", "仅被 @ 时发言"];

    /// <summary>
    /// 保存命令。
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// 取消命令。
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// 保存完成事件。
    /// </summary>
    public event EventHandler? SaveCompleted;

    /// <summary>
    /// 取消请求事件。
    /// </summary>
    public event EventHandler? CancelRequested;

    /// <summary>
    /// 使用指定的服务创建角色编辑 ViewModel。
    /// </summary>
    /// <param name="chatRoomService">聊天室服务。</param>
    /// <param name="editingRoleId">编辑模式下的角色 ID；新建模式传 <see langword="null"/>。</param>
    public RoleEditViewModel(ChatRoomService chatRoomService, string? editingRoleId)
    {
        _chatRoomService = chatRoomService;
        _editingRoleId = editingRoleId;

        SaveCommand = new SimpleAsyncCommand(SaveAsync);
        CancelCommand = new SimpleCommand(() => CancelRequested?.Invoke(this, EventArgs.Empty));

        if (editingRoleId is not null)
        {
            LoadExistingRole(editingRoleId);
        }
    }

    private void LoadExistingRole(string roleId)
    {
        if (_chatRoomService.CurrentManager is null)
        {
            return;
        }

        ChatRoomRole? role = _chatRoomService.CurrentManager.Roles
            .FirstOrDefault(r => r.Definition.RoleId == roleId);

        if (role is null)
        {
            return;
        }

        ChatRoomRoleDefinition def = role.Definition;
        _roleName = def.RoleName;
        _systemPrompt = def.SystemPrompt;
        _memoryContent = def.MemoryContent ?? string.Empty;
        _isHuman = def.IsHuman;
        _modelProviderId = def.ModelProviderId;
        _modelId = def.ModelId;
        _participationMode = def.ParticipationMode == ChatRoomParticipationMode.MentionOnly
            ? ParticipationModeOptions[1]
            : ParticipationModeOptions[0];

        // 从角色的 EndpointManager 读取当前实际生效的模型
        _currentEffectiveModel = ResolveEffectiveModel(role);

        OnPropertyChanged(nameof(RoleName));
        OnPropertyChanged(nameof(SystemPrompt));
        OnPropertyChanged(nameof(MemoryContent));
        OnPropertyChanged(nameof(IsHuman));
        OnPropertyChanged(nameof(ModelProviderId));
        OnPropertyChanged(nameof(ModelId));
        OnPropertyChanged(nameof(ParticipationMode));
        OnPropertyChanged(nameof(CurrentEffectiveModel));
    }

        /// <summary>
        /// 从角色运行时的 <see cref="AgentLib.Core.AgentApiEndpointManager.PrimaryModel"/> 读取当前实际生效的模型。
        /// 若端点管理器尚无可用模型，返回 "加入聊天室后生效"。
        /// </summary>
        private static string? ResolveEffectiveModel(ChatRoomRole role)
        {
            try
            {
                var supportedModels = role.EndpointManager.GetSupportedModels();
                if (supportedModels.Count == 0)
                {
                    return "加入聊天室后生效";
                }

                var primary = role.EndpointManager.PrimaryModel;
                var def = primary.ModelDefinition;
                return $"{def.Provider} / {def.ModelName}";
            }
            catch
            {
                return "加入聊天室后生效";
            }
        }

        private async Task SaveAsync()
    {
        if (_chatRoomService.CurrentManager is null)
        {
            return;
        }

        ChatRoomParticipationMode mode = ParticipationMode == ParticipationModeOptions[1]
            ? ChatRoomParticipationMode.MentionOnly
            : ChatRoomParticipationMode.AlwaysParticipate;

        if (IsEditing && _editingRoleId is not null)
        {
            // 编辑模式：移除旧角色，添加更新后的角色
            await _chatRoomService.RemoveRoleAsync(_editingRoleId).ConfigureAwait(true);
        }

        var definition = new ChatRoomRoleDefinition
        {
            RoleId = IsEditing && _editingRoleId is not null
                ? _editingRoleId
                : Guid.NewGuid().ToString("N"),
            RoleName = _roleName,
            SystemPrompt = _systemPrompt,
            IsHuman = _isHuman,
            ModelProviderId = _modelProviderId,
            ModelId = _modelId,
            MemoryContent = string.IsNullOrWhiteSpace(_memoryContent) ? null : _memoryContent,
            ParticipationMode = mode,
        };

        await _chatRoomService.AddRoleAsync(definition).ConfigureAwait(true);

        // 确保持久化到磁盘并刷新会话列表
        await _chatRoomService.SaveAsync().ConfigureAwait(true);

        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }
}
