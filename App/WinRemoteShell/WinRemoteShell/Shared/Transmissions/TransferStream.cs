using System.Buffers;

namespace WinRemoteShell.Shared.Transmissions;

internal static class TransferStream
{
    internal static async Task WriteAsync
    (
        Stream stream,
        TransferDefinition definition,
        CancellationToken cancellationToken
    )
    {
        var manifestLength = definition.Entries.Sum(entry =>
            TransferProtocol.GetManifestEntryLength(entry.RelativePath));
        await TransferProtocol.WriteHeaderAsync(
            stream,
            definition.RootType,
            definition.Entries.Count,
            manifestLength,
            cancellationToken);

        foreach (var entry in definition.Entries)
        {
            await TransferProtocol.WriteManifestEntryAsync(stream, entry, cancellationToken);
        }

        foreach (var entry in definition.Entries.Where(entry => entry.EntryType == TransferEntryType.File))
        {
            await using var file = new FileStream(
                entry.SourcePath!,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (file.Length != entry.Length)
            {
                throw new IOException($"The source file changed while being transferred: '{entry.SourcePath}'.");
            }

            await CopyExactlyAsync(file, stream, entry.Length, cancellationToken);
            if (await file.ReadAsync(new byte[1], cancellationToken) != 0)
            {
                throw new IOException($"The source file changed while being transferred: '{entry.SourcePath}'.");
            }
        }
    }

    internal static async Task ReceiveAsync(Stream stream, string target,
        bool placeFileInExistingDirectory,
        CancellationToken cancellationToken)
    {
        var header = await TransferProtocol.ReadHeaderAsync(stream, cancellationToken);
        var entries = new List<TransferManifestEntry>(header.EntryCount);
        long manifestLength = 0;
        for (var index = 0; index < header.EntryCount; index++)
        {
            var entry = await TransferProtocol.ReadManifestEntryAsync(stream, cancellationToken);
            manifestLength = checked(manifestLength + TransferProtocol.GetManifestEntryLength(entry.RelativePath));
            entries.Add(entry);
        }

        if (manifestLength != header.ManifestLength)
        {
            throw new InvalidDataException("The transfer manifest length does not match its header.");
        }

        ValidateManifest(header.RootType, entries);
        var targetRoot = Path.GetFullPath(target);
        if (header.RootType == TransferRootType.File && placeFileInExistingDirectory && Directory.Exists(targetRoot))
        {
            targetRoot = Path.Combine(targetRoot, entries[0].RelativePath);
        }

        var destinationPaths = entries.ToDictionary(
            entry => entry,
            entry => GetDestinationPath(header.RootType, targetRoot, entry.RelativePath));

        if (header.RootType == TransferRootType.Directory)
        {
            Directory.CreateDirectory(targetRoot);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetRoot)!);
        }

        foreach (var entry in entries.Where(entry => entry.EntryType == TransferEntryType.Directory))
        {
            Directory.CreateDirectory(destinationPaths[entry]);
        }

        foreach (var entry in entries.Where(entry => entry.EntryType == TransferEntryType.File))
        {
            var destinationPath = destinationPaths[entry];
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            await using (var file = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await CopyExactlyAsync(stream, file, entry.Length, cancellationToken);
            }

            File.SetCreationTimeUtc(destinationPath, entry.CreationTimeUtc);
            File.SetLastWriteTimeUtc(destinationPath, entry.LastWriteTimeUtc);
        }

        foreach (var entry in entries
                     .Where(entry => entry.EntryType == TransferEntryType.Directory)
                     .OrderByDescending(entry => entry.RelativePath.Count(character => character == '/')))
        {
            var destinationPath = destinationPaths[entry];
            Directory.SetCreationTimeUtc(destinationPath, entry.CreationTimeUtc);
            Directory.SetLastWriteTimeUtc(destinationPath, entry.LastWriteTimeUtc);
        }
    }

    private static void ValidateManifest(TransferRootType rootType,
        IReadOnlyList<TransferManifestEntry> entries)
    {
        if (rootType == TransferRootType.File &&
            (entries.Count != 1 || entries[0].EntryType != TransferEntryType.File))
        {
            throw new InvalidDataException("A file transfer must contain exactly one file entry.");
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.RelativePath) ||
                Path.IsPathRooted(entry.RelativePath) ||
                entry.RelativePath.Contains('\\'))
            {
                throw new InvalidDataException($"Transfer entry path '{entry.RelativePath}' is invalid.");
            }

            if (!paths.Add(entry.RelativePath))
            {
                throw new InvalidDataException($"Transfer entry path '{entry.RelativePath}' is duplicated.");
            }
        }
    }

    private static string GetDestinationPath(TransferRootType rootType, string targetRoot,
        string relativePath)
    {
        if (rootType == TransferRootType.File)
        {
            return targetRoot;
        }

        var destinationPath = Path.GetFullPath(Path.Combine(targetRoot, relativePath));
        if (!IsPathWithinDirectory(targetRoot, destinationPath))
        {
            throw new InvalidDataException($"Transfer entry '{relativePath}' is outside the target directory.");
        }

        return destinationPath;
    }

    private static async Task CopyExactlyAsync(Stream source, Stream destination,
        long length,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            var remaining = length;
            while (remaining > 0)
            {
                var read = await source.ReadAsync(
                    buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)),
                    cancellationToken);
                if (read == 0)
                {
                    throw new EndOfStreamException("The transfer ended before the declared file length was received.");
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static bool IsPathWithinDirectory(string directory, string path)
    {
        if (path.Equals(directory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var directoryPrefix = Path.EndsInDirectorySeparator(directory)
            ? directory
            : directory + Path.DirectorySeparatorChar;
        return path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
    }
}