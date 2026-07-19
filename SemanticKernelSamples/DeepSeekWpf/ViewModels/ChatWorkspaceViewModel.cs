using System.Collections.ObjectModel;
using System.ComponentModel;
using AgentLib;
using AgentLib.Model;
using DeepSeekWpf.Infrastructure;
using DeepSeekWpf.Services;

namespace DeepSeekWpf.ViewModels;

public sealed class ChatWorkspaceViewModel : ViewModelBase
{
    private readonly CopilotChatManager _chatManager;
    private readonly IAppLogger _logger;
    private readonly AsyncRelayCommand _sendMessageCommand;
    private readonly RelayCommand _stopCommand;
    private readonly RelayCommand _newChatCommand;
    private readonly RelayCommand _deleteSelectedChatCommand;
    private string _pendingUserMessage = string.Empty;
    private string _statusMessage = "就绪";

    public ChatWorkspaceViewModel(CopilotChatManager chatManager, IAppLogger logger)
    {
        _chatManager = chatManager;
        _logger = logger;
        _sendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
        _stopCommand = new RelayCommand(StopResponse, () => IsResponding);
        _newChatCommand = new RelayCommand(CreateNewChat, () => !IsResponding);
        _deleteSelectedChatCommand = new RelayCommand(DeleteSelectedChat, CanDeleteSelectedChat);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
    }

    public ObservableCollection<CopilotChatSession> Sessions => _chatManager.ChatSessions;

    public ObservableCollection<CopilotChatMessage> CurrentMessages => _chatManager.ChatMessages;

    public CopilotChatSession SelectedSession
    {
        get => _chatManager.SelectedSession;
        set
        {
            if (ReferenceEquals(_chatManager.SelectedSession, value))
            {
                return;
            }

            _chatManager.SelectedSession = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentMessages));
            OnPropertyChanged(nameof(CurrentSessionTitle));
            NotifyCommandStates();
        }
    }

    public string CurrentSessionTitle => SelectedSession.Title;

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
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsResponding => _chatManager.IsChatting;

    public AsyncRelayCommand SendMessageCommand => _sendMessageCommand;

    public RelayCommand StopCommand => _stopCommand;

    public RelayCommand NewChatCommand => _newChatCommand;

    public RelayCommand DeleteSelectedChatCommand => _deleteSelectedChatCommand;

    public void SetConfigurationStatus(AgentConfigurationLoadResult result)
    {
        StatusMessage = result.IsDebugFallback
            ? $"已使用本地调试配置：{result.SourceDescription}"
            : $"已加载配置：{result.SourceDescription}";
    }

    private async Task SendMessageAsync()
    {
        var prompt = PendingUserMessage.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        PendingUserMessage = string.Empty;
        StatusMessage = "模型正在流式生成回复";
        _logger.Log($"发送消息，会话：{_chatManager.CurrentSessionId}");

        try
        {
            await _chatManager.SendMessageAsync(prompt);
            StatusMessage = _chatManager.WasLastChatCanceled ? "已停止生成" : "回复完成";
        }
        catch (Exception exception)
        {
            StatusMessage = "生成失败";
            _logger.Log($"生成失败，会话：{_chatManager.CurrentSessionId}，错误：{exception}");
        }
        finally
        {
            OnPropertyChanged(nameof(CurrentSessionTitle));
            NotifyCommandStates();
        }
    }

    private bool CanSendMessage()
    {
        return !IsResponding && !string.IsNullOrWhiteSpace(PendingUserMessage);
    }

    private void StopResponse()
    {
        _chatManager.CancelCurrentChat();
        StatusMessage = "正在停止生成";
    }

    private void CreateNewChat()
    {
        _chatManager.CreateNewSession();
        OnPropertyChanged(nameof(SelectedSession));
        OnPropertyChanged(nameof(CurrentMessages));
        OnPropertyChanged(nameof(CurrentSessionTitle));
        StatusMessage = "已创建新会话";
        NotifyCommandStates();
    }

    private void DeleteSelectedChat()
    {
        if (Sessions.Count <= 1)
        {
            return;
        }

        var session = SelectedSession;
        var selectedIndex = Sessions.IndexOf(session);
        Sessions.Remove(session);
        SelectedSession = Sessions[Math.Min(selectedIndex, Sessions.Count - 1)];
        StatusMessage = "已从当前运行实例删除会话";
        _logger.Log($"删除运行期会话：{session.SessionId}");
    }

    private bool CanDeleteSelectedChat()
    {
        return !IsResponding && Sessions.Count > 1;
    }

    private void OnChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotChatManager.IsChatting))
        {
            OnPropertyChanged(nameof(IsResponding));
            NotifyCommandStates();
        }
        else if (e.PropertyName == nameof(CopilotChatManager.SelectedSession))
        {
            OnPropertyChanged(nameof(SelectedSession));
            OnPropertyChanged(nameof(CurrentMessages));
            OnPropertyChanged(nameof(CurrentSessionTitle));
            NotifyCommandStates();
        }
    }

    private void NotifyCommandStates()
    {
        _sendMessageCommand.NotifyCanExecuteChanged();
        _stopCommand.NotifyCanExecuteChanged();
        _newChatCommand.NotifyCanExecuteChanged();
        _deleteSelectedChatCommand.NotifyCanExecuteChanged();
    }
}
