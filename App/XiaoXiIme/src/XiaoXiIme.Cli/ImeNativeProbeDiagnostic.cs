namespace XiaoXiIme.Cli;

internal sealed record ImeNativeProbeDiagnostic
(
    bool ImageMappingSucceeded,
    int ImageMappingErrorCode,
    string? ImageMappingErrorMessage
);