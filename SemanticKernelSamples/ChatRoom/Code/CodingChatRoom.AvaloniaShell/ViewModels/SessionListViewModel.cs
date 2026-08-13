using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib.Logging;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 表示左侧历史会话列表。
/// </summary>
public sealed class SessionListViewModel : ViewModelBase
{
    private readonly CodingChatApplication? _application;
    private readonly SimpleAsyncCommand _createNewSessionCommand;
    private readonly SimpleAsyncCommand<SessionItemViewModel> _openSessionCommand;
    private readonly SimpleAsyncCommand<SessionItemViewModel> _deleteSessionCommand;
    private SessionItemViewModel? _selectedSession;

    /// <summary>
    /// 初始化空会话列表骨架。
    /// </summary>
    public SessionListViewModel()
    {
        _createNewSessionCommand = new SimpleAsyncCommand(static () => Task.CompletedTask, static () => false);
        _openSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(static _ => Task.CompletedTask, static _ => false);
        _deleteSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(static _ => Task.CompletedTask, static _ => false);
    }

    internal SessionListViewModel(CodingChatApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _application = application;
        _createNewSessionCommand = new SimpleAsyncCommand(CreateNewSessionAsync, () => CanChangeSession);
        _openSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(OpenSessionAsync, CanExecuteSessionCommand);
        _deleteSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(DeleteSessionAsync, CanExecuteSessionCommand);
        _application.Sessions.CollectionChanged += OnSessionsCollectionChanged;
        _application.StateChanged += OnApplicationStateChanged;
        Refresh();
    }

    /// <summary>
    /// 获取会话列表项。
    /// </summary>
    public ObservableCollection<SessionItemViewModel> Sessions { get; } = [];

    /// <summary>
    /// 获取当前是否没有可显示的历史会话。
    /// </summary>
    public bool IsEmpty => Sessions.Count == 0;

    public SessionItemViewModel? SelectedSession
    {
        get => _selectedSession;
        private set => SetField(ref _selectedSession, value);
    }

    public bool CanChangeSession => _application?.CanChangeSession ?? false;

    /// <summary>
    /// 获取新建会话命令。
    /// </summary>
    public ICommand CreateNewSessionCommand => _createNewSessionCommand;

    public ICommand OpenSessionCommand => _openSessionCommand;

    public ICommand DeleteSessionCommand => _deleteSessionCommand;

    private async Task CreateNewSessionAsync()
    {
        await _application!.CreateNewSessionAsync().ConfigureAwait(true);
        Refresh();
    }

    private async Task OpenSessionAsync(SessionItemViewModel? session)
    {
        if (session is null)
        {
            return;
        }

        SessionItemViewModel? previousSelection = SelectedSession;
        try
        {
            await _application!.OpenSessionAsync(session.SessionId).ConfigureAwait(true);
            Refresh();
        }
        catch
        {
            SelectedSession = previousSelection;
            throw;
        }
    }

    private async Task DeleteSessionAsync(SessionItemViewModel? session)
    {
        if (session is null)
        {
            return;
        }

        await _application!.DeleteSessionAsync(session.SessionId).ConfigureAwait(true);
        Refresh();
    }

    private bool CanExecuteSessionCommand(SessionItemViewModel? session)
        => session is not null && CanChangeSession;

    private void OnSessionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            int index = e.NewStartingIndex;
            foreach (CopilotChatSessionSummary summary in e.NewItems)
            {
                Sessions.Insert(index++, new SessionItemViewModel(summary));
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (CopilotChatSessionSummary summary in e.OldItems)
            {
                SessionItemViewModel? item = Sessions.FirstOrDefault(candidate => candidate.SessionId == summary.SessionId);
                if (item is not null)
                {
                    Sessions.Remove(item);
                }
            }
        }
        else
        {
            Refresh();
            return;
        }

        UpdateState();
    }

    private void OnApplicationStateChanged(object? sender, EventArgs e)
    {
        UpdateState();
    }

    private void Refresh()
    {
        if (_application is null)
        {
            return;
        }

        Sessions.Clear();
        foreach (CopilotChatSessionSummary summary in _application.Sessions)
        {
            Sessions.Add(new SessionItemViewModel(summary));
        }

        UpdateState();
    }

    private void UpdateState()
    {
        SelectedSession = Sessions.FirstOrDefault(item => item.SessionId == _application?.SelectedSessionId);
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CanChangeSession));
        _createNewSessionCommand.RaiseCanExecuteChanged();
        _openSessionCommand.RaiseCanExecuteChanged();
        _deleteSessionCommand.RaiseCanExecuteChanged();
    }
}

/// <summary>
/// 表示历史会话的只读摘要。
/// </summary>
public sealed class SessionItemViewModel : ViewModelBase
{
    internal SessionItemViewModel(CopilotChatSessionSummary summary)
    {
        SessionId = summary.SessionId;
        Title = summary.Title;
        StartedTime = summary.StartedTime;
        MessageCount = summary.MessageCount;
    }

    public Guid SessionId { get; }

    /// <summary>
    /// 获取会话标题。
    /// </summary>
    public string Title { get; }

    public DateTimeOffset StartedTime { get; }

    public int MessageCount { get; }

    /// <summary>
    /// 获取消息数与活动时间摘要。
    /// </summary>
    public string Subtitle => string.Create(
        CultureInfo.CurrentCulture,
        $"{MessageCount} 条消息 · {StartedTime:MM-dd HH:mm}");
}
