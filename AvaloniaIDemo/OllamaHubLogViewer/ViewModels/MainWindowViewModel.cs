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
    private CancellationTokenSource? _sessionLoadCancellationTokenSource;
    private string _logRootPath;
    private LogSessionViewModel? _selectedSession;
    private string _statusText = "准备读取日志。";
    private string _warningText = string.Empty;
    private string _sessionMetadataText = string.Empty;
    private string _usageSummaryText = string.Empty;
    private string _usageDetailsText = string.Empty;
    private bool _isScanning;
    private bool _isLoading;
    private bool _isDisposed;

    public MainWindowViewModel()
        : this(new OpenAiLogLoader())
    {
    }

    internal MainWindowViewModel(OpenAiLogLoader logLoader)
    {
        ArgumentNullException.ThrowIfNull(logLoader);

        _logLoader = logLoader;
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
            LoadSelectedSessionAsync(value);
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

    public bool IsBusy => IsScanning || IsLoading;

    public bool HasSessions => Sessions.Count > 0;

    public bool HasMessages => Messages.Count > 0;

    public bool HasSelectedSession => SelectedSession is not null;

    public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningText);

    public bool HasSessionMetadata => !string.IsNullOrWhiteSpace(SessionMetadataText);

    public bool HasUsage => !string.IsNullOrWhiteSpace(UsageSummaryText)
                            || !string.IsNullOrWhiteSpace(UsageDetailsText);

    public bool HasUsageDetails => !string.IsNullOrWhiteSpace(UsageDetailsText);

    public bool ShowEmptyState => HasSelectedSession && !HasMessages && !IsLoading;

    public string SessionCountText => $"{Sessions.Count} 个会话";

    public string SessionTitle => SelectedSession?.DateText ?? "选择一个日志会话";

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
        CancellationTokenSource? cancellationTokenSource =
            Interlocked.Exchange(ref _sessionLoadCancellationTokenSource, null);
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
        string? previouslySelectedPath = SelectedSession?.DirectoryPath;
        try
        {
            IReadOnlyList<LogSessionViewModel> sessions = await Task.Run(
                () => ScanSessions(rootPath),
                CancellationToken.None);
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
        finally
        {
            IsScanning = false;
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
                .LoadAsync(session.DirectoryPath, cancellationTokenSource.Token);
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
            StatusText = $"已加载 {conversation.Messages.Count} 条消息。";
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
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

    private static IReadOnlyList<LogSessionViewModel> ScanSessions(string rootPath)
    {
        string fullRootPath = Path.GetFullPath(rootPath);
        if (!Directory.Exists(fullRootPath))
        {
            throw new DirectoryNotFoundException(fullRootPath);
        }

        return Directory
            .EnumerateDirectories(fullRootPath)
            .Where(static directory =>
                File.Exists(Path.Join(directory, "request.log"))
                || File.Exists(Path.Join(directory, "response.log")))
            .Select(static directory => new LogSessionViewModel(directory))
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
