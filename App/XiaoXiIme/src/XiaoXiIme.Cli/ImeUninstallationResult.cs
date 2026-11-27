namespace XiaoXiIme.Cli;

internal readonly record struct ImeUninstallationResult
(
    bool Succeeded,
    IReadOnlyList<string> RemovedLayoutIds,
    string Message,
    bool RebootRequired = false,
    IReadOnlyList<string>? PendingDeletePaths = null,
    IReadOnlyList<string>? RetiredFilePaths = null
);