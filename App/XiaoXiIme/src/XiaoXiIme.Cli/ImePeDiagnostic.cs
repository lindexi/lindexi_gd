namespace XiaoXiIme.Cli;

internal sealed record ImePeDiagnostic
(
    string Machine,
    string Magic,
    string Subsystem,
    bool IsDll,
    bool HasImportTable,
    int ImportCount,
    string? ParseError
);