using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Services;
using AgentLib.Model;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 会话列表项 ViewModel。
/// </summary>
public sealed class SessionItemViewModel : ViewModelBase
{
    private bool _isSelected;

    /// <summary>
    /// 会话 ID。
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// 会话标题。
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// 角色数量。
    /// </summary>
    public int RoleCount { get; }

    /// <summary>
    /// 消息数量。
    /// </summary>
    public int MessageCount { get; }

    /// <summary>
    /// 显示文本。
    /// </summary>
    public string DisplayText => $"{Title} ({CreatedAt:MM-dd HH:mm})";

    /// <summary>
    /// 副标题文本。
    /// </summary>
    public string Subtitle => $"{RoleCount} 角色 · {MessageCount} 条消息";

    /// <summary>
    /// 是否选中。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>
    /// 使用会话摘要创建会话列表项。
    /// </summary>
    public SessionItemViewModel(SessionSummary summary)
    {
        SessionId = summary.SessionId;
        Title = summary.Title;
        CreatedAt = summary.CreatedAt;
        RoleCount = summary.RoleCount;
        MessageCount = summary.MessageCount;
    }
}

/// <summary>
/// 左栏会话列表 ViewModel。
/// </summary>
public sealed class SessionListViewModel : ViewModelBase
{
    private readonly ChatRoomService _chatRoomService;
    private readonly SessionService _sessionService;
    private SessionItemViewModel? _selectedSession;

    /// <summary>
    /// 会话列表。
    /// </summary>
    public ObservableCollection<SessionItemViewModel> Sessions { get; } = [];

    /// <summary>
    /// 当前选中的会话。
    /// </summary>
    public SessionItemViewModel? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetField(ref _selectedSession, value) && value is not null)
            {
                _ = OpenSessionAsync(value);
            }
        }
    }

    /// <summary>
    /// 新建会话命令。
    /// </summary>
    public ICommand CreateNewSessionCommand { get; }

    /// <summary>
    /// 打开会话命令。
    /// </summary>
    public ICommand OpenSessionCommand { get; }

    /// <summary>
    /// 删除会话命令。
    /// </summary>
    public ICommand DeleteSessionCommand { get; }

    /// <summary>
    /// 打开设置命令。
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    /// <summary>
    /// 打开角色大厅命令。
    /// </summary>
    public ICommand OpenLobbyCommand { get; }

    /// <summary>
    /// 会话打开事件。参数为打开的会话 ID。
    /// </summary>
    public event EventHandler<string>? SessionOpened;

    /// <summary>
    /// 新建会话事件。
    /// </summary>
    public event EventHandler? NewSessionCreated;

    /// <summary>
    /// 打开设置事件。
    /// </summary>
    public event EventHandler? SettingsRequested;

    /// <summary>
    /// 打开角色大厅事件。
    /// </summary>
    public event EventHandler? OpenLobbyRequested;

    /// <summary>
    /// 使用指定的服务创建会话列表 ViewModel。
    /// </summary>
    public SessionListViewModel(ChatRoomService chatRoomService, SessionService sessionService)
    {
        _chatRoomService = chatRoomService;
        _sessionService = sessionService;

        CreateNewSessionCommand = new SimpleAsyncCommand(CreateNewSessionAsync);
        OpenSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(OpenSessionAsync);
        DeleteSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(DeleteSessionAsync, CanDeleteSession);
        OpenSettingsCommand = new SimpleCommand(() => SettingsRequested?.Invoke(this, EventArgs.Empty));
        OpenLobbyCommand = new SimpleCommand(() => OpenLobbyRequested?.Invoke(this, EventArgs.Empty));

        _chatRoomService.SessionChanged += OnSessionChanged;
        RefreshSessions();
    }

    private void OnSessionChanged(object? sender, ChatRoomManager? manager)
    {
        // 会话切换后刷新删除命令的可用状态（当前活跃会话不可删除）
        if (DeleteSessionCommand is SimpleAsyncCommand<SessionItemViewModel> cmd)
        {
            cmd.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 判断指定会话是否可删除。当前活跃会话不允许删除。
    /// </summary>
    private bool CanDeleteSession(SessionItemViewModel? session)
    {
        if (session is null)
        {
            return false;
        }

        Guid? activeSessionId = _chatRoomService.CurrentManager?.Session.SessionId;
        return activeSessionId is null ||
            activeSessionId.Value.ToString("N") != session.SessionId;
    }

    /// <summary>
    /// 刷新会话列表。
    /// </summary>
    public void RefreshSessions()
    {
        Sessions.Clear();

        foreach (SessionSummary summary in _sessionService.ListSessions())
        {
            Sessions.Add(new SessionItemViewModel(summary));
        }
    }

    private async Task CreateNewSessionAsync()
    {
        IsBusy = true;
        try
        {
            await _chatRoomService.CreateNewSessionAsync().ConfigureAwait(false);
            NewSessionCreated?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenSessionAsync(SessionItemViewModel? session)
    {
        if (session is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _chatRoomService.LoadSessionAsync(session.SessionId).ConfigureAwait(false);
            SelectedSession = session;
            SessionOpened?.Invoke(this, session.SessionId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task DeleteSessionAsync(SessionItemViewModel? session)
    {
        if (session is null)
        {
            return Task.CompletedTask;
        }

        // 禁止删除当前活跃会话
        Guid? activeSessionId = _chatRoomService.CurrentManager?.Session.SessionId;
        if (activeSessionId is not null &&
            activeSessionId.Value.ToString("N") == session.SessionId)
        {
            return Task.CompletedTask;
        }

        _sessionService.DeleteSession(session.SessionId);
        Sessions.Remove(session);

        if (SelectedSession == session)
        {
            SelectedSession = null;
        }

        return Task.CompletedTask;
    }
}
