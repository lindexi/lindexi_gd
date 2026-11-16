namespace XiaoXiIme.Cli;

internal sealed record ImeDiagnosticReport
(
    ImeEnvironmentDiagnostic Environment,
    ImeFileDiagnostic SourceFile,
    ImePeDiagnostic? PeImage,
    IReadOnlyList<ImeImportDiagnostic> Imports,
    ImeNativeProbeDiagnostic NativeProbe,
    ImeInstallationStateDiagnostic InstallationState,
    IReadOnlyList<string> Findings
);