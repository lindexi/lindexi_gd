namespace XiaoXiIme.Cli;

internal sealed record ImeInstallationStateDiagnostic(
    string ExpectedSystemImePath,
    bool ExpectedSystemImeExists,
    long? ExpectedSystemImeLength,
    bool SystemDirectoryWriteProbeSucceeded,
    int SystemDirectoryWriteProbeErrorCode,
    string? SystemDirectoryWriteProbeErrorMessage,
    IReadOnlyList<ImeKeyboardLayoutDiagnostic> MatchingKeyboardLayouts);