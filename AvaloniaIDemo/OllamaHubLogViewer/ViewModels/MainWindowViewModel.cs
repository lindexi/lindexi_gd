using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OllamaHubLogViewer.Infrastructure;
using OllamaHubLogViewer.Models;
using OllamaHubLogViewer.Services;

namespace OllamaHubLogViewer.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly OpenAiLogLoader _logLoader;
    private readonly LogMergeService _logMergeService;
    private readonly CancellationTokenSource _lifetimeCancellationTokenSource = new();
    private CancellationTokenSource? _sessionLoadCancellationTokenSource;
    private CancellationTokenSource? _mergeCancellationTokenSource;
    private string _logRootPath;
    private LogSessionViewModel? _selectedSession;
    private string _statusText = "准备读取日志。";
    private string _warningText = string.Empty;
    private string _sessionMetadataText = string.Empty;
    private string _usageSummaryText = string.Empty;
    private string _usageDetailsText = string.Empty;
    private string _mergeStatusText = string.Empty;
    private bool _isScanning;
    private bool _isLoading;
    private bool _isMerging;
    private bool _isDisposed;

    public MainWindowViewModel()
        : this(new OpenAiLogLoader())
    {
    }

    internal MainWindowViewModel(OpenAiLogLoader logLoader)
        : this(logLoader, new LogMergeService(logLoader))
    {
    }

    internal MainWindowViewModel(OpenAiLogLoader logLoader, LogMergeService logMergeService)
    {
        ArgumentNullException.ThrowIfNull(logLoader);
        ArgumentNullException.ThrowIfNull(logMergeService);

        _logLoader = logLoader;
        _logMergeService = logMergeService;
        _logRootPath = FindDefaultLogRoot();
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
    }

    public ObservableCollection<LogSessionViewModel> Sessions { get; } = [];

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    public AsyncCommand RefreshCommand { get; }

    public string LogRootPath
    {
        get => _logRootPath;
        set => SetProperty(ref _logRootPath, value);
    }

    public LogSessionViewModel? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (!SetProperty(ref _selectedSession, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedSession));
            OnPropertyChanged(nameof(SessionTitle));
            OnPropertyChanged(nameof(HasMergedSelectedSession));
            OnPropertyChanged(nameof(SelectedSessionMergeText));
            LoadSelectedSessionAsync(value);
        }
    }

    public string MergeStatusText
    {
        get => _mergeStatusText;
        private set
        {
            if (SetProperty(ref _mergeStatusText, value))
            {
                OnPropertyChanged(nameof(HasMergeStatus));
            }
        }
    }

    public string UsageSummaryText
    {
        get => _usageSummaryText;
        private set
        {
            if (SetProperty(ref _usageSummaryText, value))
            {
                OnPropertyChanged(nameof(HasUsage));
            }
        }
    }

    public string UsageDetailsText
    {
        get => _usageDetailsText;
        private set
        {
            if (SetProperty(ref _usageDetailsText, value))
            {
                OnPropertyChanged(nameof(HasUsageDetails));
                OnPropertyChanged(nameof(HasUsage));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string WarningText
    {
        get => _warningText;
        private set
        {
            if (SetProperty(ref _warningText, value))
            {
                OnPropertyChanged(nameof(HasWarnings));
            }
        }
    }

    public string SessionMetadataText
    {
        get => _sessionMetadataText;
        private set
        {
            if (SetProperty(ref _sessionMetadataText, value))
            {
                OnPropertyChanged(nameof(HasSessionMetadata));
            }
        }
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(ShowEmptyState));
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsMerging
    {
        get => _isMerging;
        private set
        {
            if (SetProperty(ref _isMerging, value))
            {
                OnPropertyChanged(nameof(IsBusy));
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBusy => IsScanning || IsLoading || IsMerging;

    public bool HasSessions => Sessions.Count > 0;

    public bool HasMessages => Messages.Count > 0;

    public bool HasSelectedSession => SelectedSession is not null;

    public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningText);

    public bool HasSessionMetadata => !string.IsNullOrWhiteSpace(SessionMetadataText);

    public bool HasMergeStatus => !string.IsNullOrWhiteSpace(MergeStatusText);

    public bool HasMergedSelectedSession => SelectedSession?.IsMerged == true;

    public bool HasUsage => !string.IsNullOrWhiteSpace(UsageSummaryText)
                            || !string.IsNullOrWhiteSpace(UsageDetailsText);

    public bool HasUsageDetails => !string.IsNullOrWhiteSpace(UsageDetailsText);

    public bool ShowEmptyState => HasSelectedSession && !HasMessages && !IsLoading;

    public string SessionCountText => $"{Sessions.Count} 个会话";

    public string SessionTitle => SelectedSession?.DisplayTitle ?? "选择一个日志会话";

    public string SelectedSessionMergeText => SelectedSession?.MergeSourceText ?? string.Empty;

    public string MergedLogOutputPath => Path.Join(AppContext.BaseDirectory, "MergedSessions");

    public Task InitializeAsync()
    {
        return RefreshAsync();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _lifetimeCancellationTokenSource.Cancel();
        CancellationTokenSource? cancellationTokenSource =
            Interlocked.Exchange(ref _sessionLoadCancellationTokenSource, null);
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = Interlocked.Exchange(ref _mergeCancellationTokenSource, null);
        cancellationTokenSource?.Cancel();
    }

    private async Task RefreshAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        string rootPath = LogRootPath.Trim();
        if (rootPath.Length == 0)
        {
            StatusText = "请输入日志根目录。";
            WarningText = string.Empty;
            ReplaceSessions([]);
            return;
        }

        IsScanning = true;
        StatusText = "正在扫描日志目录...";
        WarningText = string.Empty;
        MergeStatusText = string.Empty;
        string? previouslySelectedPath = SelectedSession?.DirectoryPath;
        IReadOnlyList<LogSessionViewModel>? scannedSessions = null;
        CancellationToken cancellationToken = _lifetimeCancellationTokenSource.Token;
        try
        {
            Task<IReadOnlyList<LogSessionViewModel>> scanTask = Task.Run(
                () => ScanSessions(rootPath, cancellationToken),
                cancellationToken);
            Task<LogMergeResult> existingMergeTask = _logMergeService.LoadExistingAsync(
                rootPath,
                MergedLogOutputPath,
                cancellationToken);
            IReadOnlyList<LogSessionViewModel> sessions = await scanTask;
            LogMergeResult existingMergeResult = await existingMergeTask;
            cancellationToken.ThrowIfCancellationRequested();
            ApplyMergeResult(sessions, existingMergeResult);
            scannedSessions = sessions;
            ReplaceSessions(sessions);

            LogSessionViewModel? sessionToSelect = sessions.FirstOrDefault(session =>
                string.Equals(session.DirectoryPath, previouslySelectedPath, StringComparison.OrdinalIgnoreCase))
                ?? sessions.FirstOrDefault();
            if (ReferenceEquals(SelectedSession, sessionToSelect))
            {
                LoadSelectedSessionAsync(sessionToSelect);
            }
            else
            {
                SelectedSession = sessionToSelect;
            }

            StatusText = sessions.Count == 0
                ? "目录中没有找到包含 request.log 或 response.log 的会话。"
                : $"已找到 {sessions.Count} 个日志会话。";
        }
        catch (DirectoryNotFoundException)
        {
            ReplaceSessions([]);
            StatusText = "日志根目录不存在。";
        }
        catch (UnauthorizedAccessException exception)
        {
            ReplaceSessions([]);
            StatusText = "没有权限读取日志根目录。";
            WarningText = exception.Message;
        }
        catch (IOException exception)
        {
            ReplaceSessions([]);
            StatusText = "扫描日志目录失败。";
            WarningText = exception.Message;
        }
        catch (ArgumentException exception)
        {
            ReplaceSessions([]);
            StatusText = "日志根目录路径无效。";
            WarningText = exception.Message;
        }
        catch (OperationCanceledException) when (_isDisposed)
        {
        }
        finally
        {
            IsScanning = false;
        }

        if (scannedSessions is not null && !_isDisposed)
        {
            await RebuildMergedSessionsAsync(rootPath);
        }
    }

    private async void LoadSelectedSessionAsync(LogSessionViewModel? session)
    {
        CancellationTokenSource cancellationTokenSource = new();
        CancellationTokenSource? previousCancellationTokenSource =
            Interlocked.Exchange(ref _sessionLoadCancellationTokenSource, cancellationTokenSource);
        previousCancellationTokenSource?.Cancel();

        Messages.Clear();
        NotifyMessageCollectionChanged();
        WarningText = string.Empty;
        SessionMetadataText = string.Empty;
        UsageSummaryText = string.Empty;
        UsageDetailsText = string.Empty;
        if (session is null || _isDisposed)
        {
            cancellationTokenSource.Dispose();
            Interlocked.CompareExchange(ref _sessionLoadCancellationTokenSource, null, cancellationTokenSource);
            return;
        }

        IsLoading = true;
        StatusText = $"正在读取 {session.DirectoryName}...";
        try
        {
            LogConversation conversation = await _logLoader
                .LoadAsync(session.ConversationDirectoryPath, cancellationTokenSource.Token);
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            for (int index = 0; index < conversation.Messages.Count; index++)
            {
                Messages.Add(new ChatMessageViewModel(conversation.Messages[index], index));
            }

            NotifyMessageCollectionChanged();
            WarningText = string.Join(Environment.NewLine, conversation.Warnings);
            SessionMetadataText = BuildSessionMetadata(conversation);
            UsageSummaryText = BuildUsageSummary(conversation.Usage);
            UsageDetailsText = BuildUsageDetails(conversation.Usage);
            StatusText = session.IsMerged
                ? $"已加载 {conversation.Messages.Count} 条消息，来自 {session.MergedSourceCount} 次请求。"
                : $"已加载 {conversation.Messages.Count} 条消息。";
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (LogSourceChangedException)
        {
            MergeStatusText = "检测到日志仍在写入，本轮已保留上一版合并结果；稍后刷新即可。";
        }
        catch (UnauthorizedAccessException exception)
        {
            StatusText = "没有权限读取所选日志。";
            WarningText = exception.Message;
        }
        catch (IOException exception)
        {
            StatusText = "读取所选日志失败。";
            WarningText = exception.Message;
        }
        catch (ArgumentException exception)
        {
            StatusText = "所选日志路径无效。";
            WarningText = exception.Message;
        }
        finally
        {
            if (ReferenceEquals(
                    Interlocked.CompareExchange(
                        ref _sessionLoadCancellationTokenSource,
                        null,
                        cancellationTokenSource),
                    cancellationTokenSource))
            {
                IsLoading = false;
            }

            cancellationTokenSource.Dispose();
        }
    }

    private async Task RebuildMergedSessionsAsync(string rootPath)
    {
        CancellationTokenSource cancellationTokenSource = new();
        CancellationTokenSource? previousCancellationTokenSource =
            Interlocked.Exchange(ref _mergeCancellationTokenSource, cancellationTokenSource);
        previousCancellationTokenSource?.Cancel();

        IsMerging = true;
        MergeStatusText = "正在后台分析可合并的工具调用会话...";
        try
        {
            LogMergeResult mergeResult = await Task.Run(
                () => _logMergeService.RebuildAsync(
                    rootPath,
                    MergedLogOutputPath,
                    cancellationTokenSource.Token),
                cancellationTokenSource.Token);
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            string? previousConversationDirectoryPath = SelectedSession?.ConversationDirectoryPath;
            ApplyMergeResult(Sessions, mergeResult);
            OnPropertyChanged(nameof(SessionTitle));
            OnPropertyChanged(nameof(HasMergedSelectedSession));
            OnPropertyChanged(nameof(SelectedSessionMergeText));

            if (SelectedSession is { } selectedSession
                && (selectedSession.IsMerged
                    || !string.Equals(
                        previousConversationDirectoryPath,
                        selectedSession.ConversationDirectoryPath,
                        StringComparison.OrdinalIgnoreCase)))
            {
                LoadSelectedSessionAsync(selectedSession);
            }

            int sourceSessionCount = mergeResult.MergedSessions
                .SelectMany(static session => session.SourceDirectoryNames)
                .Distinct(StringComparer.Ordinal)
                .Count();
            MergeStatusText = mergeResult.MergedSessions.Count == 0
                ? $"未发现可安全合并的会话。合并索引位于 {mergeResult.OutputDirectoryPath}。"
                : $"已生成 {mergeResult.MergedSessions.Count} 个合并会话，关联 {sourceSessionCount} 条原始日志。输出到 {mergeResult.OutputDirectoryPath}。";
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (UnauthorizedAccessException exception)
        {
            MergeStatusText = $"自动合并失败：没有写入合并目录的权限。{exception.Message}";
        }
        catch (IOException exception)
        {
            MergeStatusText = $"自动合并失败：{exception.Message}";
        }
        catch (ArgumentException exception)
        {
            MergeStatusText = $"自动合并失败：路径无效。{exception.Message}";
        }
        finally
        {
            if (ReferenceEquals(
                    Interlocked.CompareExchange(
                        ref _mergeCancellationTokenSource,
                        null,
                        cancellationTokenSource),
                    cancellationTokenSource))
            {
                IsMerging = false;
            }

            cancellationTokenSource.Dispose();
        }
    }

    private static void ApplyMergeResult(
        IEnumerable<LogSessionViewModel> sessions,
        LogMergeResult mergeResult)
    {
        Dictionary<string, LogMergedSession[]> mergedSessionsBySourceName = mergeResult.MergedSessions
            .SelectMany(static mergedSession => mergedSession.SourceDirectoryNames.Select(
                sourceDirectoryName => new { sourceDirectoryName, mergedSession }))
            .GroupBy(static item => item.sourceDirectoryName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static item => item.mergedSession).ToArray(),
                StringComparer.Ordinal);

        foreach (LogSessionViewModel session in sessions)
        {
            if (mergedSessionsBySourceName.TryGetValue(
                    session.DirectoryName,
                    out LogMergedSession[]? mergedSessions)
                && mergedSessions.Length == 1)
            {
                LogMergedSession mergedSession = mergedSessions[0];
                session.ApplyMergedSession(
                    mergedSession.DirectoryPath,
                    mergedSession.SourceDirectoryNames);
            }
            else
            {
                session.ApplyMergedSession(null, []);
            }
        }
    }

    private void ReplaceSessions(IReadOnlyList<LogSessionViewModel> sessions)
    {
        Sessions.Clear();
        foreach (LogSessionViewModel session in sessions)
        {
            Sessions.Add(session);
        }

        if (sessions.Count == 0)
        {
            SelectedSession = null;
        }

        OnPropertyChanged(nameof(HasSessions));
        OnPropertyChanged(nameof(SessionCountText));
    }

    private void NotifyMessageCollectionChanged()
    {
        OnPropertyChanged(nameof(HasMessages));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private static IReadOnlyList<LogSessionViewModel> ScanSessions(
        string rootPath,
        CancellationToken cancellationToken)
    {
        string fullRootPath = Path.GetFullPath(rootPath);
        if (!Directory.Exists(fullRootPath))
        {
            throw new DirectoryNotFoundException(fullRootPath);
        }

        List<LogSessionViewModel> sessions = [];
        foreach (string directory in Directory.EnumerateDirectories(fullRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(Path.Join(directory, "request.log"))
                || File.Exists(Path.Join(directory, "response.log")))
            {
                sessions.Add(new LogSessionViewModel(directory));
            }
        }

        return sessions
            .OrderByDescending(static session => session.SortTimestamp)
            .ToArray();
    }

    private static string BuildSessionMetadata(LogConversation conversation)
    {
        List<string> parts = new(3);
        if (!string.IsNullOrWhiteSpace(conversation.Model))
        {
            parts.Add($"模型：{conversation.Model}");
        }

        if (conversation.ResponseCreatedAt is { } createdAt)
        {
            parts.Add($"响应时间：{createdAt:yyyy-MM-dd HH:mm:ss}");
        }

        if (!string.IsNullOrWhiteSpace(conversation.ResponseId))
        {
            parts.Add($"响应 ID：{conversation.ResponseId}");
        }

        return string.Join("  ·  ", parts);
    }

    private static string BuildUsageSummary(LogUsage? usage)
    {
        if (usage is null)
        {
            return string.Empty;
        }

        List<string> parts = new(3);
        if (usage.PromptTokens is { } promptTokens)
        {
            parts.Add($"输入 {promptTokens:N0}");
        }

        if (usage.CompletionTokens is { } completionTokens)
        {
            parts.Add($"输出 {completionTokens:N0}");
        }

        if (usage.TotalTokens is { } totalTokens)
        {
            parts.Add($"合计 {totalTokens:N0} tokens");
        }

        return string.Join("  ·  ", parts);
    }

    private static string BuildUsageDetails(LogUsage? usage)
    {
        if (usage is null)
        {
            return string.Empty;
        }

        List<string> parts = new(11);
        AddTokenDetail(parts, "缓存输入", usage.CachedPromptTokens);
        AddTokenDetail(parts, "推理", usage.ReasoningTokens);
        AddTokenDetail(parts, "接受预测", usage.AcceptedPredictionTokens);
        AddTokenDetail(parts, "拒绝预测", usage.RejectedPredictionTokens);
        AddTokenDetail(parts, "输入音频", usage.PromptAudioTokens);
        AddTokenDetail(parts, "输出音频", usage.CompletionAudioTokens);
        AddDurationDetail(parts, "总耗时", usage.TotalDuration);
        AddDurationDetail(parts, "加载", usage.LoadDuration);
        AddDurationDetail(parts, "输入评估", usage.PromptEvaluationDuration);
        AddDurationDetail(parts, "生成", usage.EvaluationDuration);
        return string.Join("  ·  ", parts);
    }

    private static void AddTokenDetail(List<string> parts, string label, long? value)
    {
        if (value is { } tokenCount)
        {
            parts.Add($"{label} {tokenCount:N0}");
        }
    }

    private static void AddDurationDetail(List<string> parts, string label, TimeSpan? value)
    {
        if (value is not { } duration)
        {
            return;
        }

        string durationText = duration.TotalSeconds >= 1
            ? $"{duration.TotalSeconds:0.###} 秒"
            : $"{duration.TotalMilliseconds:0.###} 毫秒";
        parts.Add($"{label} {durationText}");
    }

    private static string FindDefaultLogRoot()
    {
        string? configuredPath = Environment.GetEnvironmentVariable("OLLAMA_HUB_LOG_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

#if DEBUG
        return @"C:\lindexi\Work\OllamaHubTest";
#else
        return Path.Join(AppContext.BaseDirectory, "Session");
#endif
    }
}
