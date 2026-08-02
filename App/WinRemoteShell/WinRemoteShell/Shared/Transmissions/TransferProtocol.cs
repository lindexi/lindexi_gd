using System.Buffers.Binary;
using System.Text;

namespace WinRemoteShell.Shared.Transmissions;

internal static class TransferProtocol
{
    private static readonly byte[] Magic = "WRSTRNS1"u8.ToArray();

    internal const ushort Version = 1;
    internal const int HeaderLength = 28;
    internal const int EntryFixedLength = 33;
    internal const int MaximumEntryCount = 1_000_000;
    internal const int MaximumPathByteLength = 32 * 1024;

    internal static async Task WriteHeaderAsync
    (
        Stream stream,
        TransferRootType rootType,
        int entryCount,
        long manifestLength,
        CancellationToken cancellationToken
    )
    {
        var buffer = new byte[HeaderLength];
        Magic.CopyTo(buffer, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(8, 2), Version);
        buffer[10] = (byte)rootType;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(12, 4), entryCount);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(16, 8), manifestLength);
        await stream.WriteAsync(buffer, cancellationToken);
    }

    internal static async Task<TransferHeader> ReadHeaderAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[HeaderLength];
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        if (!buffer.AsSpan(0, Magic.Length).SequenceEqual(Magic))
        {
            throw new InvalidDataException("The transfer protocol header is invalid.");
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(8, 2));
        if (version != Version)
        {
            throw new InvalidDataException($"Transfer protocol version '{version}' is not supported.");
        }

        var rootType = (TransferRootType)buffer[10];
        if (rootType is not (TransferRootType.File or TransferRootType.Directory))
        {
            throw new InvalidDataException($"Transfer root type '{rootType}' is invalid.");
        }

        var entryCount = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(12, 4));
        var manifestLength = BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(16, 8));
        if (entryCount is < 1 or > MaximumEntryCount || manifestLength < EntryFixedLength)
        {
            throw new InvalidDataException("The transfer manifest header is invalid.");
        }

        return new TransferHeader(rootType, entryCount, manifestLength);
    }

    internal static long GetManifestEntryLength(string relativePath)
    {
        var pathLength = Encoding.UTF8.GetByteCount(relativePath);
        if (pathLength is < 1 or > MaximumPathByteLength)
        {
            throw new InvalidDataException($"The relative path length for '{relativePath}' is invalid.");
        }

        return EntryFixedLength + pathLength;
    }

    internal static async Task WriteManifestEntryAsync(
        Stream stream,
        TransferManifestEntry entry,
        CancellationToken cancellationToken)
    {
        var pathBytes = Encoding.UTF8.GetBytes(entry.RelativePath);
        if (pathBytes.Length is < 1 or > MaximumPathByteLength)
        {
            throw new InvalidDataException($"The relative path length for '{entry.RelativePath}' is invalid.");
        }

        var buffer = new byte[EntryFixedLength];
        buffer[0] = (byte)entry.EntryType;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(1, 4), entry.Permissions);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(5, 4), pathBytes.Length);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(9, 8), entry.Length);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(17, 8), entry.CreationTimeUtc.Ticks);
        BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(25, 8), entry.LastWriteTimeUtc.Ticks);
        await stream.WriteAsync(buffer, cancellationToken);
        await stream.WriteAsync(pathBytes, cancellationToken);
    }

    internal static async Task<TransferManifestEntry> ReadManifestEntryAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[EntryFixedLength];
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        var entryType = (TransferEntryType)buffer[0];
        if (entryType is not (TransferEntryType.File or TransferEntryType.Directory))
        {
            throw new InvalidDataException($"Transfer entry type '{entryType}' is invalid.");
        }

        var permissions = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(1, 4));
        if (permissions != 0)
        {
            throw new InvalidDataException("Non-default file permissions are not supported.");
        }

        var pathLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(5, 4));
        var length = BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(9, 8));
        if (pathLength is < 1 or > MaximumPathByteLength ||
            length < 0 ||
            entryType == TransferEntryType.Directory && length != 0)
        {
            throw new InvalidDataException("The transfer manifest entry is invalid.");
        }

        var creationTimeUtc = ReadUtcDateTime(buffer.AsSpan(17, 8));
        var lastWriteTimeUtc = ReadUtcDateTime(buffer.AsSpan(25, 8));
        var pathBytes = new byte[pathLength];
        await stream.ReadExactlyAsync(pathBytes, cancellationToken);
        return new TransferManifestEntry(
            entryType,
            Encoding.UTF8.GetString(pathBytes),
            length,
            creationTimeUtc,
            lastWriteTimeUtc,
            permissions,
            null);
    }

    private static DateTime ReadUtcDateTime(ReadOnlySpan<byte> value)
    {
        var ticks = BinaryPrimitives.ReadInt64LittleEndian(value);
        try
        {
            return new DateTime(ticks, DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidDataException("The transfer manifest contains an invalid timestamp.", exception);
        }
    }
}