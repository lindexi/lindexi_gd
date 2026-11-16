namespace XiaoXiIme.ImeIpc;

/// <summary>Stable failure categories; user-facing text is supplied by UI resources.</summary>
public enum ImeProductSettingsError
{
    None, Unsupported, InvalidSettings, Conflict, HostChanged, PackageUnavailable, StorageUnavailable
}