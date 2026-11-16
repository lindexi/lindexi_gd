namespace XiaoXiIme.Cli;

internal sealed record ImeInstallationVariantResult
(
    string Id,
    string ProbePath,
    bool Copied,
    bool InstallSucceeded,
    string? KeyboardLayout,
    int Win32ErrorCode,
    string? Win32ErrorMessage,
    bool UninstallSucceeded,
    bool CleanupSucceeded,
    IReadOnlyList<string> Notes
);