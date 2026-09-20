using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using AgentLib;
using AgentLib.Model;
using CodingChatRoom.AvaloniaShell.Abilities;
using CodingChatRoom.AvaloniaShell.Services;
using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 表示右侧编程助手聊天区域。
/// </summary>
public sealed class ChatViewModel : ViewModelBase, IDisposable
{
    private readonly CopilotChatManager? _chatManager;
    private readonly CodingChatApplication? _application;
    private readonly CodingWorkspaceController? _workspaceController;
    private readonly AbilityCatalog? _abilityCatalog;
    private string _modelStatusText;
    private CopilotChatSession? _subscribedSession;
    private LanguageModelOptionViewModel? _selectedModel;
    private ReasoningEffortOptionViewModel? _selectedReasoningEffort;
    private AbilityOptionViewModel? _selectedAbility;
    private string _inputText = string.Empty;
    private string? _runStatusText;
    private bool _isLoopIterationEnabled;
    private bool _isAutomaticCompressionEnabled = true;
    private bool _isDotNetRunEnabled;
    private bool _isDisposed;

    /// <summary>
    /// 初始化尚未接入模型发送的聊天视图骨架。
    /// </summary>
    public ChatViewModel()
    {
        _modelStatusText = "等待应用初始化";
        SendCommand = new SimpleCommand(static () => { }, static () => false);
        CompressConversationCommand = new SimpleCommand(static () => { }, static () => false);
        StopCommand = new SimpleCommand(static () => { }, static () => false);
        StopLanguageServerCommand = new SimpleCommand(static () => { }, static () => false);
        ApplyWorkspaceCommand = new SimpleCommand(static () => { }, static () => false);
        PendingImages.CollectionChanged += OnPendingImagesCollectionChanged;
        InitializeReasoningEfforts();
        RebuildAbilities();
    }

    internal ChatViewModel(
        CopilotChatManager chatManager,
        CodingChatApplication application,
        string statusText)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(application);
        _chatManager = chatManager;
        _application = application;
        _modelStatusText = statusText;
        SendCommand = new SimpleAsyncCommand(SendAsync, () => CanSend, allowConcurrentExecutions: true);
        CompressConversationCommand = new SimpleAsyncCommand(CompressConversationAsync, () => CanCompressConversation);
        StopCommand = new SimpleAsyncCommand(application.StopActiveRunByUserAsync, () => IsRunning);
        StopLanguageServerCommand = new SimpleAsyncCommand(StopLanguageServerAsync, () => CanStopLanguageServer);
        ApplyWorkspaceCommand = new SimpleCommand(static () => { }, static () => false);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
        _application.StateChanged += OnApplicationStateChanged;
        PendingImages.CollectionChanged += OnPendingImagesCollectionChanged;
        InitializeAvailableModels();
        InitializeReasoningEfforts();
        RebuildAbilities();
        AttachSession(_chatManager.SelectedSession);
    }

    internal ChatViewModel(
        CopilotChatManager chatManager,
        CodingChatApplication application,
        CodingWorkspaceController workspaceController,
        string statusText,
        AbilityCatalog? abilityCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(workspaceController);
        _chatManager = chatManager;
        _application = application;
        _workspaceController = workspaceController;
        _abilityCatalog = abilityCatalog;
        _modelStatusText = statusText;
        SendCommand = new SimpleAsyncCommand(SendAsync, () => CanSend, allowConcurrentExecutions: true);
        CompressConversationCommand = new SimpleAsyncCommand(CompressConversationAsync, () => CanCompressConversation);
        StopCommand = new SimpleAsyncCommand(application.StopActiveRunByUserAsync, () => IsRunning);
        StopLanguageServerCommand = new SimpleAsyncCommand(StopLanguageServerAsync, () => CanStopLanguageServer);
        ApplyWorkspaceCommand = new SimpleAsyncCommand(ApplyWorkspaceAsync, () => CanApplyWorkspace);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
        _application.StateChanged += OnApplicationStateChanged;
        _workspaceController.PropertyChanged += OnWorkspaceControllerPropertyChanged;
        if (_abilityCatalog is not null)
        {
            _abilityCatalog.Changed += OnAbilityCatalogChanged;
        }
        PendingImages.CollectionChanged += OnPendingImagesCollectionChanged;
        InitializeAvailableModels();
        InitializeReasoningEfforts();
        RebuildAbilities();
        AttachSession(_chatManager.SelectedSession);
    }

    /// <summary>
    /// 获取是否仅有预设欢迎信息，尚无实际对话。
    /// </summary>
    public bool ShowWelcome => Messages.All(item => item.Message.IsPresetInfo);

    /// <summary>获取当前会话标题。</summary>
    public string CurrentSessionTitle => _subscribedSession?.Title ?? "编程助手";

    /// <summary>
    /// 获取当前会话 ID。
    /// </summary>
    public Guid CurrentSessionId => _subscribedSession?.SessionId ?? Guid.Empty;

    /// <summary>
    /// 获取当前状态说明。
    /// </summary>
    public string StatusText => _runStatusText ?? _modelStatusText;

    /// <summary>
    /// 获取当前进程可用的语言模型。
    /// </summary>
    public ObservableCollection<LanguageModelOptionViewModel> AvailableModels { get; } = [];

    /// <summary>
    /// 获取可选择的思考强度。
    /// </summary>
    public ObservableCollection<ReasoningEffortOptionViewModel> AvailableReasoningEfforts { get; } = [];

    /// <summary>
    /// 获取或设置当前思考强度。
    /// </summary>
    public ReasoningEffortOptionViewModel? SelectedReasoningEffort
    {
        get => _selectedReasoningEffort;
        set
        {
            if (value is not null && AvailableReasoningEfforts.Contains(value))
            {
                SetField(ref _selectedReasoningEffort, value);
            }
        }
    }

    /// <summary>
    /// 获取或设置当前对话使用的语言模型。
    /// </summary>
    public LanguageModelOptionViewModel? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (value is null || !AvailableModels.Contains(value) || !SetField(ref _selectedModel, value))
            {
                return;
            }

            if (_chatManager is not null)
            {
                _chatManager.AgentApiEndpointManager.PrimaryModel = value.Model;
                _modelStatusText = $"当前模型：{value.DisplayName}";
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    /// <summary>
    /// 获取发送按钮文本。
    /// </summary>
    public string SendButtonText => IsRunning ? "插话" : "发送";

    /// <summary>获取可选择的发送前能力。</summary>
    public ObservableCollection<AbilityOptionViewModel> AvailableAbilities { get; } = [];

    /// <summary>获取或设置当前发送前能力。</summary>
    public AbilityOptionViewModel? SelectedAbility
    {
        get => _selectedAbility;
        set
        {
            if (value is not null && AvailableAbilities.Contains(value) && SetField(ref _selectedAbility, value))
            {
                OnPropertyChanged(nameof(SendButtonText));
                OnPropertyChanged(nameof(CanSend));
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 获取消息投影集合。
    /// </summary>
    public ObservableCollection<MessageItemViewModel> Messages { get; } = [];

    /// <summary>
    /// 获取待随下一条消息发送的图片附件。
    /// </summary>
    public ObservableCollection<ImageAttachmentViewModel> PendingImages { get; } = [];

    /// <summary>
    /// 获取或设置输入文本。
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetField(ref _inputText, value))
            {
                OnPropertyChanged(nameof(CanSend));
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 获取或设置下一次发送是否启用循环迭代。
    /// </summary>
    public bool IsLoopIterationEnabled
    {
        get => _isLoopIterationEnabled;
        set
        {
            if (SetField(ref _isLoopIterationEnabled, value))
            {
                if (_application is not null)
                {
                    _application.IsLoopIterationEnabled = value;
                }

                OnPropertyChanged(nameof(CanSend));
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 获取或设置发送消息时是否自动压缩对话历史。
    /// </summary>
    public bool IsAutomaticCompressionEnabled
    {
        get => _isAutomaticCompressionEnabled;
        set => SetField(ref _isAutomaticCompressionEnabled, value);
    }

    /// <summary>
    /// 获取或设置下一次新运行是否提供 <c>dotnet run</c> 工具。
    /// </summary>
    public bool IsDotNetRunEnabled
    {
        get => _isDotNetRunEnabled;
        set => SetField(ref _isDotNetRunEnabled, value);
    }

    /// <summary>
    /// 获取发送命令。
    /// </summary>
    public ICommand SendCommand { get; }

    /// <summary>
    /// 获取压缩当前对话命令。
    /// </summary>
    public ICommand CompressConversationCommand { get; }

    /// <summary>
    /// 获取停止命令。
    /// </summary>
    public ICommand StopCommand { get; }

    /// <summary>
    /// 获取停止 Roslyn Language Server 的命令。
    /// </summary>
    public ICommand StopLanguageServerCommand { get; }

    /// <summary>
    /// 获取应用工作路径命令。
    /// </summary>
    public ICommand ApplyWorkspaceCommand { get; }

    /// <summary>
    /// 获取是否可发送消息。
    /// </summary>
    public bool CanSend
    {
        get
        {
            if (SelectedAbility?.IsCompression == true)
            {
                return _application?.CanCompressConversation == true && PendingImages.Count == 0;
            }

            return _application?.CanSend == true
                   && (IsLoopIterationEnabled
                       ? !string.IsNullOrWhiteSpace(InputText)
                       : !string.IsNullOrWhiteSpace(InputText) || PendingImages.Count > 0);
        }
    }

    /// <summary>
    /// 获取当前对话是否可以压缩。
    /// </summary>
    public bool CanCompressConversation => _application?.CanCompressConversation == true;

    /// <summary>
    /// 获取当前是否可以停止 Roslyn Language Server。
    /// </summary>
    public bool CanStopLanguageServer => _application is not null && !IsRunning && !IsCompressing;

    /// <summary>
    /// 获取是否存在待发送图片。
    /// </summary>
    public bool HasPendingImages => PendingImages.Count > 0;

    /// <summary>
    /// 获取是否正在运行。
    /// </summary>
    public bool IsRunning => _application?.IsRunActive == true || _application?.IsLoopActive == true;

    /// <summary>
    /// 获取当前是否正在压缩对话。
    /// </summary>
    public bool IsCompressing => _application?.IsCompressionActive == true;

    /// <summary>获取当前是否正在完成最终保存。</summary>
    public bool IsFinalizing => _application?.IsFinalizing == true;

    /// <summary>
    /// 获取或设置待应用的工作路径。
    /// </summary>
    public string WorkspaceInput
    {
        get => _workspaceController?.WorkspaceInput ?? string.Empty;
        set
        {
            if (_workspaceController is not null)
            {
                _workspaceController.WorkspaceInput = value;
            }
        }
    }

    /// <summary>
    /// 获取当前已提交的工作路径。
    /// </summary>
    public string? NextRunWorkspacePath => _workspaceController?.NextRunWorkspacePath;

    /// <summary>
    /// 获取工作路径状态文本。
    /// </summary>
    public string WorkspaceStatusText => _workspaceController?.StatusText ?? "工作路径功能尚未初始化";

    /// <summary>
    /// 获取是否正在切换工作路径。
    /// </summary>
    public bool IsChangingWorkspace => _workspaceController?.IsChangingWorkspace == true;

    /// <summary>
    /// 获取当前是否可以应用工作路径。
    /// </summary>
    public bool CanApplyWorkspace => _workspaceController is not null && !IsChangingWorkspace;

    private void InitializeReasoningEfforts()
    {
        AvailableReasoningEfforts.Add(new ReasoningEffortOptionViewModel("默认", null));
        AvailableReasoningEfforts.Add(new ReasoningEffortOptionViewModel("低", ReasoningEffort.Low));
        AvailableReasoningEfforts.Add(new ReasoningEffortOptionViewModel("中", ReasoningEffort.Medium));
        AvailableReasoningEfforts.Add(new ReasoningEffortOptionViewModel("高", ReasoningEffort.High));
        _selectedReasoningEffort = AvailableReasoningEfforts[0];
    }

    private void InitializeAvailableModels()
    {
        RefreshAvailableModels();
    }

    internal void RefreshAvailableModels(string? preferredModelDisplayName = null)
    {
        if (_chatManager is null)
        {
            return;
        }

        preferredModelDisplayName ??= _selectedModel?.DisplayName;
        AvailableModels.Clear();
        foreach (var model in _chatManager.AgentApiEndpointManager.GetSupportedModels())
        {
            AvailableModels.Add(new LanguageModelOptionViewModel(model));
        }

        LanguageModelOptionViewModel? selectedModel = AvailableModels.FirstOrDefault
            (option => string.Equals(option.DisplayName, preferredModelDisplayName, StringComparison.Ordinal));
        selectedModel ??= AvailableModels.FirstOrDefault
            (option => ReferenceEquals(option.Model, _chatManager.AgentApiEndpointManager.PrimaryModel));

        _selectedModel = selectedModel;
        if (selectedModel is not null)
        {
            _chatManager.AgentApiEndpointManager.PrimaryModel = selectedModel.Model;
            _modelStatusText = $"当前模型：{selectedModel.DisplayName}";
        }
        else
        {
            _modelStatusText = "没有可用模型";
        }

        OnPropertyChanged(nameof(SelectedModel));
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// 尝试添加一张待发送图片。
    /// </summary>
    /// <param name="fileName">图片文件名。</param>
    /// <param name="data">图片二进制数据。</param>
    /// <returns>图片格式受支持且数据非空时返回 <see langword="true"/>。</returns>
    public bool TryAddImageAttachment(string fileName, ReadOnlyMemory<byte> data)
    {
        if (!ImageAttachmentViewModel.TryCreate(fileName, data, out ImageAttachmentViewModel? attachment))
        {
            return false;
        }

        PendingImages.Add(attachment);
        return true;
    }

    /// <summary>
    /// 移除一张待发送图片。
    /// </summary>
    /// <param name="attachment">要移除的图片附件。</param>
    public void RemoveImageAttachment(ImageAttachmentViewModel attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        PendingImages.Remove(attachment);
    }

    internal Task AddSystemNoticeAsync(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return AddSystemMessageAsync(_subscribedSession, content);
    }

    /// <summary>
    /// 同意指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void ApproveTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        _chatManager?.ApproveToolExecution(approvalToolItem);
    }

    /// <summary>
    /// 拒绝指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void RejectTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        _chatManager?.RejectToolExecution(approvalToolItem);
    }

    private void OnWorkspaceControllerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodingWorkspaceController.WorkspaceInput))
        {
            OnPropertyChanged(nameof(WorkspaceInput));
        }
        else if (e.PropertyName == nameof(CodingWorkspaceController.NextRunWorkspacePath))
        {
            OnPropertyChanged(nameof(NextRunWorkspacePath));
        }
        else if (e.PropertyName == nameof(CodingWorkspaceController.StatusText))
        {
            OnPropertyChanged(nameof(WorkspaceStatusText));
        }
        else if (e.PropertyName == nameof(CodingWorkspaceController.IsChangingWorkspace))
        {
            OnPropertyChanged(nameof(IsChangingWorkspace));
            OnPropertyChanged(nameof(CanApplyWorkspace));
            RaiseCommandCanExecuteChanged();
        }
    }

    private async Task ApplyWorkspaceAsync()
    {
        if (_workspaceController is null || !CanApplyWorkspace)
        {
            return;
        }

        CopilotChatSession? session = _subscribedSession;
        try
        {
            await _workspaceController.ChangeWorkspaceAsync(WorkspaceInput).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            const string message = "工作路径切换已取消。";
            Trace.TraceInformation(message);
            await AddSystemMessageAsync(session, message).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Trace.TraceError($"工作路径切换失败：{exception}");
            await AddSystemMessageAsync(session, $"工作路径切换失败：{exception.Message}").ConfigureAwait(true);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_chatManager is not null)
        {
            _chatManager.PropertyChanged -= OnChatManagerPropertyChanged;
        }

        if (_application is not null)
        {
            _application.StateChanged -= OnApplicationStateChanged;
        }

        if (_workspaceController is not null)
        {
            _workspaceController.PropertyChanged -= OnWorkspaceControllerPropertyChanged;
        }

        if (_abilityCatalog is not null)
        {
            _abilityCatalog.Changed -= OnAbilityCatalogChanged;
        }

        DetachSession();
        ClearMessages();
        PendingImages.CollectionChanged -= OnPendingImagesCollectionChanged;
        PendingImages.Clear();
        _isDisposed = true;
    }

    private void OnApplicationStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanSend));
        OnPropertyChanged(nameof(CanCompressConversation));
        OnPropertyChanged(nameof(CanStopLanguageServer));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsCompressing));
        OnPropertyChanged(nameof(IsFinalizing));
        OnPropertyChanged(nameof(SendButtonText));
        RaiseCommandCanExecuteChanged();
    }

    private async Task StopLanguageServerAsync()
    {
        if (_application is null || !CanStopLanguageServer)
        {
            return;
        }

        try
        {
            bool stopped = await _application.StopLanguageServerAsync().ConfigureAwait(true);
            _runStatusText = stopped
                ? "LSP 服务已结束，将在下次调用符号工具时重新启动"
                : "当前没有正在运行的 LSP 服务";
            await AddSystemMessageAsync(_subscribedSession, _runStatusText).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _runStatusText = $"结束 LSP 服务失败：{exception.Message}";
            await AddSystemMessageAsync(_subscribedSession, _runStatusText).ConfigureAwait(true);
        }
        finally
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private Task CompressConversationAsync() => CompressConversationAsync(null);

    private async Task CompressConversationAsync(string? compressionRequest)
    {
        if (_application is null || !CanCompressConversation)
        {
            return;
        }

        CopilotChatSession? session = _subscribedSession;
        _runStatusText = "正在压缩对话";
        OnPropertyChanged(nameof(StatusText));
        try
        {
            await _application.CompressConversationAsync(compressionRequest).ConfigureAwait(true);
            _runStatusText = "对话压缩完成";
            await AddSystemMessageAsync(session, "对话压缩完成。").ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            _runStatusText = "对话压缩已取消";
            await AddSystemMessageAsync(session, "对话压缩已取消。").ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _runStatusText = $"对话压缩失败：{exception.Message}";
            await AddSystemMessageAsync(session, _runStatusText).ConfigureAwait(true);
        }
        finally
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private async Task SendAsync()
    {
        if (_application is null || !CanSend || SelectedAbility is null)
        {
            return;
        }

        AbilityOptionViewModel ability = SelectedAbility;
        string originalInput = InputText;
        if (ability.IsCompression)
        {
            string compressionRequest = ability.Definition.Expand(originalInput);
            InputText = string.Empty;
            SelectProgrammingAbility();
            await CompressConversationAsync(compressionRequest).ConfigureAwait(true);
            return;
        }

        string expandedInput = ability.Definition.Expand(originalInput);
        var contents = new List<AIContent>(PendingImages.Count + 1);
        if (!string.IsNullOrWhiteSpace(expandedInput))
        {
            contents.Add(new TextContent(expandedInput));
        }

        foreach (ImageAttachmentViewModel attachment in PendingImages)
        {
            contents.Add(new DataContent(attachment.Data.ToMemory(), attachment.MimeType));
        }

        CopilotChatSession? session = _subscribedSession;
        string loopPrompt = expandedInput;
        bool isInterruption = IsRunning;
        bool runLoopIteration = IsLoopIterationEnabled && !isInterruption;

        InputText = string.Empty;
        PendingImages.Clear();
        SelectProgrammingAbility();
        _runStatusText = isInterruption ? "正在提交插话" : "正在运行";
        OnPropertyChanged(nameof(StatusText));
        var runOptions = new CodingChatRunOptions(
            IsAutomaticCompressionEnabled,
            IsDotNetRunEnabled,
            SelectedReasoningEffort?.Value);
        try
        {
            if (runLoopIteration)
            {
                await _application
                    .RunLoopIterationAsync(loopPrompt, runOptions)
                    .ConfigureAwait(true);
            }
            else
            {
                await _application
                    .SendMessageAsync(contents, runOptions)
                    .ConfigureAwait(true);
            }

            _runStatusText = isInterruption && IsRunning
                ? "插话已提交，等待 Agent 处理"
                : IsRunning
                    ? "正在运行"
                    : null;
        }
        catch (OperationCanceledException)
        {
            _runStatusText = "已停止";
            await AddSystemMessageAsync(session, "运行已停止。").ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _runStatusText = $"运行失败：{exception.Message}";
            await AddSystemMessageAsync(session, _runStatusText).ConfigureAwait(true);
        }
        finally
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    /// <summary>在能力菜单打开时异步刷新目录，不阻塞菜单展示。</summary>
    public void RefreshAbilities()
    {
        if (_abilityCatalog is not null)
        {
            _ = _abilityCatalog.RefreshAsync();
        }
    }

    private void OnAbilityCatalogChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            RebuildAbilities();
        }
        else
        {
            Dispatcher.UIThread.Post(RebuildAbilities);
        }
    }

    private void RebuildAbilities()
    {
        string selectedId = SelectedAbility?.Id ?? AbilityCatalog.ProgrammingId;
        AvailableAbilities.Clear();
        AvailableAbilities.Add(new AbilityOptionViewModel(new AbilityDefinition(
            AbilityCatalog.ProgrammingId, "编程", null, null)));
        AbilityCollection abilities = _abilityCatalog?.Current ?? AbilityCollection.Empty;
        AvailableAbilities.Add(new AbilityOptionViewModel(new AbilityDefinition(
            AbilityCatalog.CompressionId,
            abilities.CompressionDisplayName ?? "按照指令进行压缩",
            abilities.CompressionPrompt,
            null)));
        foreach (AbilityDefinition ability in abilities.Items)
        {
            AvailableAbilities.Add(new AbilityOptionViewModel(ability));
        }

        _selectedAbility = AvailableAbilities.FirstOrDefault(item =>
                               string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                           ?? AvailableAbilities[0];
        OnPropertyChanged(nameof(SelectedAbility));
        OnPropertyChanged(nameof(SendButtonText));
        OnPropertyChanged(nameof(CanSend));
        RaiseCommandCanExecuteChanged();
    }

    private void SelectProgrammingAbility()
    {
        SelectedAbility = AvailableAbilities.First(item => item.Id == AbilityCatalog.ProgrammingId);
    }

    private void OnPendingImagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasPendingImages));
        OnPropertyChanged(nameof(CanSend));
        RaiseCommandCanExecuteChanged();
    }

    private void RaiseCommandCanExecuteChanged()
    {
        if (SendCommand is SimpleAsyncCommand sendCommand)
        {
            sendCommand.RaiseCanExecuteChanged();
        }

        if (CompressConversationCommand is SimpleAsyncCommand compressConversationCommand)
        {
            compressConversationCommand.RaiseCanExecuteChanged();
        }

        if (StopCommand is SimpleAsyncCommand stopCommand)
        {
            stopCommand.RaiseCanExecuteChanged();
        }

        if (ApplyWorkspaceCommand is SimpleAsyncCommand applyWorkspaceCommand)
        {
            applyWorkspaceCommand.RaiseCanExecuteChanged();
        }

        if (StopLanguageServerCommand is SimpleAsyncCommand stopLanguageServerCommand)
        {
            stopLanguageServerCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotChatManager.SelectedSession) && _chatManager is not null)
        {
            AttachSession(_chatManager.SelectedSession);
            OnPropertyChanged(nameof(CanCompressConversation));
            RaiseCommandCanExecuteChanged();
        }
    }

    private void AttachSession(CopilotChatSession session)
    {
        if (ReferenceEquals(_subscribedSession, session))
        {
            return;
        }

        DetachSession();
        ClearMessages();
        _subscribedSession = session;
        _subscribedSession.PropertyChanged += OnSessionPropertyChanged;
        _subscribedSession.ChatMessages.CollectionChanged += OnChatMessagesCollectionChanged;
        foreach (CopilotChatMessage message in _subscribedSession.ChatMessages)
        {
            Messages.Add(new MessageItemViewModel(message));
        }

        OnPropertyChanged(nameof(ShowWelcome));
        OnPropertyChanged(nameof(CurrentSessionId));
        OnPropertyChanged(nameof(CurrentSessionTitle));
    }

    private void DetachSession()
    {
        if (_subscribedSession is null)
        {
            return;
        }

        _subscribedSession.PropertyChanged -= OnSessionPropertyChanged;
        _subscribedSession.ChatMessages.CollectionChanged -= OnChatMessagesCollectionChanged;
        _subscribedSession = null;
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotChatSession.Title))
        {
            OnPropertyChanged(nameof(CurrentSessionTitle));
        }
    }

    private void OnChatMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            int insertionIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : Messages.Count;
            foreach (CopilotChatMessage message in e.NewItems)
            {
                Messages.Insert(insertionIndex++, new MessageItemViewModel(message));
            }

            OnPropertyChanged(nameof(ShowWelcome));
            return;
        }

        RebuildMessages();
        OnPropertyChanged(nameof(ShowWelcome));
    }

    private void RebuildMessages()
    {
        ClearMessages();
        if (_subscribedSession is null)
        {
            return;
        }

        foreach (CopilotChatMessage message in _subscribedSession.ChatMessages)
        {
            Messages.Add(new MessageItemViewModel(message));
        }
    }

    private void ClearMessages()
    {
        foreach (MessageItemViewModel message in Messages)
        {
            message.Dispose();
        }

        Messages.Clear();
    }

    private static Task AddSystemMessageAsync(CopilotChatSession? session, string content)
    {
        if (session is null)
        {
            return Task.CompletedTask;
        }

        var message = new CopilotChatMessage(ChatRole.System, content)
        {
            IsPresetInfo = true,
        };
        return session.AddMessageAsync(message);
    }
}