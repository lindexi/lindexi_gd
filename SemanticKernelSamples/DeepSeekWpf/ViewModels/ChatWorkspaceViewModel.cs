using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using AgentLib.Model;
using DeepSeekWpf.Infrastructure;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;

namespace DeepSeekWpf.ViewModels;

public sealed class ChatWorkspaceViewModel : ViewModelBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IChatRepository _chatRepository;
    private readonly ISettingsService _settingsService;
    private readonly IAgentModelService _agentModelService;
    private readonly IUserInteractionService _userInteractionService;
    private readonly IAppLogger _logger;
    private readonly ObservableCollection<ChatMessageViewModel> _emptyMessages = [];
    private readonly AsyncRelayCommand _sendMessageCommand;
    private readonly RelayCommand _stopCommand;
    private readonly AsyncRelayCommand _newChatCommand;
    private readonly AsyncRelayCommand _deleteSelectedChatCommand;
    private readonly AsyncRelayCommand _reloadSessionsCommand;
    private readonly RelayCommand _beginInlineEditCommand;
    private readonly AsyncRelayCommand _saveInlineEditCommand;
    private readonly RelayCommand _cancelInlineEditCommand;
    private readonly AsyncRelayCommand _renameSessionCommand;
    private readonly AsyncRelayCommand _retryCommand;
    private Task _activeResponseTask = Task.CompletedTask;
    private ChatSession? _selectedSession;
    private ChatMessageViewModel? _selectedMessage;
    private CancellationTokenSource? _responseCancellationTokenSource;
    private string _pendingUserMessage = string.Empty;
    private string _statusMessage = "就绪";
    private bool _isResponding;
    private bool _isLoadingSessions;
    private string _searchText = string.Empty;
    private string _editableSessionTitle = string.Empty;
    private string _errorMessage = string.Empty;
    private FailedSendContext? _failedSendContext;

    public ChatWorkspaceViewModel(
        IAiChatService aiChatService,
        IChatRepository chatRepository,
        ISettingsService settingsService,
        IAgentModelService agentModelService,
        IUserInteractionService userInteractionService,
        IAppLogger logger)
    {
        _aiChatService = aiChatService;
        _chatRepository = chatRepository;
        _settingsService = settingsService;
        _agentModelService = agentModelService;
        _userInteractionService = userInteractionService;
        _logger = logger;

        Sessions = [];
        FilteredSessions = CollectionViewSource.GetDefaultView(Sessions);
        FilteredSessions.Filter = FilterSession;
        FilteredSessions.SortDescriptions.Add(new SortDescription(nameof(ChatSession.UpdatedAt), ListSortDirection.Descending));
        _sendMessageCommand = new AsyncRelayCommand(StartSendMessageAsync, CanSendMessage);
        _stopCommand = new RelayCommand(StopResponse, () => IsResponding);
        _newChatCommand = new AsyncRelayCommand(CreateNewChatAsync, CanCreateNewChat);
        _deleteSelectedChatCommand = new AsyncRelayCommand(DeleteSelectedChatAsync, CanDeleteSelectedChat);
        _reloadSessionsCommand = new AsyncRelayCommand(ReloadSessionsAsync, CanReloadSessions);
        _beginInlineEditCommand = new RelayCommand(BeginInlineEdit, CanBeginInlineEdit);
        _saveInlineEditCommand = new AsyncRelayCommand(SaveInlineEditAsync, CanSaveInlineEdit);
        _cancelInlineEditCommand = new RelayCommand(CancelInlineEdit, CanCancelInlineEdit);
        _renameSessionCommand = new AsyncRelayCommand(RenameSessionAsync, CanRenameSession);
        _retryCommand = new AsyncRelayCommand(RetryAsync, () => CanRetry);
    }

    private async Task GenerateReplyAsync(ChatSession session, ChatMessageViewModel userMessage)
    {
        var assistantMessage = new ChatMessageViewModel(
            CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false));
        session.Messages.Add(assistantMessage);
        SelectedMessage = assistantMessage;
        NotifySessionStateChanged();

        _responseCancellationTokenSource = new CancellationTokenSource();
        IsResponding = true;
        StatusMessage = "模型正在流式生成回复";
        var lastSavedAt = DateTime.UtcNow;

        try
        {
            await foreach (var chunk in _aiChatService.GetReplyAsync(session, _responseCancellationTokenSource.Token))
            {
                if (chunk.Part == AiResponsePart.Thought)
                {
                    assistantMessage.Message.AppendReasoning(chunk.Delta);
                }
                else
                {
                    assistantMessage.Message.AppendText(chunk.Delta);
                }

                if (DateTime.UtcNow - lastSavedAt >= TimeSpan.FromMilliseconds(500))
                {
                    session.Touch();
                    await PersistSessionAsync(session);
                    lastSavedAt = DateTime.UtcNow;
                }
            }

            _failedSendContext = null;
            ClearError();
            StatusMessage = "回复完成";
            await _logger.InformationAsync($"收到流式回复，会话：{session.Id}");
        }
        catch (OperationCanceledException)
        {
            _failedSendContext = null;
            ClearError();
            StatusMessage = "已停止生成";
            await _logger.InformationAsync($"停止生成，会话：{session.Id}");
        }
        catch (AiChatException exception)
        {
            var errorMessage = GetAiErrorMessage(exception);
            SetError(errorMessage);
            StatusMessage = errorMessage;
            _failedSendContext = exception.IsRetryable
                ? new FailedSendContext(session.Id, userMessage.Id, assistantMessage.Id)
                : null;
            await _logger.ErrorAsync(
                $"生成失败，会话：{session.Id}，类别：{exception.Category}，关联 ID：{exception.CorrelationId}",
                exception);
        }
        catch (Exception exception)
        {
            _failedSendContext = null;
            SetError("生成失败：发生未知错误，请稍后重试。");
            StatusMessage = ErrorMessage;
            await _logger.ErrorAsync($"生成失败，会话：{session.Id}，异常类型：{exception.GetType().Name}");
        }
        finally
        {
            session.Touch();
            MoveSessionToTop(session);
            await PersistSessionAsync(session);
            _responseCancellationTokenSource?.Dispose();
            _responseCancellationTokenSource = null;
            IsResponding = false;
            OnPropertyChanged(nameof(CanRetry));
            NotifySessionStateChanged();
        }
    }

    private async Task RetryAsync()
    {
        var context = _failedSendContext;
        if (context is null)
        {
            return;
        }

        var session = Sessions.FirstOrDefault(item => item.Id == context.SessionId);
        var userMessage = session?.Messages.FirstOrDefault(item => item.Id == context.UserMessageId);
        if (session is null || userMessage is null)
        {
            _failedSendContext = null;
            OnPropertyChanged(nameof(CanRetry));
            return;
        }

        var failedAssistant = session.Messages.FirstOrDefault(item => item.Id == context.AssistantMessageId);
        if (failedAssistant is not null)
        {
            session.Messages.Remove(failedAssistant);
        }

        _failedSendContext = null;
        ClearError();
        SelectedSession = session;
        await PersistSessionAsync(session);
        await GenerateReplyAsync(session, userMessage);
    }

    private async Task RenameSessionAsync()
    {
        if (SelectedSession is null)
        {
            return;
        }

        var title = EditableSessionTitle.Trim();
        if (title.Length is 0 or > 80)
        {
            SetError("会话标题不能为空且不能超过 80 个字符");
            return;
        }

        SelectedSession.Title = title;
        SelectedSession.Touch();
        await PersistSessionAsync(SelectedSession);
        MoveSessionToTop(SelectedSession);
        FilteredSessions.Refresh();
        OnPropertyChanged(nameof(CurrentSessionTitle));
        ClearError();
        StatusMessage = "会话标题已保存";
        await _logger.InformationAsync($"重命名会话：{SelectedSession.Id}");
    }

    private bool CanRenameSession() =>
        SelectedSession is not null &&
        !IsBusy &&
        !string.IsNullOrWhiteSpace(EditableSessionTitle) &&
        EditableSessionTitle.Trim().Length <= 80 &&
        !string.Equals(SelectedSession.Title, EditableSessionTitle.Trim(), StringComparison.Ordinal);

    public ObservableCollection<ChatSession> Sessions { get; }

    public ICollectionView FilteredSessions { get; }

    public IEnumerable<ChatMessageViewModel> CurrentMessages => SelectedSession?.Messages ?? _emptyMessages;

    public ChatSession? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (IsResponding && !ReferenceEquals(_selectedSession, value))
            {
                StatusMessage = "生成期间不能切换会话，请先停止生成";
                OnPropertyChanged();
                return;
            }

            if (!SetProperty(ref _selectedSession, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentMessages));
            OnPropertyChanged(nameof(CurrentSessionTitle));
            OnPropertyChanged(nameof(HasMessages));
            EditableSessionTitle = value?.Title ?? string.Empty;
            SelectedMessage = value?.Messages.LastOrDefault();
            CancelEditingOnOtherMessages(null);
            NotifyCommandStates();
        }
    }

    public ChatMessageViewModel? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            if (SetProperty(ref _selectedMessage, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public string CurrentSessionTitle => SelectedSession?.Title ?? "未选择会话";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                FilteredSessions.Refresh();
            }
        }
    }

    public string EditableSessionTitle
    {
        get => _editableSessionTitle;
        set
        {
            if (SetProperty(ref _editableSessionTitle, value ?? string.Empty))
            {
                _renameSessionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string PendingUserMessage
    {
        get => _pendingUserMessage;
        set
        {
            if (SetProperty(ref _pendingUserMessage, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ShouldShowStatus));
            }
        }
    }

    public bool HasSessions => Sessions.Count > 0;

    public bool HasMessages => SelectedSession?.Messages.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy => IsResponding || IsLoadingSessions;

    public bool ConfigurationRequired => _agentModelService.SelectedModel is null;

    public bool CanRetry => _failedSendContext is not null && !IsBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(ShouldShowStatus));
            }
        }
    }

    public bool ShouldShowStatus => IsBusy || HasError;

    public bool IsResponding
    {
        get => _isResponding;
        private set
        {
            if (SetProperty(ref _isResponding, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(CanRetry));
                OnPropertyChanged(nameof(ShouldShowStatus));
                NotifyCommandStates();
            }
        }
    }

    public bool IsLoadingSessions
    {
        get => _isLoadingSessions;
        private set
        {
            if (SetProperty(ref _isLoadingSessions, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(CanRetry));
                OnPropertyChanged(nameof(ShouldShowStatus));
                NotifyCommandStates();
            }
        }
    }

    public AsyncRelayCommand SendMessageCommand => _sendMessageCommand;

    public RelayCommand StopCommand => _stopCommand;

    public AsyncRelayCommand NewChatCommand => _newChatCommand;

    public AsyncRelayCommand DeleteSelectedChatCommand => _deleteSelectedChatCommand;

    public AsyncRelayCommand ReloadSessionsCommand => _reloadSessionsCommand;

    public RelayCommand BeginInlineEditCommand => _beginInlineEditCommand;

    public AsyncRelayCommand SaveInlineEditCommand => _saveInlineEditCommand;

    public RelayCommand CancelInlineEditCommand => _cancelInlineEditCommand;

    public AsyncRelayCommand RenameSessionCommand => _renameSessionCommand;

    public AsyncRelayCommand RetryCommand => _retryCommand;

    public Task InitializeAsync()
    {
        return ReloadSessionsAsync();
    }

    public void Stop()
    {
        StopResponse();
    }

    public void RefreshConfigurationState()
    {
        OnPropertyChanged(nameof(ConfigurationRequired));
        NotifyCommandStates();
    }

    public void RefreshSettings()
    {
        RefreshConfigurationState();
    }

    public async Task ShutdownAsync()
    {
        Stop();
        await _activeResponseTask;

        await Task.WhenAll(Sessions.Select(session => PersistSessionAsync(session)));

        await _logger.InformationAsync("聊天工作区已关闭并保存会话");
    }

    public async Task ReloadSessionsAsync()
    {
        if (IsLoadingSessions)
        {
            return;
        }

        try
        {
            IsLoadingSessions = true;
            ClearError();
            StatusMessage = "正在异步加载历史会话...";
            var previousSessionId = SelectedSession?.Id;
            var loadResult = await _chatRepository.LoadSessionsAsync();

            foreach (var existingSession in Sessions)
            {
                UnsubscribeSession(existingSession);
            }

            Sessions.Clear();
            foreach (var session in loadResult.Sessions)
            {
                SubscribeSession(session);
                Sessions.Add(session);
            }

            NotifySessionStateChanged();

            if (Sessions.Count == 0)
            {
                await CreateNewChatAsync();
                StatusMessage = loadResult.Issues.Count == 0
                    ? "未找到历史会话，已创建新会话"
                    : $"加载成功 0 个，失败 {loadResult.Issues.Count} 个；已创建新会话";
                await LogLoadIssuesAsync(loadResult.Issues);
                return;
            }

            SelectedSession = Sessions.FirstOrDefault(session => session.Id == previousSessionId) ?? Sessions[0];
            StatusMessage = $"加载成功 {Sessions.Count} 个，失败 {loadResult.Issues.Count} 个";
            await _logger.InformationAsync(
                $"异步加载会话完成，成功：{Sessions.Count}，失败：{loadResult.Issues.Count}");
            await LogLoadIssuesAsync(loadResult.Issues);
        }
        catch (Exception exception)
        {
            StatusMessage = "加载历史会话失败";
            SetError("加载历史会话失败");
            await _logger.ErrorAsync("加载历史会话失败", exception);
        }
        finally
        {
            IsLoadingSessions = false;
        }
    }

    private async Task CreateNewChatAsync()
    {
        var session = new ChatSession();
        session.Touch();
        SubscribeSession(session);
        Sessions.Insert(0, session);
        SelectedSession = session;
        await PersistSessionAsync(session);
        NotifySessionStateChanged();
        StatusMessage = "已创建新会话";
        await _logger.InformationAsync($"创建新会话：{session.Id}");
    }

    private bool CanCreateNewChat()
    {
        return !IsResponding && !IsLoadingSessions;
    }

    private async Task DeleteSelectedChatAsync()
    {
        if (SelectedSession is null)
        {
            return;
        }

        var selectedSession = SelectedSession;
        if (!await _userInteractionService.ConfirmAsync(
                "删除会话",
                $"确定要删除会话“{selectedSession.Title}”吗？"))
        {
            return;
        }

        var deletedSessionId = selectedSession.Id;
        await _chatRepository.DeleteSessionAsync(deletedSessionId);
        UnsubscribeSession(selectedSession);
        Sessions.Remove(selectedSession);

        if (Sessions.Count == 0)
        {
            SelectedSession = null;
            await CreateNewChatAsync();
        }
        else
        {
            SelectedSession = Sessions[0];
        }

        StatusMessage = "已删除选中会话";
        NotifySessionStateChanged();
        await _logger.InformationAsync($"删除会话：{deletedSessionId}");
    }

    private bool CanDeleteSelectedChat()
    {
        return SelectedSession is not null && !IsResponding && !IsLoadingSessions;
    }

    private Task StartSendMessageAsync()
    {
        var responseTask = SendMessageAsync();
        _activeResponseTask = responseTask;
        return responseTask;
    }

    private async Task SendMessageAsync()
    {
        var prompt = PendingUserMessage.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        if (SelectedSession is null)
        {
            await CreateNewChatAsync();
        }

        var session = SelectedSession!;
        _failedSendContext = null;
        ClearError();
        CancelEditingOnOtherMessages(null);

        var userMessage = new ChatMessageViewModel(CopilotChatMessage.CreateUser(prompt));

        session.Messages.Add(userMessage);
        NotifySessionStateChanged();
        session.RefreshTitleFromMessages();
        session.Touch();
        PendingUserMessage = string.Empty;
        MoveSessionToTop(session);
        await PersistSessionAsync(session);
        OnPropertyChanged(nameof(CurrentSessionTitle));
        await _logger.InformationAsync($"发送消息，会话：{session.Id}");

        await GenerateReplyAsync(session, userMessage);
    }

    private static string GetAiErrorMessage(AiChatException exception)
    {
        return exception.Category switch
        {
            AiChatErrorCategory.Configuration => "生成失败：模型配置不可用，请前往设置检查配置。",
            AiChatErrorCategory.Authentication => "生成失败：模型服务认证失败，请检查凭据。",
            AiChatErrorCategory.RateLimit => "生成失败：请求过于频繁，请稍后重试。",
            AiChatErrorCategory.Network => "生成失败：网络连接异常，请检查网络后重试。",
            AiChatErrorCategory.Timeout => "生成失败：请求超时，请稍后重试。",
            AiChatErrorCategory.Server => "生成失败：模型服务暂时不可用，请稍后重试。",
            AiChatErrorCategory.Protocol => "生成失败：模型服务响应不兼容，请检查配置。",
            AiChatErrorCategory.EmptyResponse => "生成失败：模型未返回内容，请重试。",
            AiChatErrorCategory.Storage => "生成失败：本地 Agent 工作区不可用，请检查目录权限。",
            AiChatErrorCategory.Canceled => "已停止生成",
            _ => "生成失败：发生未知错误，请稍后重试。",
        };
    }

    private bool CanSendMessage()
    {
        return !ConfigurationRequired &&
               !IsResponding &&
               !IsLoadingSessions &&
               !string.IsNullOrWhiteSpace(PendingUserMessage);
    }

    private bool CanReloadSessions()
    {
        return !IsResponding && !IsLoadingSessions;
    }

    private void StopResponse()
    {
        _responseCancellationTokenSource?.Cancel();
        StatusMessage = "正在停止生成";
    }

    private void BeginInlineEdit(object? parameter)
    {
        if (parameter is not ChatMessageViewModel message || SelectedSession is null)
        {
            return;
        }

        CancelEditingOnOtherMessages(message);
        message.EditingContent = message.Content;
        message.EditingThoughtContent = message.ThoughtContent;
        message.IsEditing = true;
        SelectedMessage = message;
        StatusMessage = "正在编辑消息";
        NotifyCommandStates();
    }

    private bool CanBeginInlineEdit(object? parameter)
    {
        return parameter is ChatMessageViewModel && !IsResponding && !IsLoadingSessions;
    }

    private async Task SaveInlineEditAsync(object? parameter)
    {
        if (parameter is not ChatMessageViewModel message || SelectedSession is null)
        {
            return;
        }

        var updatedContent = message.EditingContent.Trim();
        if (string.IsNullOrWhiteSpace(updatedContent))
        {
            return;
        }

        var updatedThoughtContent = message.IsAssistant
            ? message.EditingThoughtContent.Trim()
            : message.ThoughtContent;
        message.ReplaceContent(updatedContent, updatedThoughtContent);

        var messageIndex = SelectedSession.Messages.IndexOf(message);
        while (SelectedSession.Messages.Count > messageIndex + 1)
        {
            SelectedSession.Messages.RemoveAt(SelectedSession.Messages.Count - 1);
        }

        message.IsEditing = false;
        SelectedSession.RefreshTitleFromMessages();
        SelectedSession.Touch();
        await PersistSessionAsync(SelectedSession);
        OnPropertyChanged(nameof(CurrentSessionTitle));
        StatusMessage = "已截断后续上下文，可重新发送";
        _failedSendContext = null;
        ClearError();
        NotifySessionStateChanged();
        await _logger.InformationAsync($"编辑消息，会话：{SelectedSession.Id}，消息：{message.Id}");
        NotifyCommandStates();
    }

    private bool CanSaveInlineEdit(object? parameter)
    {
        if (parameter is not ChatMessageViewModel message || SelectedSession is null || IsResponding || IsLoadingSessions)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.EditingContent))
        {
            return false;
        }

        return !string.Equals(message.Content, message.EditingContent, StringComparison.Ordinal) ||
               !string.Equals(message.ThoughtContent, message.EditingThoughtContent, StringComparison.Ordinal);
    }

    private void CancelInlineEdit(object? parameter)
    {
        if (parameter is not ChatMessageViewModel message)
        {
            return;
        }

        message.EditingContent = message.Content;
        message.EditingThoughtContent = message.ThoughtContent;
        message.IsEditing = false;
        StatusMessage = "已取消消息编辑";
        NotifyCommandStates();
    }

    private bool CanCancelInlineEdit(object? parameter)
    {
        return parameter is ChatMessageViewModel message && message.IsEditing;
    }

    private void CancelEditingOnOtherMessages(ChatMessageViewModel? excludedMessage)
    {
        if (SelectedSession is null)
        {
            return;
        }

        foreach (var message in SelectedSession.Messages.Where(message => !ReferenceEquals(message, excludedMessage) && message.IsEditing))
        {
            message.EditingContent = message.Content;
            message.EditingThoughtContent = message.ThoughtContent;
            message.IsEditing = false;
        }
    }

    private Task PersistSessionAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        return _chatRepository.SaveSessionAsync(session, cancellationToken);
    }

    private async Task LogLoadIssuesAsync(IReadOnlyList<ChatLoadIssue> issues)
    {
        foreach (var issue in issues)
        {
            await _logger.WarningAsync(
                $"会话文件加载失败，文件：{issue.FileName}，异常类型：{issue.ExceptionType}，恢复建议：{issue.RecoverySuggestion}");
        }
    }

    private void MoveSessionToTop(ChatSession session)
    {
        var existingIndex = Sessions.IndexOf(session);
        if (existingIndex <= 0)
        {
            return;
        }

        Sessions.Move(existingIndex, 0);
    }

    private bool FilterSession(object item)
    {
        if (item is not ChatSession session || string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var searchText = SearchText.Trim();
        return session.Title.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
               session.Messages.Any(message =>
                   message.Content.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
    }

    private void SubscribeSession(ChatSession session)
    {
        session.PropertyChanged += SessionOnPropertyChanged;
        session.Messages.CollectionChanged += MessagesOnCollectionChanged;
    }

    private void UnsubscribeSession(ChatSession session)
    {
        session.PropertyChanged -= SessionOnPropertyChanged;
        session.Messages.CollectionChanged -= MessagesOnCollectionChanged;
    }

    private void SessionOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatSession.Title) or nameof(ChatSession.UpdatedAt))
        {
            FilteredSessions.Refresh();
        }
    }

    private void MessagesOnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        FilteredSessions.Refresh();
        OnPropertyChanged(nameof(HasMessages));
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    private void NotifySessionStateChanged()
    {
        OnPropertyChanged(nameof(HasSessions));
        OnPropertyChanged(nameof(HasMessages));
        OnPropertyChanged(nameof(ConfigurationRequired));
        OnPropertyChanged(nameof(CanRetry));
        FilteredSessions.Refresh();
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        _sendMessageCommand.NotifyCanExecuteChanged();
        _stopCommand.NotifyCanExecuteChanged();
        _newChatCommand.NotifyCanExecuteChanged();
        _deleteSelectedChatCommand.NotifyCanExecuteChanged();
        _reloadSessionsCommand.NotifyCanExecuteChanged();
        _beginInlineEditCommand.NotifyCanExecuteChanged();
        _saveInlineEditCommand.NotifyCanExecuteChanged();
        _cancelInlineEditCommand.NotifyCanExecuteChanged();
        _renameSessionCommand.NotifyCanExecuteChanged();
        _retryCommand.NotifyCanExecuteChanged();
    }

    private sealed record FailedSendContext(Guid SessionId, Guid UserMessageId, Guid AssistantMessageId);
}
