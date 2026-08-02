namespace WinRemoteShell.Shared.Transmissions;

internal sealed record TransferManifestEntry
(
    TransferEntryType EntryType,
    string RelativePath,
    long Length,
    DateTime CreationTimeUtc,
    DateTime LastWriteTimeUtc,
    uint Permissions,
    string? SourcePath
);