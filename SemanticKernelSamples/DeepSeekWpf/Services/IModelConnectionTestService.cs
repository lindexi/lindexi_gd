namespace DeepSeekWpf.Services;

public interface IModelConnectionTestService
{
    Task<ModelConnectionTestResult> TestAsync(CancellationToken cancellationToken = default);
}

public sealed record ModelConnectionTestResult(
    bool IsSuccess,
    AiChatErrorCategory? ErrorCategory,
    string Message);
