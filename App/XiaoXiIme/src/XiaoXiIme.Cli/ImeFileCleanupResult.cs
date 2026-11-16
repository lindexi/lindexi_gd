namespace XiaoXiIme.Cli;

internal readonly record struct ImeFileCleanupResult(
    bool Succeeded,
    IReadOnlyList<string> Messages,
    bool RebootRequired,
    IReadOnlyList<string> PendingDeletePaths,
    IReadOnlyList<string> RetiredFilePaths);