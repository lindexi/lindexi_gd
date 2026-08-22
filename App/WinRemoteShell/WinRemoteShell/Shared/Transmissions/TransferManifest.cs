namespace WinRemoteShell.Shared.Transmissions;

internal static class TransferManifest
{
    internal static TransferDefinition Create(string source)
    {
        var attributes = File.GetAttributes(source);
        if ((attributes & FileAttributes.Directory) == 0)
        {
            return new TransferDefinition(
                TransferRootType.File,
                [CreateFileEntry(source, Path.GetFileName(source))]);
        }

        var entries = new List<TransferManifestEntry>
        {
            CreateDirectoryEntry(source, ".")
        };
        foreach (var directoryPath in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            entries.Add(CreateDirectoryEntry(directoryPath, NormalizeRelativePath(source, directoryPath)));
        }

        foreach (var filePath in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            entries.Add(CreateFileEntry(filePath, NormalizeRelativePath(source, filePath)));
        }

        return new TransferDefinition(TransferRootType.Directory, entries);
    }

    private static TransferManifestEntry CreateDirectoryEntry(string path, string relativePath) =>
        new(
            TransferEntryType.Directory,
            relativePath,
            0,
            Directory.GetCreationTimeUtc(path),
            Directory.GetLastWriteTimeUtc(path),
            0,
            null);

    private static TransferManifestEntry CreateFileEntry(string path, string relativePath)
    {
        var file = new FileInfo(path);
        return new TransferManifestEntry(
            TransferEntryType.File,
            relativePath,
            file.Length,
            file.CreationTimeUtc,
            file.LastWriteTimeUtc,
            0,
            path);
    }

    private static string NormalizeRelativePath(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');
}