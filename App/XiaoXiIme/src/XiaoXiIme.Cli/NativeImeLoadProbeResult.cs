namespace XiaoXiIme.Cli;

internal sealed record NativeImeLoadProbeResult(
    string ImePath,
    bool LoadSucceeded,
    int LoadErrorCode,
    string? LoadErrorMessage,
    IReadOnlyDictionary<string, bool> RequiredExports,
    bool AllRequiredExportsFound,
    bool ImeEnumRegisterWordCallSucceeded);