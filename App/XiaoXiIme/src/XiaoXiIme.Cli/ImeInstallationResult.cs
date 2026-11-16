namespace XiaoXiIme.Cli;

internal readonly record struct ImeInstallationResult
(
    bool Succeeded,
    string Message,
    int? Win32ErrorCode,
    string? SourcePath,
    string? InstalledPath,
    bool CopiedToSystemDirectory,
    bool? RollbackSucceeded,
    string? RollbackError,
    ImeInstallationFailureKind FailureKind = ImeInstallationFailureKind.None
)
{
    public static ImeInstallationResult Failure
    (
        string message,
        int? win32ErrorCode = null,
        string? installedPath = null,
        bool copiedToSystemDirectory = false,
        bool? rollbackSucceeded = null,
        string? rollbackError = null,
        ImeInstallationFailureKind failureKind = ImeInstallationFailureKind.Other
    ) =>
        new
        (
            false, message, win32ErrorCode, null, installedPath, copiedToSystemDirectory, rollbackSucceeded,
            rollbackError, failureKind
        );
}