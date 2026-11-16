namespace XiaoXiIme.Cli;

internal sealed record SystemTestCommand(string Id, string FileName, IReadOnlyList<string> Arguments);