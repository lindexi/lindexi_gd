using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    /// 是否选中了当前活跃会话。
    /// </summary>
    public bool IsCurrentSessionSelected => SelectedSession is not null && IsCurrentSession(SelectedSession);

    /// <summary>
    /// 当前选中的会话。
    /// </summary>
    public SessionItemViewModel? SelectedSession
    {
        get => _selectedSession;
        set
        {
            bool wasCurrentSessionSelected = IsCurrentSessionSelected;
            if (SetField(ref _selectedSession, value) && value is not null)
            {
                _ = OpenSessionAsync(value);
            }

            if (wasCurrentSessionSelected != IsCurrentSessionSelected)
            {
                OnPropertyChanged(nameof(IsCurrentSessionSelected));
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
    }

    private void OnSessionChanged(object? sender, ChatRoomManager? manager)
    {
        SelectCurrentSession();

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
    /// <returns>表示异步刷新操作的任务。</returns>
    public async Task RefreshSessionsAsync()
    {
        string? selectedSessionId = SelectedSession?.SessionId;
        Sessions.Clear();

        IReadOnlyList<SessionSummary> summaries = await _sessionService.ListSessionsAsync().ConfigureAwait(true);
        foreach (SessionSummary summary in summaries)
        {
            Sessions.Add(new SessionItemViewModel(summary));
        }

        ChatRoomManager? currentManager = _chatRoomService.CurrentManager;
        if (currentManager is not null && !Sessions.Any(session => IsCurrentSession(session)))
        {
            Sessions.Insert(0, CreateSessionItem(currentManager));
        }

        SelectedSession = Sessions.FirstOrDefault(session => session.SessionId == selectedSessionId)
            ?? Sessions.FirstOrDefault(session => IsCurrentSession(session));
    }

    /// <summary>
    /// 选中并打开当前活跃会话。
    /// </summary>
    public void OpenCurrentSession()
    {
        AddOrSelectCurrentSession();
        if (SelectedSession is not null)
        {
            SessionOpened?.Invoke(this, SelectedSession.SessionId);
        }
    }

    private async Task CreateNewSessionAsync()
    {
        IsBusy = true;
        try
        {
            if (TrySelectBlankSession())
            {
                SessionOpened?.Invoke(this, SelectedSession!.SessionId);
                return;
            }

            await _chatRoomService.CreateNewSessionAsync().ConfigureAwait(false);
            AddOrSelectCurrentSession();
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

        if (IsCurrentSession(session))
        {
            SessionOpened?.Invoke(this, session.SessionId);
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

    private bool TrySelectBlankSession()
    {
        ChatRoomManager? currentManager = _chatRoomService.CurrentManager;
        if (currentManager?.Session.Messages.Count != 0)
        {
            return false;
        }

        AddOrSelectCurrentSession();
        return SelectedSession is not null;
    }

    private void AddOrSelectCurrentSession()
    {
        ChatRoomManager? currentManager = _chatRoomService.CurrentManager;
        if (currentManager is null)
        {
            return;
        }

        SessionItemViewModel? session = Sessions.FirstOrDefault(IsCurrentSession);
        if (session is null)
        {
            session = CreateSessionItem(currentManager);
            Sessions.Insert(0, session);
        }

        SelectedSession = session;
    }

    private void SelectCurrentSession()
    {
        ChatRoomManager? currentManager = _chatRoomService.CurrentManager;
        if (currentManager is null)
        {
            return;
        }

        SessionItemViewModel? session = Sessions.FirstOrDefault(IsCurrentSession);
        if (session is not null)
        {
            SelectedSession = session;
        }
    }

    private bool IsCurrentSession(SessionItemViewModel session)
    {
        Guid? activeSessionId = _chatRoomService.CurrentManager?.Session.SessionId;
        return activeSessionId is not null && activeSessionId.Value.ToString("N") == session.SessionId;
    }

    private static SessionItemViewModel CreateSessionItem(ChatRoomManager manager)
    {
        return new SessionItemViewModel(new SessionSummary
        {
            SessionId = manager.Session.SessionId.ToString("N"),
            Title = manager.Session.Title,
            CreatedAt = manager.Session.CreatedAt,
            RoleCount = manager.Roles.Count,
            MessageCount = manager.Session.Messages.Count,
        });
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
