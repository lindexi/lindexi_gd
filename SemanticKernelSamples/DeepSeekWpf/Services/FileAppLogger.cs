using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace DeepSeekWpf.Services;

public sealed class FileAppLogger : IAppLogger, IDisposable
{
    private const int RetentionDays = 14;
    private const long DefaultMaximumTotalBytes = 100L * 1024 * 1024;
    private readonly ISettingsService _settingsService;
    private readonly Channel<LogEntry> _entries = Channel.CreateUnbounded<LogEntry>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private CancellationTokenSource? _writerCancellation;
    private Task? _writerTask;
    private bool _disposed;

    public FileAppLogger(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public Exception? LastWriteError { get; private set; }

    public string LogDirectory => _settingsService.CurrentSettings.LogPath;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_writerTask is not null)
        {
            return;
        }

        Directory.CreateDirectory(LogDirectory);
        await MaintainLogsAsync(cancellationToken).ConfigureAwait(false);
        _writerCancellation = new CancellationTokenSource();
        _writerTask = ProcessEntriesAsync(_writerCancellation.Token);
    }

    public ValueTask LogAsync(
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);
        return _entries.Writer.WriteAsync(
            new LogEntry(DateTimeOffset.Now, level, message, exception),
            cancellationToken);
    }

    public async Task ClearLogsAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!Directory.Exists(LogDirectory))
            {
                return;
            }

            foreach (var filePath in Directory.EnumerateFiles(LogDirectory, "app-*.log"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Delete(filePath);
            }
        }
        catch (Exception exception)
        {
            ReportWriteFailure(exception);
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_writerTask is null)
        {
            return;
        }

        _entries.Writer.TryComplete();
        try
        {
            await _writerTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writerCancellation?.Cancel();
            _writerCancellation?.Dispose();
            _writerCancellation = null;
            _writerTask = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _entries.Writer.TryComplete();
        _writerCancellation?.Cancel();
        _writerCancellation?.Dispose();
        _fileLock.Dispose();
    }

    private async Task ProcessEntriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entry in _entries.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await WriteEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task WriteEntryAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var filePath = Path.Combine(LogDirectory, $"app-{entry.Timestamp:yyyyMMdd}.log");
            var builder = new StringBuilder()
                .Append('[').Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)).Append("] [")
                .Append(entry.Level).Append("] ").Append(entry.Message);
            if (entry.Exception is not null)
            {
                builder.AppendLine().Append(entry.Exception);
            }

            builder.AppendLine();
            await File.AppendAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            LastWriteError = null;
            await MaintainLogsCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            ReportWriteFailure(exception);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task MaintainLogsAsync(CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await MaintainLogsCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private Task MaintainLogsCoreAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(LogDirectory))
        {
            return Task.CompletedTask;
        }

        var files = Directory.EnumerateFiles(LogDirectory, "app-*.log")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToList();
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

        foreach (var expiredFile in files.Where(file => file.LastWriteTimeUtc < cutoff).ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            expiredFile.Delete();
            files.Remove(expiredFile);
        }

        var totalBytes = files.Sum(file => file.Exists ? file.Length : 0L);
        foreach (var file in files.OrderBy(file => file.LastWriteTimeUtc))
        {
            if (totalBytes <= DefaultMaximumTotalBytes)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var length = file.Length;
            file.Delete();
            totalBytes -= length;
        }

        return Task.CompletedTask;
    }

    private void ReportWriteFailure(Exception exception)
    {
        LastWriteError = exception;
        Debug.WriteLine($"DeepSeekWpf 日志写入失败：{exception}");
    }

    private sealed record LogEntry(
        DateTimeOffset Timestamp,
        LogLevel Level,
        string Message,
        Exception? Exception);
}