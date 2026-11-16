namespace XiaoXiIme.Cli;

internal sealed record ImeFileDiagnostic(
    string FullPath,
    bool Exists,
    long? Length,
    string? Sha256,
    string? Attributes,
    bool CanOpenForRead,
    string? ReadError,
    bool HasMarkOfTheWeb,
    string? FileVersion,
    string? ProductVersion);