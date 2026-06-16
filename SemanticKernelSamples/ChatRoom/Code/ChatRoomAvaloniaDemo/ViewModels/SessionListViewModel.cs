using AgentLib.Model;

using ChatRoomAvaloniaDemo.Services;

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 历史会话列表 ViewModel。管理已持久化的会话列表的加载、新建、打开和删除操作。
/// </summary>
public sealed class SessionListViewModel : NotifyBase
{
    private readonly ChatRoomService _chatRoomService;
    private SessionSummary? _selectedSession;
    private bool _isLoading;

    /// <summary>
    /// 创建会话列表 ViewModel。
    /// </summary>
    /// <param name="chatRoomService">聊天室服务。</param>
    public SessionListViewModel(ChatRoomService chatRoomService)
    {
        _chatRoomService = chatRoomService ?? throw new ArgumentNullException(nameof(chatRoomService));

        NewSessionCommand = new DelegateCommand(NewSession);
        OpenSessionCommand = new DelegateCommand<SessionSummary?>(OpenSession);
        DeleteSessionCommand = new DelegateCommand<SessionSummary?>(DeleteSession);
    }

    /// <summary>
    /// 历史会话列表。
    /// </summary>
    public ObservableCollection<SessionSummary> Sessions { get; } = [];

    /// <summary>
    /// 当前选中的会话。
    /// </summary>
    public SessionSummary? SelectedSession
    {
        get => _selectedSession;
        set => SetField(ref _selectedSession, value);
    }

    /// <summary>
    /// 是否正在加载。
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>新建会话命令。</summary>
    public DelegateCommand NewSessionCommand { get; }

    /// <summary>打开选中的会话命令。</summary>
    public DelegateCommand<SessionSummary?> OpenSessionCommand { get; }

    /// <summary>删除选中的会话命令。</summary>
    public DelegateCommand<SessionSummary?> DeleteSessionCommand { get; }

    /// <summary>
    /// 会话已加载事件。当用户选择打开一个历史会话时触发。
    /// </summary>
    public event EventHandler<SessionSummary>? SessionLoaded;

    /// <summary>
    /// 从持久化加载历史会话列表。
    /// </summary>
    public async Task LoadSessionsAsync()
    {
        IsLoading = true;
        try
        {
            Sessions.Clear();
            var summaries = await _chatRoomService.ListSessionSummariesAsync();

            foreach (var summary in summaries)
            {
                Sessions.Add(summary);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NewSession()
    {
        _chatRoomService.CloseSession();
        _chatRoomService.CreateNewSession();
        SessionLoaded?.Invoke(this, new SessionSummary());
    }

    private async void OpenSession(SessionSummary? session)
    {
        if (session is null || string.IsNullOrEmpty(session.SessionId))
        {
            return;
        }

        _chatRoomService.CloseSession();
        var manager = await _chatRoomService.LoadSessionAsync(session.SessionId);
        if (manager is not null)
        {
            SessionLoaded?.Invoke(this, session);
        }
    }

    private void DeleteSession(SessionSummary? session)
    {
        if (session is null || string.IsNullOrEmpty(session.SessionId))
        {
            return;
        }

        _chatRoomService.DeleteSession(session.SessionId);
        Sessions.Remove(session);
    }
}