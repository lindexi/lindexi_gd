using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>组合独立工作任务与常驻导航。</summary>
public sealed class MainViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly SettingsViewModel? _settingsViewModel;
    private readonly WorkTaskStore? _workTaskStore;
    private readonly Func<Task<CodingChatRuntime>>? _createRuntimeAsync;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private WorkTaskItemViewModel _activeWorkTask;
    private string? _errorMessage;
    private bool _isSettingsOpen;
    private bool _isHistoryOpen;
    private bool _isDisposed;

    /// <summary>创建未连接模型的设计期界面。</summary>
    public MainViewModel()
        : this([CreatePlaceholderTask()], null, null, null)
    {
    }

    private MainViewModel(
        IReadOnlyList<WorkTaskItemViewModel> tasks,
        CodingChatSettingsService? settingsService,
        WorkTaskStore? workTaskStore,
        Func<Task<CodingChatRuntime>>? createRuntimeAsync)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        if (tasks.Count == 0)
        {
            throw new ArgumentException("至少需要一个工作任务。", nameof(tasks));
        }

        _settingsViewModel = settingsService is null
            ? null
            : new SettingsViewModel(settingsService, () => IsSettingsOpen = false);
        _workTaskStore = workTaskStore;
        _createRuntimeAsync = createRuntimeAsync;
        _activeWorkTask = tasks[0];
        _activeWorkTask.IsActive = true;

        foreach (WorkTaskItemViewModel task in tasks)
        {
            WorkTasks.Add(task);
            Subscribe(task);
        }

        OpenSettingsCommand = new SimpleAsyncCommand(OpenSettingsAsync, () => _settingsViewModel is not null);
        OpenHistoryCommand = new SimpleAsyncCommand(() => OpenHistoryAsync(false), allowConcurrentExecutions: true);
        OpenTaskHistoryCommand = new SimpleAsyncCommand(() => OpenHistoryAsync(true), allowConcurrentExecutions: true);
        CloseHistoryCommand = new SimpleCommand(() => { IsHistoryOpen = false; IsSettingsOpen = false; });
        OpenSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>(OpenSessionAsync,
            item => item is not null && SessionListViewModel.CanChangeSession);
        CreateWorkTaskCommand = new SimpleAsyncCommand(CreateWorkTaskAsync, () => _createRuntimeAsync is not null);
        ActivateWorkTaskCommand = new SimpleCommand<WorkTaskItemViewModel>(task => { if (task is not null) Activate(task); });
        RenameWorkTaskCommand = new SimpleCommand<WorkTaskItemViewModel>(StartRenamingTask);
        SaveWorkTaskNameCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(SaveTaskNameAsync,
            task => task is not null && !string.IsNullOrWhiteSpace(task.EditedDisplayName));
        DeleteWorkTaskCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(DeleteTaskAsync,
            task => task is not null && !task.IsWorking && WorkTasks.Count > 1);
    }

    internal static MainViewModel Create(
        IReadOnlyList<WorkTaskItemViewModel> tasks,
        CodingChatSettingsService settingsService,
        WorkTaskStore workTaskStore,
        Func<Task<CodingChatRuntime>> createRuntimeAsync)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(workTaskStore);
        ArgumentNullException.ThrowIfNull(createRuntimeAsync);
        return new MainViewModel(tasks, settingsService, workTaskStore, createRuntimeAsync);
    }

    internal static MainViewModel CreateForTests(
        SessionListViewModel sessions,
        ChatViewModel chat,
        CodingChatSettingsService? settingsService = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(chat);
        return new MainViewModel(
            [new WorkTaskItemViewModel(GetTaskName(1), chat, sessions)],
            settingsService,
            null,
            null);
    }

    /// <summary>获取工作任务列表。</summary>
    public ObservableCollection<WorkTaskItemViewModel> WorkTasks { get; } = [];
    /// <summary>获取当前任务。</summary>
    public WorkTaskItemViewModel ActiveWorkTask => _activeWorkTask;
    /// <summary>获取当前任务聊天。</summary>
    public ChatViewModel ChatViewModel => _activeWorkTask.Chat;
    /// <summary>获取当前历史页面。</summary>
    public SessionListViewModel SessionListViewModel => _activeWorkTask.Sessions;
    /// <summary>获取全局设置。</summary>
    public SettingsViewModel? SettingsViewModel => _settingsViewModel;
    /// <summary>获取任务操作错误。</summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetField(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }
    }
    /// <summary>获取是否存在任务操作错误。</summary>
    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);
    /// <summary>获取聊天页可见性。</summary>
    public bool IsChatOpen => !IsHistoryOpen && !IsSettingsOpen;
    /// <summary>获取历史页可见性。</summary>
    public bool IsHistoryOpen { get => _isHistoryOpen; private set { if (SetField(ref _isHistoryOpen, value)) OnPropertyChanged(nameof(IsChatOpen)); } }
    /// <summary>获取设置页可见性。</summary>
    public bool IsSettingsOpen { get => _isSettingsOpen; private set { if (SetField(ref _isSettingsOpen, value)) OnPropertyChanged(nameof(IsChatOpen)); } }
    /// <summary>新建独立任务。</summary>
    public ICommand CreateWorkTaskCommand { get; }
    /// <summary>切换任务而不停止后台执行。</summary>
    public ICommand ActivateWorkTaskCommand { get; }
    /// <summary>编辑任务名。</summary>
    public ICommand RenameWorkTaskCommand { get; }
    /// <summary>确认任务名称并退出编辑。</summary>
    public ICommand SaveWorkTaskNameCommand { get; }
    /// <summary>删除空闲任务，不删除历史文件。</summary>
    public ICommand DeleteWorkTaskCommand { get; }
    /// <summary>打开全部历史。</summary>
    public ICommand OpenHistoryCommand { get; }
    /// <summary>按任务路径打开历史。</summary>
    public ICommand OpenTaskHistoryCommand { get; }
    /// <summary>返回聊天。</summary>
    public ICommand CloseHistoryCommand { get; }
    /// <summary>在当前工作任务中打开历史会话。</summary>
    public ICommand OpenSessionCommand { get; }
    /// <summary>打开设置。</summary>
    public ICommand OpenSettingsCommand { get; }

    internal static WorkTaskItemViewModel CreateRuntimeTask(CodingChatRuntime runtime, WorkTaskRecord record)
        => new(
            record.DisplayName,
            new ChatViewModel(runtime.ChatManager, runtime.Application, runtime.WorkspaceController, runtime.ModelDisplayName),
            new SessionListViewModel(runtime.Application),
            runtime,
            record.Id);

    private static WorkTaskItemViewModel CreatePlaceholderTask()
        => new(GetTaskName(1), new ChatViewModel(), new SessionListViewModel());

    private static string GetTaskName(int number)
        => $"{Avalonia.Application.Current?.FindResource("WorkTaskText") ?? "工作任务"} {number}";

    private static void StartRenamingTask(WorkTaskItemViewModel? task)
    {
        if (task is null) return;
        task.EditedDisplayName = task.DisplayName;
        task.IsEditing = true;
    }

    private void Subscribe(WorkTaskItemViewModel task)
    {
        task.PropertyChanged += OnTaskPropertyChanged;
    }

    private void Unsubscribe(WorkTaskItemViewModel task)
    {
        task.PropertyChanged -= OnTaskPropertyChanged;
        task.Detach();
    }

    private async void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        (DeleteWorkTaskCommand as SimpleAsyncCommand<WorkTaskItemViewModel>)?.RaiseCanExecuteChanged();
        if (e.PropertyName == nameof(WorkTaskItemViewModel.EditedDisplayName))
        {
            (SaveWorkTaskNameCommand as SimpleAsyncCommand<WorkTaskItemViewModel>)?.RaiseCanExecuteChanged();
        }

        if (e.PropertyName is nameof(WorkTaskItemViewModel.DisplayName)
            or nameof(ChatViewModel.NextRunWorkspacePath)
            or nameof(ChatViewModel.SelectedModel)
            or nameof(ChatViewModel.SelectedReasoningEffort))
        {
            await SaveTasksAndReportAsync().ConfigureAwait(true);
        }
    }

    private void Activate(WorkTaskItemViewModel task)
    {
        _activeWorkTask.IsActive = false;
        _activeWorkTask = task;
        task.IsActive = true;
        IsHistoryOpen = false;
        IsSettingsOpen = false;
        OnPropertyChanged(nameof(ActiveWorkTask));
        OnPropertyChanged(nameof(ChatViewModel));
        OnPropertyChanged(nameof(SessionListViewModel));
        (OpenSessionCommand as SimpleAsyncCommand<SessionItemViewModel>)?.RaiseCanExecuteChanged();
    }

    private async Task OpenSessionAsync(SessionItemViewModel? item)
    {
        WorkTaskItemViewModel activeTask = ActiveWorkTask;
        if (await activeTask.Sessions.OpenSessionAsync(item).ConfigureAwait(true))
        {
            IsHistoryOpen = false;
            IsSettingsOpen = false;
        }
    }

    private async Task CreateWorkTaskAsync()
    {
        if (_createRuntimeAsync is null) return;
        ErrorMessage = null;
        CodingChatRuntime? runtime = null;
        WorkTaskItemViewModel? task = null;
        try
        {
            runtime = await _createRuntimeAsync().ConfigureAwait(true);
            var record = new WorkTaskRecord(
                Guid.NewGuid(),
                GetTaskName(WorkTasks.Count + 1),
                null,
                runtime.ModelDisplayName,
                null);
            task = CreateRuntimeTask(runtime, record);
            WorkTasks.Add(task);
            Subscribe(task);
            await SaveTasksAsync().ConfigureAwait(true);
            Activate(task);
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            if (task is not null && WorkTasks.Contains(task))
            {
                Unsubscribe(task);
                task.Chat.Dispose();
                task.Sessions.Dispose();
                WorkTasks.Remove(task);
            }

            if (runtime is not null)
            {
                await runtime.DisposeAsync().ConfigureAwait(true);
            }

            ReportError($"创建工作任务失败：{exception.Message}", exception);
        }
    }

    private async Task SaveTaskNameAsync(WorkTaskItemViewModel? task)
    {
        if (task is null || string.IsNullOrWhiteSpace(task.EditedDisplayName)) return;
        string previousName = task.DisplayName;
        task.PropertyChanged -= OnTaskPropertyChanged;
        task.DisplayName = task.EditedDisplayName;
        try
        {
            await SaveTasksAsync().ConfigureAwait(true);
            task.IsEditing = false;
            ErrorMessage = null;
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            task.DisplayName = previousName;
            ReportError($"保存任务名称失败：{exception.Message}", exception);
        }
        finally
        {
            task.PropertyChanged += OnTaskPropertyChanged;
        }
    }

    private async Task DeleteTaskAsync(WorkTaskItemViewModel? task)
    {
        if (task is null || task.IsWorking || WorkTasks.Count <= 1) return;
        ErrorMessage = null;
        try
        {
            await SaveTasksAsync(WorkTasks.Where(item => !ReferenceEquals(item, task))).ConfigureAwait(true);
            if (task.Runtime is not null)
            {
                await task.Runtime.DisposeAsync().ConfigureAwait(true);
            }

            Unsubscribe(task);
            task.Chat.Dispose();
            task.Sessions.Dispose();
            WorkTasks.Remove(task);
            if (ReferenceEquals(task, _activeWorkTask)) Activate(WorkTasks[0]);
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            ReportError($"删除工作任务失败：{exception.Message}", exception);
        }
    }

    private async Task OpenHistoryAsync(bool filterByPath)
    {
        SessionListViewModel sessions = SessionListViewModel;
        sessions.SearchText = filterByPath ? ChatViewModel.NextRunWorkspacePath ?? string.Empty : string.Empty;
        IsSettingsOpen = false;
        IsHistoryOpen = true;
        await sessions.LoadAsync().ConfigureAwait(true);
    }

    private async Task OpenSettingsAsync()
    {
        if (_settingsViewModel is null) return;
        IsHistoryOpen = false;
        IsSettingsOpen = true;
        await _settingsViewModel.LoadAsync().ConfigureAwait(true);
    }

    private async Task SaveTasksAndReportAsync()
    {
        try
        {
            await SaveTasksAsync().ConfigureAwait(true);
            ErrorMessage = null;
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            ReportError($"保存工作任务失败：{exception.Message}", exception);
        }
    }

    private Task SaveTasksAsync(IEnumerable<WorkTaskItemViewModel>? tasks = null)
        => SaveTasksCoreAsync((tasks ?? WorkTasks).ToArray());

    private async Task SaveTasksCoreAsync(IReadOnlyList<WorkTaskItemViewModel> tasks)
    {
        if (_workTaskStore is null) return;
        await _saveGate.WaitAsync().ConfigureAwait(true);
        try
        {
            WorkTaskRecord[] records = tasks.Select(task => new WorkTaskRecord(
                task.Id,
                task.DisplayName,
                task.Chat.NextRunWorkspacePath,
                task.Chat.SelectedModel?.DisplayName,
                task.Chat.SelectedReasoningEffort?.Value)).ToArray();
            await _workTaskStore.SaveAsync(records).ConfigureAwait(true);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private void ReportError(string message, Exception exception)
    {
        Trace.TraceError(exception.ToString());
        ErrorMessage = message;
    }

    private static bool IsExpectedOperationException(Exception exception)
        => exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or JsonException;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        foreach (WorkTaskItemViewModel task in WorkTasks)
        {
            Unsubscribe(task);
            task.Chat.Dispose();
            task.Sessions.Dispose();
            if (task.Runtime is not null)
            {
                await task.Runtime.DisposeAsync().ConfigureAwait(true);
            }
        }

        _saveGate.Dispose();
    }
}
