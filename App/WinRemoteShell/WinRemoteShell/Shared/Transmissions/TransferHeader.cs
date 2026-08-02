namespace WinRemoteShell.Shared.Transmissions;

internal sealed record TransferHeader(TransferRootType RootType, int EntryCount, long ManifestLength);