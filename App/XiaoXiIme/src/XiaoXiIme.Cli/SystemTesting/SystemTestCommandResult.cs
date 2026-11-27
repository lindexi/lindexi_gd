namespace XiaoXiIme.Cli;

internal sealed record SystemTestCommandResult(string Id, int ExitCode, string StandardOutput, string StandardError);