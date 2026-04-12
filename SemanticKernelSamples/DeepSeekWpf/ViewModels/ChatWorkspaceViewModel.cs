using System.Collections.ObjectModel;
using System.Linq;
using DeepSeekWpf.Infrastructure;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;

namespace DeepSeekWpf.ViewModels;

public sealed class ChatWorkspaceViewModel : ViewModelBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IChatRepository _chatRepository;
    private readonly ISettingsService _settingsService;
    private readonly IAppLogger _logger;
    private readonly ObservableCollection<ChatMessage> _emptyMessages = [];
    private readonly AsyncRelayCommand _sendMessageCommand;
    private readonly RelayCommand _stopCommand;
    private readonly RelayCommand _newChatCommand;
    private readonly RelayCommand _deleteSelectedChatCommand;
    private readonly AsyncRelayCommand _reloadSessionsCommand;
    private readonly RelayCommand _beginInlineEditCommand;
    private readonly RelayCommand _saveInlineEditCommand;
    private readonly RelayCommand _cancelInlineEditCommand;
    private ChatSession? _selectedSession;
    private ChatMessage? _selectedMessage;
    private CancellationTokenSource? _responseCancellationTokenSource;
    private string _pendingUserMessage = string.Empty;
    private string _statusMessage = "就绪";
    private bool _isResponding;
    private bool _isLoadingSessions;

    public ChatWorkspaceViewModel(
        IAiChatService aiChatService,
        IChatRepository chatRepository,
        ISettingsService settingsService,
        IAppLogger logger)
    {
        _aiChatService = aiChatService;
        _chatRepository = chatRepository;
        _settingsService = settingsService;
        _logger = logger;

        Sessions = [];
        _sendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
        _stopCommand = new RelayCommand(StopResponse, () => IsResponding);
        _newChatCommand = new RelayCommand(CreateNewChat, CanCreateNewChat);
        _deleteSelectedChatCommand = new RelayCommand(DeleteSelectedChat, CanDeleteSelectedChat);
        _reloadSessionsCommand = new AsyncRelayCommand(ReloadSessionsAsync, CanReloadSessions);
        _beginInlineEditCommand = new RelayCommand(BeginInlineEdit, CanBeginInlineEdit);
        _saveInlineEditCommand = new RelayCommand(SaveInlineEdit, CanSaveInlineEdit);
        _cancelInlineEditCommand = new RelayCommand(CancelInlineEdit, CanCancelInlineEdit);
    }

    public ObservableCollection<ChatSession> Sessions { get; }

    public IEnumerable<ChatMessage> CurrentMessages => SelectedSession?.Messages ?? _emptyMessages;

    public ChatSession? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (!SetProperty(ref _selectedSession, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentMessages));
            OnPropertyChanged(nameof(CurrentSessionTitle));
            SelectedMessage = value?.Messages.LastOrDefault();
            CancelEditingOnOtherMessages(null);
            NotifyCommandStates();
        }
    }

    public ChatMessage? SelectedMessage
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

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsResponding
    {
        get => _isResponding;
        private set
        {
            if (SetProperty(ref _isResponding, value))
            {
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
                NotifyCommandStates();
            }
        }
    }

    public AsyncRelayCommand SendMessageCommand => _sendMessageCommand;

    public RelayCommand StopCommand => _stopCommand;

    public RelayCommand NewChatCommand => _newChatCommand;

    public RelayCommand DeleteSelectedChatCommand => _deleteSelectedChatCommand;

    public AsyncRelayCommand ReloadSessionsCommand => _reloadSessionsCommand;

    public RelayCommand BeginInlineEditCommand => _beginInlineEditCommand;

    public RelayCommand SaveInlineEditCommand => _saveInlineEditCommand;

    public RelayCommand CancelInlineEditCommand => _cancelInlineEditCommand;

    public Task InitializeAsync()
    {
        return ReloadSessionsAsync();
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
            StatusMessage = "正在异步加载历史会话...";
            var previousSessionId = SelectedSession?.Id;
            var loadedSessions = await _chatRepository.LoadSessionsAsync();

            Sessions.Clear();
            foreach (var session in loadedSessions)
            {
                Sessions.Add(session);
            }

            if (Sessions.Count == 0)
            {
                CreateNewChat();
                StatusMessage = "未找到历史会话，已创建新会话";
                return;
            }

            SelectedSession = Sessions.FirstOrDefault(session => session.Id == previousSessionId) ?? Sessions[0];
            StatusMessage = $"已加载 {Sessions.Count} 个会话";
            _logger.Log($"异步加载会话完成，总数：{Sessions.Count}");
        }
        catch (Exception exception)
        {
            StatusMessage = "加载历史会话失败";
            _logger.Log($"加载历史会话失败：{exception.Message}");
        }
        finally
        {
            IsLoadingSessions = false;
        }
    }

    private void CreateNewChat()
    {
        var session = new ChatSession();
        session.Touch();
        Sessions.Insert(0, session);
        SelectedSession = session;
        PersistSession(session);
        StatusMessage = "已创建新会话";
        _logger.Log($"创建新会话：{session.Id}");
    }

    private bool CanCreateNewChat()
    {
        return !IsResponding && !IsLoadingSessions;
    }

    private void DeleteSelectedChat()
    {
        if (SelectedSession is null)
        {
            return;
        }

        var deletedSessionId = SelectedSession.Id;
        _chatRepository.DeleteSession(deletedSessionId);
        Sessions.Remove(SelectedSession);

        if (Sessions.Count == 0)
        {
            SelectedSession = null;
            CreateNewChat();
        }
        else
        {
            SelectedSession = Sessions[0];
        }

        StatusMessage = "已删除选中会话";
        _logger.Log($"删除会话：{deletedSessionId}");
    }

    private bool CanDeleteSelectedChat()
    {
        return SelectedSession is not null && !IsResponding && !IsLoadingSessions;
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
            CreateNewChat();
        }

        var session = SelectedSession!;
        CancelEditingOnOtherMessages(null);

        var userMessage = new ChatMessage
        {
            Role = ChatRole.User,
            Content = prompt,
            CreatedAt = DateTime.Now,
        };

        session.Messages.Add(userMessage);
        session.RefreshTitleFromMessages();
        session.Touch();
        PendingUserMessage = string.Empty;
        MoveSessionToTop(session);
        PersistSession(session);
        OnPropertyChanged(nameof(CurrentSessionTitle));
        _logger.Log($"发送消息，会话：{session.Id}");

        var assistantMessage = new ChatMessage
        {
            Role = ChatRole.Assistant,
            Content = string.Empty,
            ThoughtContent = string.Empty,
            CreatedAt = DateTime.Now,
        };

        session.Messages.Add(assistantMessage);
        SelectedMessage = assistantMessage;
        PersistSession(session);

        _responseCancellationTokenSource = new CancellationTokenSource();
        IsResponding = true;
        StatusMessage = "模型正在流式生成回复";

        try
        {
            await foreach (var chunk in _aiChatService.GetReplyAsync(
                               session,
                               assistantMessage,
                               _settingsService.CurrentSettings,
                               _responseCancellationTokenSource.Token))
            {
                if (chunk.Part == AiResponsePart.Thought)
                {
                    assistantMessage.ThoughtContent += chunk.Delta;
                }
                else
                {
                    assistantMessage.Content += chunk.Delta;
                }
            }

            if (string.IsNullOrWhiteSpace(assistantMessage.Content))
            {
                assistantMessage.Content = "模型未返回正文内容。";
            }

            StatusMessage = "回复完成";
            _logger.Log($"收到流式回复，会话：{session.Id}");
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrWhiteSpace(assistantMessage.Content))
            {
                assistantMessage.Content = "已停止生成。";
            }

            StatusMessage = "已停止生成";
            _logger.Log($"停止生成，会话：{session.Id}");
        }
        catch (Exception exception)
        {
            assistantMessage.Content = $"生成失败：{exception.Message}";
            StatusMessage = "生成失败";
            _logger.Log($"生成失败，会话：{session.Id}，错误：{exception.Message}");
        }
        finally
        {
            session.Touch();
            MoveSessionToTop(session);
            PersistSession(session);
            _responseCancellationTokenSource?.Dispose();
            _responseCancellationTokenSource = null;
            IsResponding = false;
        }
    }

    private bool CanSendMessage()
    {
        return !IsResponding && !IsLoadingSessions && !string.IsNullOrWhiteSpace(PendingUserMessage);
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
        if (parameter is not ChatMessage message || SelectedSession is null)
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
        return parameter is ChatMessage && !IsResponding && !IsLoadingSessions;
    }

    private void SaveInlineEdit(object? parameter)
    {
        if (parameter is not ChatMessage message || SelectedSession is null)
        {
            return;
        }

        var updatedContent = message.EditingContent.Trim();
        if (string.IsNullOrWhiteSpace(updatedContent))
        {
            return;
        }

        message.Content = updatedContent;
        if (message.IsAssistant)
        {
            message.ThoughtContent = message.EditingThoughtContent.Trim();
        }

        message.IsEditing = false;
        SelectedSession.RefreshTitleFromMessages();
        SelectedSession.Touch();
        PersistSession(SelectedSession);
        OnPropertyChanged(nameof(CurrentSessionTitle));
        StatusMessage = "已保存消息编辑";
        _logger.Log($"编辑消息，会话：{SelectedSession.Id}，消息：{message.Id}");
        NotifyCommandStates();
    }

    private bool CanSaveInlineEdit(object? parameter)
    {
        if (parameter is not ChatMessage message || SelectedSession is null || IsResponding || IsLoadingSessions)
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
        if (parameter is not ChatMessage message)
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
        return parameter is ChatMessage message && message.IsEditing;
    }

    private void CancelEditingOnOtherMessages(ChatMessage? excludedMessage)
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

    private void PersistSession(ChatSession session)
    {
        _chatRepository.SaveSession(session);
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
    }
}
