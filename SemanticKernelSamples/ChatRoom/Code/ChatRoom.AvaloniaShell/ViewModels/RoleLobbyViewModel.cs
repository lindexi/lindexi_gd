using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Model;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 角色模板卡片 ViewModel。封装 <see cref="RoleTemplate"/> 供 UI 绑定。
/// </summary>
public sealed class RoleTemplateCardViewModel : ViewModelBase
{
    /// <summary>
    /// 模板 ID。
    /// </summary>
    public string TemplateId { get; }

    /// <summary>
    /// 模板显示名。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 模板描述。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 分类。
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// 标签显示文本（如 "开发 · 架构"）。
    /// </summary>
    public string TagsDisplay { get; }

    /// <summary>
    /// 名称首字（头像显示）。
    /// </summary>
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[..1].ToUpperInvariant();

    /// <summary>
    /// 系统提示词摘要（前 50 字）。
    /// </summary>
    public string SystemPromptSummary { get; }

    /// <summary>
    /// 模型显示文本。
    /// </summary>
    public string ModelDisplay { get; }

    /// <summary>
    /// 是否预置模板。
    /// </summary>
    public bool IsPreset { get; }

    /// <summary>
    /// 从模板创建卡片 ViewModel。
    /// </summary>
    /// <param name="template">角色模板。</param>
    public RoleTemplateCardViewModel(RoleTemplate template)
    {
        TemplateId = template.TemplateId;
        Name = template.Name;
        Description = template.Description;
        Category = template.Category;
        TagsDisplay = template.Tags.Count > 0
            ? string.Join(" · ", template.Tags)
            : template.Category;
        IsPreset = template.IsPreset;

        SystemPromptSummary = string.IsNullOrWhiteSpace(template.Definition.SystemPrompt)
            ? "（未设置人设）"
            : (template.Definition.SystemPrompt.Length > 50
                ? $"{template.Definition.SystemPrompt[..50]}..."
                : template.Definition.SystemPrompt);

        ModelDisplay = string.IsNullOrWhiteSpace(template.Definition.ModelId)
            ? "默认模型"
            : template.Definition.ModelId;
    }
}

/// <summary>
/// 角色大厅 ViewModel。提供模板浏览、搜索、筛选、添加到会话、提升角色到大厅等功能。
/// </summary>
public sealed class RoleLobbyViewModel : ViewModelBase
{
    private readonly RoleTemplateService _templateService;
    private readonly ChatRoomService _chatRoomService;
    private string _searchText = string.Empty;
    private string? _selectedCategory;
    private bool _isPromotePanelVisible;
    private string _promoteName = string.Empty;
    private string _promoteDescription = string.Empty;
    private string _promoteCategory = "通用";
    private string _promoteTags = string.Empty;
    private string? _promoteSelectedRoleId;
    private string _promoteErrorMessage = string.Empty;

    private IReadOnlyList<RoleTemplate> _allTemplates = [];

    /// <summary>
    /// 筛选后的模板卡片列表。
    /// </summary>
    public ObservableCollection<RoleTemplateCardViewModel> Templates { get; } = [];

    /// <summary>
    /// 所有可用分类。
    /// </summary>
    public ObservableCollection<string> Categories { get; } = [];

    /// <summary>
    /// 当前会话中的角色列表（用于提升表单选择）。
    /// </summary>
    public ObservableCollection<RoleItemViewModel> SessionRoles { get; } = [];

    /// <summary>
    /// 搜索关键词。
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                RefreshFilteredTemplates();
            }
        }
    }

    /// <summary>
    /// 当前选中的分类筛选。为 null 时表示"全部"。
    /// </summary>
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetField(ref _selectedCategory, value))
            {
                RefreshFilteredTemplates();
            }
        }
    }

    /// <summary>
    /// 是否显示提升面板。
    /// </summary>
    public bool IsPromotePanelVisible
    {
        get => _isPromotePanelVisible;
        set => SetField(ref _isPromotePanelVisible, value);
    }

    /// <summary>
    /// 提升表单 — 模板名称。
    /// </summary>
    public string PromoteName
    {
        get => _promoteName;
        set => SetField(ref _promoteName, value);
    }

    /// <summary>
    /// 提升表单 — 描述。
    /// </summary>
    public string PromoteDescription
    {
        get => _promoteDescription;
        set => SetField(ref _promoteDescription, value);
    }

    /// <summary>
    /// 提升表单 — 分类。
    /// </summary>
    public string PromoteCategory
    {
        get => _promoteCategory;
        set => SetField(ref _promoteCategory, value);
    }

    /// <summary>
    /// 提升表单 — 标签（逗号分隔）。
    /// </summary>
    public string PromoteTags
    {
        get => _promoteTags;
        set => SetField(ref _promoteTags, value);
    }

    /// <summary>
    /// 提升表单 — 选中的来源角色 ID。
    /// </summary>
    public string? PromoteSelectedRoleId
    {
        get => _promoteSelectedRoleId;
        set => SetField(ref _promoteSelectedRoleId, value);
    }

    /// <summary>
    /// 提升表单错误提示。
    /// </summary>
    public string PromoteErrorMessage
    {
        get => _promoteErrorMessage;
        set
        {
            if (SetField(ref _promoteErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasPromoteErrorMessage));
            }
        }
    }

    /// <summary>
    /// 是否存在提升表单错误提示。
    /// </summary>
    public bool HasPromoteErrorMessage => !string.IsNullOrWhiteSpace(PromoteErrorMessage);

    /// <summary>
    /// 是否有活跃会话（控制"添加到当前会话"按钮可用性）。
    /// </summary>
    public bool HasActiveSession => _chatRoomService.HasActiveSession;

    /// <summary>
    /// 添加到当前会话命令。
    /// </summary>
    public ICommand AddToSessionCommand { get; }

    /// <summary>
    /// 编辑模板命令。
    /// </summary>
    public ICommand EditTemplateCommand { get; }

    /// <summary>
    /// 删除模板命令。
    /// </summary>
    public ICommand DeleteTemplateCommand { get; }

    /// <summary>
    /// 提升当前角色到大厅命令（打开提升面板）。
    /// </summary>
    public ICommand PromoteToLobbyCommand { get; }

    /// <summary>
    /// 确认提升命令。
    /// </summary>
    public ICommand ConfirmPromoteCommand { get; }

    /// <summary>
    /// 取消提升命令。
    /// </summary>
    public ICommand CancelPromoteCommand { get; }

    /// <summary>
    /// 返回命令。
    /// </summary>
    public ICommand BackCommand { get; }

    /// <summary>
    /// 返回请求事件。
    /// </summary>
    public event EventHandler? BackRequested;

    /// <summary>
    /// 编辑模板请求事件。参数为模板 ID。
    /// </summary>
    public event EventHandler<string>? EditTemplateRequested;

    /// <summary>
    /// 使用指定的服务创建角色大厅 ViewModel。
    /// </summary>
    /// <param name="templateService">角色模板服务。</param>
    /// <param name="chatRoomService">聊天室服务。</param>
    public RoleLobbyViewModel(RoleTemplateService templateService, ChatRoomService chatRoomService)
    {
        _templateService = templateService;
        _chatRoomService = chatRoomService;

        AddToSessionCommand = new SimpleAsyncCommand<RoleTemplateCardViewModel>(AddToSessionAsync, CanAddToSession);
        EditTemplateCommand = new SimpleCommand<RoleTemplateCardViewModel>(OnEditTemplate);
        DeleteTemplateCommand = new SimpleAsyncCommand<RoleTemplateCardViewModel>(DeleteTemplateAsync);
        PromoteToLobbyCommand = new SimpleCommand(OpenPromotePanel);
        ConfirmPromoteCommand = new SimpleAsyncCommand(ConfirmPromoteAsync);
        CancelPromoteCommand = new SimpleCommand(ClosePromotePanel);
        BackCommand = new SimpleCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));

        _chatRoomService.SessionChanged += OnSessionChanged;
    }

    /// <summary>
    /// 刷新模板列表和分类。从磁盘重新加载所有模板。
    /// </summary>
    public void RefreshTemplates()
    {
        _allTemplates = _templateService.LoadAll();

        // 刷新分类列表
        var categories = _allTemplates
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        Categories.Clear();
        foreach (string category in categories)
        {
            Categories.Add(category);
        }

        RefreshFilteredTemplates();
    }

    /// <summary>
    /// 刷新当前会话角色列表（用于提升表单的选择）。
    /// </summary>
    public void RefreshSessionRoles()
    {
        SessionRoles.Clear();

        if (_chatRoomService.CurrentManager is null)
        {
            return;
        }

        foreach (ChatRoomRole role in _chatRoomService.CurrentManager.Roles)
        {
            SessionRoles.Add(new RoleItemViewModel(role));
        }
    }

    /// <summary>
    /// 打开提升面板，并预选指定角色。
    /// </summary>
    /// <param name="roleId">要提升的角色 ID。为 null 时由用户选择。</param>
    public void OpenPromotePanelForRole(string? roleId)
    {
        RefreshSessionRoles();
        PromoteSelectedRoleId = roleId;
        PromoteErrorMessage = string.Empty;

        // 预填充表单
        if (roleId is not null)
        {
            RoleItemViewModel? role = SessionRoles.FirstOrDefault(r => r.RoleId == roleId);
            if (role is not null)
            {
                PromoteName = role.RoleName;
                PromoteDescription = role.SystemPromptSummary;
            }
        }
        else
        {
            PromoteName = string.Empty;
            PromoteDescription = string.Empty;
        }

        PromoteCategory = "通用";
        PromoteTags = string.Empty;
        IsPromotePanelVisible = true;
    }

    private void OnSessionChanged(object? sender, ChatRoomManager? manager)
    {
        OnPropertyChanged(nameof(HasActiveSession));

        if (AddToSessionCommand is SimpleAsyncCommand<RoleTemplateCardViewModel> cmd)
        {
            cmd.RaiseCanExecuteChanged();
        }
    }

    private void RefreshFilteredTemplates()
    {
        IEnumerable<RoleTemplate> filtered = _allTemplates;

        // 分类筛选
        if (!string.IsNullOrEmpty(_selectedCategory))
        {
            filtered = filtered.Where(t => t.Category == _selectedCategory);
        }

        // 搜索筛选
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            string search = _searchText.Trim();
            filtered = filtered.Where(t =>
                t.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        Templates.Clear();
        foreach (RoleTemplate template in filtered)
        {
            Templates.Add(new RoleTemplateCardViewModel(template));
        }
    }

    private bool CanAddToSession(RoleTemplateCardViewModel? card)
    {
        return card is not null && _chatRoomService.HasActiveSession;
    }

    private async Task AddToSessionAsync(RoleTemplateCardViewModel? card)
    {
        if (card is null || !_chatRoomService.HasActiveSession)
        {
            return;
        }

        IsBusy = true;
        try
        {
            RoleTemplate? template = _allTemplates.FirstOrDefault(t => t.TemplateId == card.TemplateId);
            if (template is null)
            {
                return;
            }

            ChatRoomRoleDefinition definition = _templateService.ToDefinition(template);
            await _chatRoomService.AddRoleAsync(definition).ConfigureAwait(false);
            await _chatRoomService.SaveAsync().ConfigureAwait(false);

            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnEditTemplate(RoleTemplateCardViewModel? card)
    {
        if (card is not null)
        {
            EditTemplateRequested?.Invoke(this, card.TemplateId);
        }
    }

    private async Task DeleteTemplateAsync(RoleTemplateCardViewModel? card)
    {
        if (card is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _templateService.Delete(card.TemplateId);
            await Task.CompletedTask;
            RefreshTemplates();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenPromotePanel()
    {
        OpenPromotePanelForRole(null);
    }

    private void ClosePromotePanel()
    {
        PromoteErrorMessage = string.Empty;
        IsPromotePanelVisible = false;
    }

    private async Task ConfirmPromoteAsync()
    {
        if (string.IsNullOrWhiteSpace(PromoteSelectedRoleId))
        {
            PromoteErrorMessage = "请选择要提升到大厅的来源角色。";
            return;
        }

        if (_chatRoomService.CurrentManager is null)
        {
            PromoteErrorMessage = "当前没有可用会话，无法提升角色到大厅。";
            return;
        }

        ChatRoomRole? role = _chatRoomService.CurrentManager.Roles
            .FirstOrDefault(r => r.Definition.RoleId == PromoteSelectedRoleId);
        if (role is null)
        {
            PromoteErrorMessage = "未找到要提升的来源角色，请重新选择。";
            return;
        }

        IsBusy = true;
        try
        {
            PromoteErrorMessage = string.Empty;
            var tags = PromoteTags
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            string templateName = string.IsNullOrWhiteSpace(PromoteName) ? role.Definition.RoleName : PromoteName;
            string templateDescription = string.IsNullOrWhiteSpace(PromoteDescription) ? "（无描述）" : PromoteDescription;
            RoleTemplate? template = _allTemplates.FirstOrDefault(t =>
                !t.IsPreset && t.Definition.RoleId == role.Definition.RoleId);
            if (template is null)
            {
                template = _templateService.FromDefinition(
                    role.Definition,
                    templateName,
                    templateDescription,
                    PromoteCategory,
                    tags);
            }
            else
            {
                _templateService.UpdateFromDefinition(
                    template,
                    role.Definition,
                    templateName,
                    templateDescription,
                    PromoteCategory,
                    tags);
            }

            await _templateService.SaveAsync(template).ConfigureAwait(false);
            RefreshTemplates();
            ClosePromotePanel();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
