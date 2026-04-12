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
    private readonly RelayCommand _saveMessageEditCommand;
    private readonly RelayCommand _reloadSessionsCommand;
    private ChatSession? _selectedSession;
    private ChatMessage? _selectedMessage;
    private CancellationTokenSource? _responseCancellationTokenSource;
    private string _pendingUserMessage = string.Empty;
    private string _editableMessageContent = string.Empty;
    private string _statusMessage = "就绪";
    private bool _isResponding;

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
        _newChatCommand = new RelayCommand(CreateNewChat, () => !IsResponding);
        _deleteSelectedChatCommand = new RelayCommand(DeleteSelectedChat, () => SelectedSession is not null && !IsResponding);
        _saveMessageEditCommand = new RelayCommand(SaveMessageEdit, CanSaveMessageEdit);
        _reloadSessionsCommand = new RelayCommand(ReloadSessions, () => !IsResponding);

        ReloadSessions();
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

            if (value?.Messages.Count > 0)
            {
                SelectedMessage = value.Messages[^1];
            }
            else
            {
                SelectedMessage = null;
            }

            NotifyCommandStates();
        }
    }

    public ChatMessage? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            if (!SetProperty(ref _selectedMessage, value))
            {
                return;
            }

            EditableMessageContent = value?.Content ?? string.Empty;
            OnPropertyChanged(nameof(SelectedMessageRoleDisplay));
            OnPropertyChanged(nameof(SelectedMessageTimeDisplay));
            NotifyCommandStates();
        }
    }

    public string CurrentSessionTitle => SelectedSession?.Title ?? "未选择会话";

    public string SelectedMessageRoleDisplay => SelectedMessage?.Role.ToString() ?? "未选择消息";

    public string SelectedMessageTimeDisplay => SelectedMessage?.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

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

    public string EditableMessageContent
    {
        get => _editableMessageContent;
        set
        {
            if (SetProperty(ref _editableMessageContent, value))
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

    public AsyncRelayCommand SendMessageCommand => _sendMessageCommand;

    public RelayCommand StopCommand => _stopCommand;

    public RelayCommand NewChatCommand => _newChatCommand;

    public RelayCommand DeleteSelectedChatCommand => _deleteSelectedChatCommand;

    public RelayCommand SaveMessageEditCommand => _saveMessageEditCommand;

    public RelayCommand ReloadSessionsCommand => _reloadSessionsCommand;

    public void ReloadSessions()
    {
        var previousSessionId = SelectedSession?.Id;
        var loadedSessions = _chatRepository.LoadSessions();

        Sessions.Clear();
        foreach (var session in loadedSessions)
        {
            Sessions.Add(session);
        }

        if (Sessions.Count == 0)
        {
            CreateNewChat();
            return;
        }

        SelectedSession = Sessions.FirstOrDefault(session => session.Id == previousSessionId) ?? Sessions[0];
        StatusMessage = $"已加载 {Sessions.Count} 个会话";
    }

    private void CreateNewChat()
    {
        var session = new ChatSession();
        session.Touch();
        Sessions.Insert(0, session);
        SelectedSession = session;
        PersistSessions();
        StatusMessage = "已创建新会话";
        _logger.Log($"创建新会话：{session.Id}");
    }

    private void DeleteSelectedChat()
    {
        if (SelectedSession is null)
        {
            return;
        }

        var deletedSessionId = SelectedSession.Id;
        Sessions.Remove(SelectedSession);

        if (Sessions.Count == 0)
        {
            SelectedSession = null;
            CreateNewChat();
        }
        else
        {
            SelectedSession = Sessions[0];
            PersistSessions();
        }

        StatusMessage = "已删除选中会话";
        _logger.Log($"删除会话：{deletedSessionId}");
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
        PersistSessions();
        OnPropertyChanged(nameof(CurrentSessionTitle));
        _logger.Log($"发送消息，会话：{session.Id}");

        var assistantMessage = new ChatMessage
        {
            Role = ChatRole.Assistant,
            Content = "正在生成，请稍候...",
            CreatedAt = DateTime.Now,
        };

        session.Messages.Add(assistantMessage);
        SelectedMessage = assistantMessage;
        PersistSessions();

        _responseCancellationTokenSource = new CancellationTokenSource();
        IsResponding = true;
        StatusMessage = "模型正在生成回复";

        try
        {
            var response = await _aiChatService.GetReplyAsync(
                session,
                _settingsService.CurrentSettings,
                _responseCancellationTokenSource.Token);

            assistantMessage.Content = response;
            if (ReferenceEquals(SelectedMessage, assistantMessage))
            {
                EditableMessageContent = assistantMessage.Content;
            }

            StatusMessage = "回复完成";
            _logger.Log($"收到回复，会话：{session.Id}");
        }
        catch (OperationCanceledException)
        {
            assistantMessage.Content = "已停止生成。";
            if (ReferenceEquals(SelectedMessage, assistantMessage))
            {
                EditableMessageContent = assistantMessage.Content;
            }

            StatusMessage = "已停止生成";
            _logger.Log($"停止生成，会话：{session.Id}");
        }
        catch (Exception exception)
        {
            assistantMessage.Content = $"生成失败：{exception.Message}";
            if (ReferenceEquals(SelectedMessage, assistantMessage))
            {
                EditableMessageContent = assistantMessage.Content;
            }

            StatusMessage = "生成失败";
            _logger.Log($"生成失败，会话：{session.Id}，错误：{exception.Message}");
        }
        finally
        {
            session.Touch();
            MoveSessionToTop(session);
            PersistSessions();
            _responseCancellationTokenSource.Dispose();
            _responseCancellationTokenSource = null;
            IsResponding = false;
        }
    }

    private bool CanSendMessage()
    {
        return !IsResponding && !string.IsNullOrWhiteSpace(PendingUserMessage);
    }

    private void StopResponse()
    {
        _responseCancellationTokenSource?.Cancel();
        StatusMessage = "正在停止生成";
    }

    private void SaveMessageEdit()
    {
        if (SelectedSession is null || SelectedMessage is null)
        {
            return;
        }

        var updatedContent = EditableMessageContent.Trim();
        if (string.IsNullOrWhiteSpace(updatedContent))
        {
            return;
        }

        SelectedMessage.Content = updatedContent;
        SelectedSession.RefreshTitleFromMessages();
        SelectedSession.Touch();
        PersistSessions();
        OnPropertyChanged(nameof(CurrentSessionTitle));
        StatusMessage = "已保存消息编辑";
        _logger.Log($"编辑消息，会话：{SelectedSession.Id}，消息：{SelectedMessage.Id}");
    }

    private bool CanSaveMessageEdit()
    {
        return SelectedMessage is not null &&
               !string.IsNullOrWhiteSpace(EditableMessageContent) &&
               !string.Equals(SelectedMessage.Content, EditableMessageContent, StringComparison.Ordinal);
    }

    private void PersistSessions()
    {
        _chatRepository.SaveSessions(Sessions);
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
        _saveMessageEditCommand.NotifyCanExecuteChanged();
        _reloadSessionsCommand.NotifyCanExecuteChanged();
    }
}
