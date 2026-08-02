namespace WinRemoteShell.Shared.Transmissions;

internal sealed record TransferDefinition(TransferRootType RootType,
    IReadOnlyList<TransferManifestEntry> Entries);