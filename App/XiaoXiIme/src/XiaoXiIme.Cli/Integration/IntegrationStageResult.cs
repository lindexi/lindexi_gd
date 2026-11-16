namespace XiaoXiIme.Cli;

internal sealed record IntegrationStageResult
(
    string Id,
    bool Succeeded,
    int ExitCode,
    string Message,
    string StandardOutput,
    string StandardError,
    object? Data = null
);