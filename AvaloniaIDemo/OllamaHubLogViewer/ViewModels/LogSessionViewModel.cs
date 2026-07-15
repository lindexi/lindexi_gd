using System;
using System.Globalization;
using System.IO;

namespace OllamaHubLogViewer.ViewModels;

internal sealed class LogSessionViewModel
{
    private const string DirectoryTimestampFormat = "yyyy-MM-dd_HH-mm-ss";

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
