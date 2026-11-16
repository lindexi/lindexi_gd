namespace XiaoXiIme.Cli;

internal sealed record ImeImportDiagnostic(
    string Module,
    string Kind,
    bool Resolved,
    string? ResolvedPath,
    int ErrorCode,
    string? ErrorMessage);