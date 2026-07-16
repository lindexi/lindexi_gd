using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OllamaHubLogViewer.Infrastructure;

namespace OllamaHubLogViewer.ViewModels;

internal sealed class LogSessionViewModel : ObservableObject
{
    private const string DirectoryTimestampFormat = "yyyy-MM-dd_HH-mm-ss";
    private string? _mergedDirectoryPath;
    private IReadOnlyList<string> _mergedSourceDirectoryNames = [];

    public LogSessionViewModel(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        DirectoryPath = Path.GetFullPath(directoryPath);
        DirectoryName = Path.GetFileName(DirectoryPath);
        Timestamp = ParseTimestamp(DirectoryName);
        DateText = Timestamp?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture) ?? DirectoryName;
        string requestPath = Path.Join(DirectoryPath, "request.log");
        string responsePath = Path.Join(DirectoryPath, "response.log");
        HasRequest = File.Exists(requestPath);
        HasResponse = File.Exists(responsePath);
        FileSummary = (HasRequest, HasResponse) switch
        {
            (true, true) => "请求 + 响应",
            (true, false) => "仅请求",
            (false, true) => "仅响应",
            _ => "无日志文件",
        };
        SortTimestamp = Timestamp ?? new DateTimeOffset(Directory.GetLastWriteTime(DirectoryPath));
    }

    public string DirectoryPath { get; }

    public string DirectoryName { get; }

    public DateTimeOffset? Timestamp { get; }

    public DateTimeOffset SortTimestamp { get; }

    public string DateText { get; }

    public string FileSummary { get; }

    public bool HasRequest { get; }

    public bool HasResponse { get; }

    public string ConversationDirectoryPath => _mergedDirectoryPath ?? DirectoryPath;

    public bool IsMerged => _mergedDirectoryPath is not null;

    public int MergedSourceCount => _mergedSourceDirectoryNames.Count;

    public string SessionSummaryText => IsMerged
        ? $"已合并 {MergedSourceCount} 次请求"
        : FileSummary;

    public string MergeSourceText => IsMerged
        ? $"合并来源：{string.Join(" → ", _mergedSourceDirectoryNames)}"
        : string.Empty;

    public string DisplayTitle => IsMerged
        ? $"{DateText} · 合并会话"
        : DateText;

    internal void ApplyMergedSession(
        string? mergedDirectoryPath,
        IReadOnlyList<string> sourceDirectoryNames)
    {
        ArgumentNullException.ThrowIfNull(sourceDirectoryNames);

        string? fullMergedDirectoryPath = string.IsNullOrWhiteSpace(mergedDirectoryPath)
            ? null
            : Path.GetFullPath(mergedDirectoryPath);
        string[] sourceNames = fullMergedDirectoryPath is null
            ? []
            : sourceDirectoryNames.ToArray();
        if (string.Equals(
                _mergedDirectoryPath,
                fullMergedDirectoryPath,
                StringComparison.OrdinalIgnoreCase)
            && _mergedSourceDirectoryNames.SequenceEqual(sourceNames, StringComparer.Ordinal))
        {
            return;
        }

        _mergedDirectoryPath = fullMergedDirectoryPath;
        _mergedSourceDirectoryNames = sourceNames;
        OnPropertyChanged(nameof(ConversationDirectoryPath));
        OnPropertyChanged(nameof(IsMerged));
        OnPropertyChanged(nameof(MergedSourceCount));
        OnPropertyChanged(nameof(SessionSummaryText));
        OnPropertyChanged(nameof(MergeSourceText));
        OnPropertyChanged(nameof(DisplayTitle));
    }

    private static DateTimeOffset? ParseTimestamp(string directoryName)
    {
        if (directoryName.Length < DirectoryTimestampFormat.Length
            || !DateTime.TryParseExact(
                directoryName[..DirectoryTimestampFormat.Length],
                DirectoryTimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime timestamp))
        {
            return null;
        }

        return new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Local));
    }
}
