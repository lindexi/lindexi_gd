using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AgentLib.Logging;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 提供按需加载、搜索和管理历史会话的页面。
/// </summary>
public sealed class SessionListViewModel : ViewModelBase, IDisposable
{
    private readonly CodingChatApplication? _application;
    private readonly NotifyCollectionChangedEventHandler? _sessionsChangedHandler;
    private readonly EventHandler? _stateChangedHandler;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isLoading;
    private bool _hasLoaded;
    private readonly System.Collections.Generic.Dictionary<Guid, SessionItemViewModel> _items = [];
    private bool _isOperating;
    private readonly SimpleAsyncCommand _createNewSessionCommand;
    private readonly SimpleAsyncCommand<SessionItemViewModel> _openSessionCommand;
    private readonly SimpleAsyncCommand<SessionItemViewModel> _deleteSessionCommand;
    private readonly SimpleAsyncCommand<SessionItemViewModel> _saveTitleCommand;

    /// <summary>
    /// 初始化空页面。
    /// </summary>
    public SessionListViewModel()
    {
        _createNewSessionCommand = new SimpleAsyncCommand(() => RunOperationAsync(async () =>
        {
            if (_application is null) return;
            await _application.CreateNewSessionAsync();
            SessionOpened?.Invoke(this, EventArgs.Empty);
        }), () => CanChangeSession);
        _openSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(item => RunOperationAsync(async () =>
        {
            if (_application is null || item is null) return;
            await _application.OpenSessionAsync(item.SessionId);
            SessionOpened?.Invoke(this, EventArgs.Empty);
        }), CanExecute);
        _deleteSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(item => RunOperationAsync(async () =>
        {
            if (_application is not null && item is not null)
                await _application.DeleteSessionAsync(item.SessionId);
        }), CanExecute);
        _saveTitleCommand = new SimpleAsyncCommand<SessionItemViewModel>(item => RunOperationAsync(async () =>
        {
            if (_application is not null && item is not null && !string.IsNullOrWhiteSpace(item.EditedTitle))
            {
                await _application.RenameSessionAsync(item.SessionId, item.EditedTitle);
                item.IsEditing = false;
            }
        }), CanExecute);
        EditTitleCommand = new SimpleCommand<SessionItemViewModel>(item =>
        {
            if (item is null) return;
            item.EditedTitle = item.Title;
            item.IsEditing = true;
        }, CanExecute);
        CancelEditCommand = new SimpleCommand<SessionItemViewModel>(item =>
        {
            if (item is not null) item.IsEditing = false;
        });
        ReloadCommand = new SimpleAsyncCommand(() => LoadCoreAsync(), () => !IsLoading);
    }

    internal SessionListViewModel(CodingChatApplication application) : this()
    {
        ArgumentNullException.ThrowIfNull(application);
        _application = application;
        _sessionsChangedHandler = (_, _) => Refresh();
        _stateChangedHandler = (_, _) => UpdateState();
        application.Sessions.CollectionChanged += _sessionsChangedHandler;
        application.StateChanged += _stateChangedHandler;
        Refresh();
    }

    internal event EventHandler? SessionOpened;

    public ObservableCollection<SessionItemViewModel> Sessions { get; } = [];
    public bool IsEmpty => !IsLoading && Sessions.Count == 0;
    public bool CanChangeSession => (_application?.CanChangeSession ?? false) && !_isOperating;
    public SessionItemViewModel? SelectedSession => Sessions.FirstOrDefault(item => item.SessionId == _application?.SelectedSessionId);
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) Refresh(); }
    }
    public bool IsLoading
    {
        get => _isLoading;
        private set { if (SetField(ref _isLoading, value)) UpdateState(); }
    }
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }
    public ICommand CreateNewSessionCommand => _createNewSessionCommand;
    public ICommand OpenSessionCommand => _openSessionCommand;
    public ICommand DeleteSessionCommand => _deleteSessionCommand;
    public ICommand SaveTitleCommand => _saveTitleCommand;
    public ICommand EditTitleCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand ReloadCommand { get; }

    /// <summary>
    /// 进入页面时在后台读取历史摘要。
    /// </summary>
    public Task LoadAsync() => _hasLoaded ? Task.CompletedTask : LoadCoreAsync();

    private async Task LoadCoreAsync()
    {
        if (_application is null || IsLoading) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await _application.InitializeAsync();
            _hasLoaded = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException or System.Xml.XmlException)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RunOperationAsync(Func<Task> operation)
    {
        _isOperating = true;
        ErrorMessage = null;
        UpdateState();
        try
        {
            await operation();
            Refresh();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException or System.Xml.XmlException)
        {
            Trace.TraceError(exception.ToString());
            ErrorMessage = exception.Message;
        }
        finally
        {
            _isOperating = false;
            UpdateState();
        }
    }

    private bool CanExecute(SessionItemViewModel? item) => item is not null && CanChangeSession;

    private void Refresh()
    {
        if (_application is null) return;
        string query = SearchText.Trim();
        var activeIds = _application.Sessions.Select(summary => summary.SessionId).ToHashSet();
        foreach (Guid id in _items.Keys.Where(id => !activeIds.Contains(id)).ToArray()) _items.Remove(id);
        var visible = new System.Collections.Generic.List<SessionItemViewModel>();
        foreach (CopilotChatSessionSummary summary in _application.Sessions)
        {
            if (!_items.TryGetValue(summary.SessionId, out var item))
            {
                item = new SessionItemViewModel(summary);
                _items.Add(summary.SessionId, item);
            }
            else
            {
                item.Update(summary);
            }
            if (query.Length == 0 || summary.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (summary.WorkspacePath?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                visible.Add(item);
        }
        var visibleIds = visible.Select(item => item.SessionId).ToHashSet();
        for (int index = Sessions.Count - 1; index >= 0; index--)
            if (!visibleIds.Contains(Sessions[index].SessionId)) Sessions.RemoveAt(index);
        for (int index = 0; index < visible.Count; index++)
        {
            var item = visible[index];
            int previousIndex = Sessions.IndexOf(item);
            if (previousIndex < 0) Sessions.Insert(index, item);
            else if (previousIndex != index) Sessions.Move(previousIndex, index);
        }
        UpdateState();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_application is null) return;
        if (_sessionsChangedHandler is not null)
        {
            _application.Sessions.CollectionChanged -= _sessionsChangedHandler;
        }

        if (_stateChangedHandler is not null)
        {
            _application.StateChanged -= _stateChangedHandler;
        }
    }

    private void UpdateState()
    {
        foreach (SessionItemViewModel item in _items.Values)
        {
            item.IsCurrent = item.SessionId == _application?.SelectedSessionId;
        }
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CanChangeSession));
        OnPropertyChanged(nameof(SelectedSession));
        _createNewSessionCommand.RaiseCanExecuteChanged();
        _openSessionCommand.RaiseCanExecuteChanged();
        _deleteSessionCommand.RaiseCanExecuteChanged();
        _saveTitleCommand.RaiseCanExecuteChanged();
        (EditTitleCommand as SimpleCommand<SessionItemViewModel>)?.RaiseCanExecuteChanged();
        (ReloadCommand as SimpleAsyncCommand)?.RaiseCanExecuteChanged();
    }
}

/// <summary>
/// 表示历史会话摘要及标题编辑状态。
/// </summary>
public sealed class SessionItemViewModel : ViewModelBase
{
    private string _editedTitle;
    private bool _isEditing;
    private bool _isCurrent;

    /// <summary>
    /// 获取此条目是否为当前聊天会话，与列表焦点或选中状态无关。
    /// </summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        internal set => SetField(ref _isCurrent, value);
    }

    internal SessionItemViewModel(CopilotChatSessionSummary summary)
    {
        SessionId = summary.SessionId;
        Title = summary.Title;
        _editedTitle = Title;
        WorkspacePath = summary.WorkspacePath;
        StartedTime = summary.StartedTime;
        MessageCount = summary.MessageCount;
    }

    public Guid SessionId { get; }
    public string Title { get; private set; }
    public string? WorkspacePath { get; private set; }
    public DateTimeOffset StartedTime { get; }
    public int MessageCount { get; private set; }

    internal void Update(CopilotChatSessionSummary summary)
    {
        if (Title != summary.Title)
        {
            Title = summary.Title;
            OnPropertyChanged(nameof(Title));
            if (!IsEditing) EditedTitle = Title;
        }
        if (WorkspacePath != summary.WorkspacePath)
        {
            WorkspacePath = summary.WorkspacePath;
            OnPropertyChanged(nameof(WorkspacePath));
        }
        if (MessageCount != summary.MessageCount)
        {
            MessageCount = summary.MessageCount;
            OnPropertyChanged(nameof(MessageCount));
            OnPropertyChanged(nameof(Subtitle));
        }
    }
    public string EditedTitle { get => _editedTitle; set => SetField(ref _editedTitle, value); }
    public bool IsEditing { get => _isEditing; set => SetField(ref _isEditing, value); }
    public string Subtitle => string.Create(CultureInfo.CurrentCulture, $"{MessageCount} 条消息 · {StartedTime:MM-dd HH:mm}");
}
