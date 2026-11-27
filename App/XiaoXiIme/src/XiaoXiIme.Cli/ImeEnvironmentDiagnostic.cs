namespace XiaoXiIme.Cli;

internal sealed record ImeEnvironmentDiagnostic
(
    string OperatingSystem,
    string OsArchitecture,
    string ProcessArchitecture,
    bool Is64BitOperatingSystem,
    bool Is64BitProcess,
    string ProcessPath,
    string CurrentDirectory,
    string WindowsDirectory,
    string SystemDirectory,
    string SystemX86Directory
);