using Microsoft.Extensions.Logging;

namespace DeepSeekWpf.Services;

public interface IAppLogger
{
    Exception? LastWriteError { get; }

    string LogDirectory { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    ValueTask LogAsync(
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default);

    ValueTask TraceAsync(string message, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevel.Trace, message, cancellationToken: cancellationToken);

    ValueTask DebugAsync(string message, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevel.Debug, message, cancellationToken: cancellationToken);

    ValueTask InformationAsync(string message, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevel.Information, message, cancellationToken: cancellationToken);

    ValueTask WarningAsync(
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default) =>
        LogAsync(LogLevel.Warning, message, exception, cancellationToken);

    ValueTask ErrorAsync(
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default) =>
        LogAsync(LogLevel.Error, message, exception, cancellationToken);

    Task ClearLogsAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
