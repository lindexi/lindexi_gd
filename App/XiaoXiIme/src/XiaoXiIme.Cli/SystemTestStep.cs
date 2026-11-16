namespace XiaoXiIme.Cli;

internal sealed record SystemTestStep(
    string Id,
    string Area,
    string Description,
    bool Destructive,
    string Evidence);