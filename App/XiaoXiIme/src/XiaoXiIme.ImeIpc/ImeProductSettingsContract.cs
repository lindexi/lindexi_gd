using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc;

/// <summary>Requests authoritative saved preferences without changing sessions.</summary>
public sealed record ImeProductSettingsRequest;

/// <summary>Submits one complete preference transaction to a specific Host instance.</summary>
public sealed record ImeSaveProductSettingsRequest(Guid HostInstanceId, Guid OperationId, long ExpectedRevision, ImeProductSettings Settings);

/// <summary>Stable failure categories; user-facing text is supplied by UI resources.</summary>
public enum ImeProductSettingsError
{
    None, Unsupported, InvalidSettings, Conflict, HostChanged, PackageUnavailable, StorageUnavailable
}

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
