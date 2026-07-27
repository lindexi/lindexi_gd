namespace DeepSeekWpf.Services;

public interface IDiagnosticsService
{
    Task<string> CreateSummaryAsync(CancellationToken cancellationToken = default);

    Task ClearLogsAsync(CancellationToken cancellationToken = default);
}