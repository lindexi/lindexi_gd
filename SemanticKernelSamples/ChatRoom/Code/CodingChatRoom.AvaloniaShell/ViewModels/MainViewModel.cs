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
using CodingChatRoom.AvaloniaShell.Abilities;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>组合独立工作任务与常驻导航。</summary>
public sealed class MainViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly SettingsViewModel? _settingsViewModel;
    private readonly WorkTaskStore? _workTaskStore;
    private readonly Func<Task<CodingChatRuntime>>? _createRuntimeAsync;
    private readonly AbilityCatalog? _abilityCatalog;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private WorkTaskItemViewModel _activeWorkTask;
    private string? _errorMessage;
    private bool _isSettingsOpen;
    private bool _isHistoryOpen;
    private bool _isArchiveOpen;
    private bool _isDisposed;
    private string _workTaskSearchText = string.Empty;

    /// <summary>创建未连接模型的设计期界面。</summary>
    public MainViewModel()
        : this([CreatePlaceholderTask()], [], null, null, null, null)
    {
    }

    private MainViewModel
    (
        IReadOnlyList<WorkTaskItemViewModel> tasks,
        IReadOnlyList<WorkTaskRecord> archivedTasks,
        CodingChatSettingsService? settingsService,
        WorkTaskStore? workTaskStore,
        Func<Task<CodingChatRuntime>>? createRuntimeAsync,
        AbilityCatalog? abilityCatalog,
        string? initialMessage = null
    )
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(archivedTasks);
        if (tasks.Count == 0)
        {
            throw new ArgumentException("至少需要一个工作任务。", nameof(tasks));
        }

        _settingsViewModel = settingsService is null
            ? null
            : new SettingsViewModel(settingsService, CloseNavigationPages);
        if (_settingsViewModel is not null)
        {
            _settingsViewModel.SettingsSaved += OnSettingsSaved;
        }

        _workTaskStore = workTaskStore;
        _createRuntimeAsync = createRuntimeAsync;
        _abilityCatalog = abilityCatalog;
        _errorMessage = initialMessage;
        _activeWorkTask = tasks[0];
        _activeWorkTask.IsActive = true;

        foreach (WorkTaskItemViewModel task in tasks)
        {
            WorkTasks.Add(task);
            Subscribe(task);
        }

        foreach (WorkTaskRecord record in archivedTasks)
        {
            ArchivedWorkTasks.Add(new ArchivedWorkTaskItemViewModel(record));
        }

        RefreshWorkTaskFilter();

        OpenSettingsCommand = new SimpleAsyncCommand(OpenSettingsAsync, () => _settingsViewModel is not null,
            exceptionHandler: HandleCommandException);
        OpenHistoryCommand = new SimpleAsyncCommand(() => OpenHistoryAsync(false), allowConcurrentExecutions: true,
            exceptionHandler: HandleCommandException);
        OpenTaskHistoryCommand = new SimpleAsyncCommand(() => OpenHistoryAsync(true), allowConcurrentExecutions: true,
            exceptionHandler: HandleCommandException);
        OpenLogDirectoryCommand = new SimpleCommand(OpenLogDirectory, () => ActiveWorkTask.Runtime is not null);
        OpenArchiveCommand = new SimpleCommand(OpenArchive);
        CloseHistoryCommand = new SimpleCommand(CloseNavigationPages);
        OpenSessionCommand = new SimpleAsyncCommand<SessionItemViewModel>
        (
            OpenSessionAsync,
            item => item is not null && SessionListViewModel.CanChangeSession,
            HandleCommandException
        );
        CreateWorkTaskCommand = new SimpleAsyncCommand(CreateWorkTaskAsync, () => _createRuntimeAsync is not null,
            exceptionHandler: HandleCommandException);
        ActivateWorkTaskCommand = new SimpleCommand<WorkTaskItemViewModel>
        (task =>
            {
                if (task is not null) Activate(task);
            }
        );
        RenameWorkTaskCommand = new SimpleCommand<WorkTaskItemViewModel>(StartRenamingTask);
        SaveWorkTaskNameCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>
        (
            SaveTaskNameAsync,
            task => task is not null && !string.IsNullOrWhiteSpace(task.EditedDisplayName),
            HandleCommandException
        );
        ArchiveWorkTaskCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>
        (
            ArchiveTaskAsync,
            task => task is not null && !task.IsWorking && WorkTasks.Count > 1,
            HandleCommandException
        );
        DeleteWorkTaskCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>
        (
            DeleteTaskAsync,
            task => task is not null && !task.IsWorking && WorkTasks.Count > 1,
            HandleCommandException
        );
        RestoreWorkTaskCommand = new SimpleAsyncCommand<ArchivedWorkTaskItemViewModel>
        (
            RestoreTaskAsync,
            item => item is not null && _createRuntimeAsync is not null,
            HandleCommandException
        );
        DeleteArchivedWorkTaskCommand = new SimpleAsyncCommand<ArchivedWorkTaskItemViewModel>
            (DeleteArchivedTaskAsync, exceptionHandler: HandleCommandException);
    }

    internal static MainViewModel Create
    (
        IReadOnlyList<WorkTaskItemViewModel> tasks,
        IReadOnlyList<WorkTaskRecord> archivedTasks,
        CodingChatSettingsService settingsService,
        WorkTaskStore workTaskStore,
        Func<Task<CodingChatRuntime>> createRuntimeAsync,
        AbilityCatalog abilityCatalog,
        string? initialMessage = null
    )
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(workTaskStore);
        ArgumentNullException.ThrowIfNull(createRuntimeAsync);
        ArgumentNullException.ThrowIfNull(abilityCatalog);
        ArgumentNullException.ThrowIfNull(archivedTasks);
        return new MainViewModel(tasks, archivedTasks, settingsService, workTaskStore, createRuntimeAsync, abilityCatalog, initialMessage);
    }

    internal static MainViewModel CreateForTests
    (
        SessionListViewModel sessions,
        ChatViewModel chat,
        CodingChatSettingsService? settingsService = null
    )
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(chat);
        return new MainViewModel
        (
            [new WorkTaskItemViewModel(GetTaskName(1), chat, sessions)],
            [],
            settingsService,
            null,
            null,
            null
        );
    }

    /// <summary>获取工作任务列表。</summary>
    public ObservableCollection<WorkTaskItemViewModel> WorkTasks { get; } = [];

    /// <summary>获取已存档工作任务。</summary>
    public ObservableCollection<ArchivedWorkTaskItemViewModel> ArchivedWorkTasks { get; } = [];

    /// <summary>获取与侧栏搜索匹配的活动任务。</summary>
    public ObservableCollection<WorkTaskItemViewModel> FilteredWorkTasks { get; } = [];

    /// <summary>获取与侧栏搜索匹配的存档任务。</summary>
    public ObservableCollection<ArchivedWorkTaskItemViewModel> FilteredArchivedWorkTasks { get; } = [];

    /// <summary>获取或设置侧栏任务搜索文本。</summary>
    public string WorkTaskSearchText
    {
        get => _workTaskSearchText;
        set
        {
            if (SetField(ref _workTaskSearchText, value ?? string.Empty))
            {
                RefreshWorkTaskFilter();
            }
        }
    }

    /// <summary>获取搜索结果中是否包含存档任务。</summary>
    public bool HasFilteredArchivedWorkTasks => FilteredArchivedWorkTasks.Count > 0;

    /// <summary>获取侧栏搜索结果是否为空。</summary>
    public bool IsWorkTaskSearchEmpty => FilteredWorkTasks.Count == 0 && FilteredArchivedWorkTasks.Count == 0;

    /// <summary>获取存档列表是否为空。</summary>
    public bool IsArchiveEmpty => ArchivedWorkTasks.Count == 0;

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
    public bool IsChatOpen => !IsHistoryOpen && !IsArchiveOpen && !IsSettingsOpen;

    /// <summary>获取历史页可见性。</summary>
    public bool IsHistoryOpen
    {
        get => _isHistoryOpen;
        private set
        {
            if (SetField(ref _isHistoryOpen, value)) OnPropertyChanged(nameof(IsChatOpen));
        }
    }

    /// <summary>获取存档页可见性。</summary>
    public bool IsArchiveOpen
    {
        get => _isArchiveOpen;
        private set
        {
            if (SetField(ref _isArchiveOpen, value)) OnPropertyChanged(nameof(IsChatOpen));
        }
    }

    /// <summary>获取设置页可见性。</summary>
    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        private set
        {
            if (SetField(ref _isSettingsOpen, value)) OnPropertyChanged(nameof(IsChatOpen));
        }
    }

    /// <summary>新建独立任务。</summary>
    public ICommand CreateWorkTaskCommand { get; }

    /// <summary>切换任务而不停止后台执行。</summary>
    public ICommand ActivateWorkTaskCommand { get; }

    /// <summary>编辑任务名。</summary>
    public ICommand RenameWorkTaskCommand { get; }

    /// <summary>确认任务名称并退出编辑。</summary>
    public ICommand SaveWorkTaskNameCommand { get; }

    /// <summary>将空闲任务移入存档。</summary>
    public ICommand ArchiveWorkTaskCommand { get; }

    /// <summary>删除空闲任务，不删除历史文件。</summary>
    public ICommand DeleteWorkTaskCommand { get; }

    /// <summary>打开存档页面。</summary>
    public ICommand OpenArchiveCommand { get; }

    /// <summary>从存档恢复工作任务。</summary>
    public ICommand RestoreWorkTaskCommand { get; }

    /// <summary>永久删除存档工作任务记录。</summary>
    public ICommand DeleteArchivedWorkTaskCommand { get; }

    /// <summary>打开全部历史。</summary>
    public ICommand OpenHistoryCommand { get; }

    /// <summary>按任务标识打开历史。</summary>
    public ICommand OpenTaskHistoryCommand { get; }

    /// <summary>使用系统文件管理器打开日志目录。</summary>
    public ICommand OpenLogDirectoryCommand { get; }

    /// <summary>返回聊天。</summary>
    public ICommand CloseHistoryCommand { get; }

    /// <summary>在当前工作任务中打开历史会话。</summary>
    public ICommand OpenSessionCommand { get; }

    /// <summary>打开设置。</summary>
    public ICommand OpenSettingsCommand { get; }

    internal static WorkTaskItemViewModel CreateRuntimeTask(
        CodingChatRuntime runtime,
        WorkTaskRecord record,
        AbilityCatalog? abilityCatalog = null)
    {
        runtime.Application.SetWorkTask(record.Id, record.DisplayName);
        return new WorkTaskItemViewModel
        (
            record.DisplayName,
            new ChatViewModel
                (runtime.ChatManager, runtime.Application, runtime.WorkspaceController, runtime.ModelDisplayName, abilityCatalog),
            new SessionListViewModel(runtime.Application),
            runtime,
            record.Id
        );
    }

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
        (ArchiveWorkTaskCommand as SimpleAsyncCommand<WorkTaskItemViewModel>)?.RaiseCanExecuteChanged();
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
            try
            {
                RefreshWorkTaskFilter();
                await SaveTasksAndReportAsync().ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                ReportError($"更新工作任务失败：{exception.Message}", exception);
            }
        }
    }

    private void Activate(WorkTaskItemViewModel task)
    {
        _activeWorkTask.IsActive = false;
        _activeWorkTask = task;
        task.IsActive = true;
        CloseNavigationPages();
        OnPropertyChanged(nameof(ActiveWorkTask));
        OnPropertyChanged(nameof(ChatViewModel));
        OnPropertyChanged(nameof(SessionListViewModel));
        (OpenSessionCommand as SimpleAsyncCommand<SessionItemViewModel>)?.RaiseCanExecuteChanged();
        (OpenLogDirectoryCommand as SimpleCommand)?.RaiseCanExecuteChanged();
    }

    private async Task OpenSessionAsync(SessionItemViewModel? item)
    {
        WorkTaskItemViewModel activeTask = ActiveWorkTask;
        if (await activeTask.Sessions.OpenSessionAsync(item).ConfigureAwait(true))
        {
            CloseNavigationPages();
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
            var record = new WorkTaskRecord
            (
                Guid.NewGuid(),
                GetTaskName(WorkTasks.Count + 1),
                null,
                runtime.ModelDisplayName,
                null
            );
            task = CreateRuntimeTask(runtime, record, _abilityCatalog);
            WorkTasks.Add(task);
            Subscribe(task);
            await SaveTasksAsync().ConfigureAwait(true);
            RefreshWorkTaskFilter();
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
            RefreshWorkTaskFilter();
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

    private async Task ArchiveTaskAsync(WorkTaskItemViewModel? task)
    {
        if (task is null || task.IsWorking || WorkTasks.Count <= 1) return;
        ErrorMessage = null;
        var archivedItem = new ArchivedWorkTaskItemViewModel(CreateRecord(task, true));
        try
        {
            await SaveTasksAsync
            (
                WorkTasks.Where(item => !ReferenceEquals(item, task)),
                [.. ArchivedWorkTasks.Select(item => item.Record), archivedItem.Record]
            ).ConfigureAwait(true);
            await DisposeTaskAsync(task).ConfigureAwait(true);
            WorkTasks.Remove(task);
            ArchivedWorkTasks.Add(archivedItem);
            RefreshWorkTaskFilter();
            OnPropertyChanged(nameof(IsArchiveEmpty));
            if (ReferenceEquals(task, _activeWorkTask)) Activate(WorkTasks[0]);
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            ReportError($"存档工作任务失败：{exception.Message}", exception);
        }
    }

    private async Task RestoreTaskAsync(ArchivedWorkTaskItemViewModel? item)
    {
        if (item is null || _createRuntimeAsync is null) return;
        ErrorMessage = null;
        CodingChatRuntime? runtime = null;
        WorkTaskItemViewModel? task = null;
        try
        {
            runtime = await _createRuntimeAsync().ConfigureAwait(true);
            task = CreateRuntimeTask(runtime, item.Record with { IsArchived = false }, _abilityCatalog);
            await RestoreTaskConfigurationAsync(task, runtime, item.Record).ConfigureAwait(true);
            await SaveTasksAsync
            (
                [.. WorkTasks, task], ArchivedWorkTasks
                    .Where(archived => !ReferenceEquals(archived, item))
                    .Select(archived => archived.Record)
            ).ConfigureAwait(true);
            WorkTasks.Add(task);
            Subscribe(task);
            ArchivedWorkTasks.Remove(item);
            RefreshWorkTaskFilter();
            OnPropertyChanged(nameof(IsArchiveEmpty));
            Activate(task);
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            if (task is not null)
            {
                task.Chat.Dispose();
                task.Sessions.Dispose();
            }

            if (runtime is not null)
            {
                await runtime.DisposeAsync().ConfigureAwait(true);
            }

            ReportError($"恢复工作任务失败：{exception.Message}", exception);
        }
    }

    private async Task DeleteArchivedTaskAsync(ArchivedWorkTaskItemViewModel? item)
    {
        if (item is null) return;
        ErrorMessage = null;
        try
        {
            await SaveTasksAsync
            (
                WorkTasks, ArchivedWorkTasks
                    .Where(archived => !ReferenceEquals(archived, item))
                    .Select(archived => archived.Record)
            ).ConfigureAwait(true);
            ArchivedWorkTasks.Remove(item);
            RefreshWorkTaskFilter();
            OnPropertyChanged(nameof(IsArchiveEmpty));
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            ReportError($"删除存档工作任务失败：{exception.Message}", exception);
        }
    }

    private async Task DeleteTaskAsync(WorkTaskItemViewModel? task)
    {
        if (task is null || task.IsWorking || WorkTasks.Count <= 1) return;
        ErrorMessage = null;
        try
        {
            await SaveTasksAsync(WorkTasks.Where(item => !ReferenceEquals(item, task))).ConfigureAwait(true);
            await DisposeTaskAsync(task).ConfigureAwait(true);
            WorkTasks.Remove(task);
            RefreshWorkTaskFilter();
            if (ReferenceEquals(task, _activeWorkTask)) Activate(WorkTasks[0]);
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            ReportError($"删除工作任务失败：{exception.Message}", exception);
        }
    }

    private void RefreshWorkTaskFilter()
    {
        string searchText = WorkTaskSearchText.Trim();
        FilteredWorkTasks.Clear();
        FilteredArchivedWorkTasks.Clear();

        foreach (WorkTaskItemViewModel task in WorkTasks.Where(task => MatchesWorkTaskSearch
                 (task.DisplayName, task.Chat.NextRunWorkspacePath, searchText)))
        {
            FilteredWorkTasks.Add(task);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            foreach (ArchivedWorkTaskItemViewModel task in ArchivedWorkTasks.Where(task => MatchesWorkTaskSearch
                     (task.DisplayName, task.WorkspacePath, searchText)))
            {
                FilteredArchivedWorkTasks.Add(task);
            }
        }

        OnPropertyChanged(nameof(HasFilteredArchivedWorkTasks));
        OnPropertyChanged(nameof(IsWorkTaskSearchEmpty));
    }

    private static bool MatchesWorkTaskSearch(string displayName, string? workspacePath, string searchText)
        => string.IsNullOrWhiteSpace(searchText)
            || displayName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
            || (workspacePath?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false);

    private void OpenLogDirectory()
    {
        string? logDirectory = ActiveWorkTask.Runtime?.Paths.LogDirectory;
        if (string.IsNullOrWhiteSpace(logDirectory)) return;

        try
        {
            Directory.CreateDirectory(logDirectory);
            Guid sessionId = ChatViewModel.CurrentSessionId;
            string? sessionLogFile = sessionId == Guid.Empty
                ? null
                : Directory.EnumerateFiles
                    (logDirectory, $"*{sessionId:N}*.log", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = sessionLogFile ?? logDirectory,
                UseShellExecute = true,
            });
            ErrorMessage = null;
        }
        catch (Exception exception) when (IsExpectedOperationException(exception))
        {
            ReportError($"打开日志失败：{exception.Message}", exception);
        }
    }

    private async Task OpenHistoryAsync(bool filterByTask)
    {
        SessionListViewModel sessions = SessionListViewModel;
        sessions.SearchText = filterByTask ? ActiveWorkTask.Id.ToString() : string.Empty;
        IsArchiveOpen = false;
        IsSettingsOpen = false;
        IsHistoryOpen = true;
        await sessions.LoadAsync().ConfigureAwait(true);
    }

    private void OpenArchive()
    {
        IsHistoryOpen = false;
        IsSettingsOpen = false;
        IsArchiveOpen = true;
    }

    private void CloseNavigationPages()
    {
        IsArchiveOpen = false;
        IsHistoryOpen = false;
        IsSettingsOpen = false;
    }

    private async Task OpenSettingsAsync()
    {
        if (_settingsViewModel is null) return;
        IsArchiveOpen = false;
        IsHistoryOpen = false;
        IsSettingsOpen = true;
        await _settingsViewModel.LoadAsync().ConfigureAwait(true);
    }

    private void OnSettingsSaved(object? sender, CodingChatSettingsSavedEventArgs e)
    {
        if (e.Settings.ModelConfiguration is not { } modelConfiguration)
        {
            return;
        }

        foreach (WorkTaskItemViewModel task in WorkTasks)
        {
            string? selectedModelDisplayName = task.Chat.SelectedModel?.DisplayName;
            task.Runtime?.EndpointManager.ReplaceConfiguration(modelConfiguration);
            task.Chat.RefreshAvailableModels(selectedModelDisplayName);
        }
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

    private Task SaveTasksAsync
    (
        IEnumerable<WorkTaskItemViewModel>? tasks = null,
        IEnumerable<WorkTaskRecord>? archivedTasks = null
    )
        => SaveTasksCoreAsync
        (
            (tasks ?? WorkTasks).ToArray(),
            (archivedTasks ?? ArchivedWorkTasks.Select(item => item.Record)).ToArray()
        );

    private async Task SaveTasksCoreAsync
    (
        IReadOnlyList<WorkTaskItemViewModel> tasks,
        IReadOnlyList<WorkTaskRecord> archivedTasks
    )
    {
        if (_workTaskStore is null) return;
        await _saveGate.WaitAsync().ConfigureAwait(true);
        try
        {
            WorkTaskRecord[] records =
            [
                .. tasks.Select(task => CreateRecord(task, false)),
                .. archivedTasks.Select(record => record with { IsArchived = true }),
            ];
            await _workTaskStore.SaveAsync(records).ConfigureAwait(true);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private static WorkTaskRecord CreateRecord(WorkTaskItemViewModel task, bool isArchived)
        => new
        (
            task.Id,
            task.DisplayName,
            task.Chat.NextRunWorkspacePath,
            task.Chat.SelectedModel?.DisplayName,
            task.Chat.SelectedReasoningEffort?.Value,
            isArchived
        );

    private static async Task RestoreTaskConfigurationAsync
    (
        WorkTaskItemViewModel task,
        CodingChatRuntime runtime,
        WorkTaskRecord record
    )
    {
        await runtime.WorkspaceController
            .ChangeWorkspaceAsync(record.WorkspacePath, validatePath: false)
            .ConfigureAwait(true);

        LanguageModelOptionViewModel? model = task.Chat.AvailableModels.FirstOrDefault
            (option => string.Equals(option.DisplayName, record.ModelReference, StringComparison.Ordinal));
        if (model is not null)
        {
            task.Chat.SelectedModel = model;
        }

        ReasoningEffortOptionViewModel? reasoningEffort = task.Chat.AvailableReasoningEfforts.FirstOrDefault
            (option => option.Value == record.ReasoningEffort);
        if (reasoningEffort is not null)
        {
            task.Chat.SelectedReasoningEffort = reasoningEffort;
        }
    }

    private async Task DisposeTaskAsync(WorkTaskItemViewModel task)
    {
        Unsubscribe(task);
        task.Chat.Dispose();
        task.Sessions.Dispose();
        if (task.Runtime is not null)
        {
            await task.Runtime.DisposeAsync().ConfigureAwait(true);
        }
    }

    private void ReportError(string message, Exception exception)
    {
        Trace.TraceError(exception.ToString());
        ErrorMessage = message;
    }

    private void HandleCommandException(Exception exception)
        => ReportError($"操作失败：{exception.Message}", exception);

    private static bool IsExpectedOperationException(Exception exception)
        => exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException
            or Win32Exception or JsonException;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        if (_settingsViewModel is not null)
        {
            _settingsViewModel.SettingsSaved -= OnSettingsSaved;
        }

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