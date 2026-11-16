using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>Authoritative configuration and startup-only scheme status, without input text.</summary>
public sealed record ImeProductSettingsResponse(
    Guid HostInstanceId,
    ImeProductSettings Settings,
    string? CurrentInputScheme,
    string InputSchemeSource,
    bool RequiresHostRestart,
    ImeProductSettingsError Error = ImeProductSettingsError.None,
    Guid? LastOperationId = null,
    ImeProductSettingsError LoadError = ImeProductSettingsError.None);