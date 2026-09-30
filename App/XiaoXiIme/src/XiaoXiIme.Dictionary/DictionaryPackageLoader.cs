using System.Text;
using System.Text.Json;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Validates and loads a XiaoXiIme runtime dictionary package into memory.
/// </summary>
public static class DictionaryPackageLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Loads a package from a directory. No package files are accessed after this method returns.
    /// </summary>
    public static CompiledImeDictionary Load(string packageDirectory)
    {
        if (string.IsNullOrWhiteSpace(packageDirectory))
        {
            throw new ArgumentException(nameof(packageDirectory), nameof(packageDirectory));
        }

        try
        {
            var (manifest, shards) = ReadAndValidatePackage(Path.GetFullPath(packageDirectory));
            return LoadValidatedPackage(manifest, shards);
        }
        catch (DictionaryPackageException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException)
        {
            throw new DictionaryPackageException(packageDirectory, exception.Message);
        }
    }

    /// <summary>
    /// Validates a package directory and returns its manifest without keeping shard files open.
    /// </summary>
    public static DictionaryPackageInspection Inspect(string packageDirectory)
    {
        if (string.IsNullOrWhiteSpace(packageDirectory))
        {
            throw new ArgumentException(nameof(packageDirectory), nameof(packageDirectory));
        }

        try
        {
            var rootPath = Path.GetFullPath(packageDirectory);
            var (manifest, shards) = ReadAndValidatePackage(rootPath);
            return new DictionaryPackageInspection(rootPath, manifest, shards);
        }
        catch (DictionaryPackageException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException)
        {
            throw new DictionaryPackageException(packageDirectory, exception.Message);
        }
    }

    private static (DictionaryPackageManifest Manifest, Dictionary<string, string> Shards) ReadAndValidatePackage(string rootPath)
    {
        var manifestPath = Path.Combine(rootPath, DictionaryPackageFormat.ManifestFileName);
        var manifestInfo = new FileInfo(manifestPath);
        if (!manifestInfo.Exists || manifestInfo.Length > DictionaryPackageFormat.MaxManifestBytes)
        {
            throw new InvalidDataException(DictionaryResources.InvalidManifestLength);
        }

        DictionaryPackageManifest manifest;
        using (var stream = File.OpenRead(manifestPath))
        {
            manifest = JsonSerializer.Deserialize<DictionaryPackageManifest>(stream, JsonOptions)
                ?? throw new InvalidDataException(DictionaryResources.InvalidManifest);
        }

        ValidateManifest(manifest);
        return (manifest, ValidateShards(rootPath, manifest.Shards));
    }

    private static CompiledImeDictionary LoadValidatedPackage(
        DictionaryPackageManifest manifest,
        IReadOnlyDictionary<string, string> shards)
    {
        var candidates = ReadCandidates(shards[DictionaryPackageFormat.CandidatesRole], manifest.Counts.Candidates);
        var exactIndex = ReadIndex(
            shards[DictionaryPackageFormat.ExactIndexRole],
            DictionaryPackageFormat.ExactIndexMagic,
            manifest.Counts.ExactKeys,
            candidates.Length);
        var prefixIndex = ReadIndex(
            shards[DictionaryPackageFormat.PrefixIndexRole],
            DictionaryPackageFormat.PrefixIndexMagic,
            manifest.Counts.PrefixKeys,
            candidates.Length);
        var shapeEntries = ReadShapeEntries(
            shards[DictionaryPackageFormat.ShapeIndexRole],
            manifest.Counts.ShapeEntries);
        var symbols = ReadSymbolEntries(
            shards[DictionaryPackageFormat.SymbolsRole],
            manifest.Counts.SymbolInputs);
        return new CompiledImeDictionary(
            candidates,
            exactIndex,
            prefixIndex,
            shapeEntries,
            symbols,
            manifest.Parameters.InputScheme);
    }

    private static void ValidateManifest(DictionaryPackageManifest manifest)
    {
        if (manifest.FormatVersion != DictionaryPackageManifest.CurrentFormatVersion)
        {
            throw new InvalidDataException(DictionaryResources.UnsupportedPackageVersion);
        }

        if (!string.Equals(manifest.PackageKind, DictionaryPackageManifest.ExpectedPackageKind, StringComparison.Ordinal))
        {
            throw new InvalidDataException(DictionaryResources.InvalidPackageKind);
        }

        if ((!string.IsNullOrEmpty(manifest.CompilerVersion)
                && !string.Equals(manifest.CompilerVersion, DictionaryPackageManifest.CurrentCompilerVersion, StringComparison.Ordinal))
            || (!string.Equals(manifest.Parameters.InputScheme, DictionaryPackageFormat.FullPinyinInputScheme, StringComparison.Ordinal)
                && !string.Equals(manifest.Parameters.InputScheme, DictionaryPackageFormat.XiaoheDoublePinyinInputScheme, StringComparison.Ordinal))
            || manifest.Parameters.MaxPrefixCandidatesPerKey <= 0
            || manifest.Parameters.MaxPrefixCandidatesPerKey > DictionaryPackageFormat.MaxCandidatesPerKey)
        {
            throw new InvalidDataException(DictionaryResources.InvalidCompilerParameters);
        }

        if (manifest.Sources.Count > DictionaryPackageFormat.MaxManifestItems
            || manifest.Shards.Count > DictionaryPackageFormat.MaxManifestItems)
        {
            throw new InvalidDataException(DictionaryResources.PackageCountTooLarge);
        }

        if (manifest.Counts.Candidates is < 0 or > DictionaryPackageFormat.MaxCandidates
            || manifest.Counts.ExactKeys is < 0 or > DictionaryPackageFormat.MaxIndexKeys
            || manifest.Counts.PrefixKeys is < 0 or > DictionaryPackageFormat.MaxIndexKeys
            || manifest.Counts.ShapeEntries is < 0 or > DictionaryPackageFormat.MaxCandidates
            || manifest.Counts.SymbolInputs is < 0 or > DictionaryPackageFormat.MaxIndexKeys)
        {
            throw new InvalidDataException(DictionaryResources.PackageCountTooLarge);
        }
    }

    private static Dictionary<string, string> ValidateShards(
        string rootPath,
        IReadOnlyList<DictionaryPackageShard> shards)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        long totalLength = 0;
        foreach (var shard in shards)
        {
            if (string.IsNullOrWhiteSpace(shard.Role) || !IsSafeRelativePath(shard.Path))
            {
                throw new InvalidDataException(DictionaryResources.InvalidShardPath);
            }

            var fullPath = Path.GetFullPath(Path.Combine(rootPath, shard.Path));
            if (!Path.IsPathFullyQualified(rootPath)
                || !fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(DictionaryResources.InvalidShardPath);
            }

            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists || shard.Length < 0 || shard.Length > DictionaryPackageFormat.MaxShardBytes || fileInfo.Length != shard.Length)
            {
                throw new InvalidDataException(DictionaryResources.InvalidShardLength);
            }

            totalLength = checked(totalLength + shard.Length);
            if (totalLength > DictionaryPackageFormat.MaxTotalShardBytes || !result.TryAdd(shard.Role, fullPath))
            {
                throw new InvalidDataException(DictionaryResources.InvalidShardSet);
            }
        }

        foreach (var requiredRole in new[]
                 {
                     DictionaryPackageFormat.CandidatesRole,
                     DictionaryPackageFormat.ExactIndexRole,
                     DictionaryPackageFormat.PrefixIndexRole,
                     DictionaryPackageFormat.ShapeIndexRole,
                     DictionaryPackageFormat.SymbolsRole,
                 })
        {
            if (!result.ContainsKey(requiredRole))
            {
                throw new InvalidDataException(DictionaryResources.InvalidShardSet);
            }
        }

        return result;
    }

    private static ImeCandidate[] ReadCandidates(string path, int expectedCount)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        ValidateHeader(reader, DictionaryPackageFormat.CandidatesMagic);
        var count = ReadBoundedCount(reader, DictionaryPackageFormat.MaxCandidates);
        if (count != expectedCount)
        {
            throw new InvalidDataException(DictionaryResources.InvalidPackageCount);
        }

        var candidates = new ImeCandidate[count];
        for (var index = 0; index < candidates.Length; index++)
        {
            var text = ReadString(reader);
            var reading = ReadString(reader);
            var frequency = reader.ReadInt32();
            if (text.Length == 0 || reading.Length == 0 || frequency < 0)
            {
                throw new InvalidDataException(DictionaryResources.InvalidCandidateData);
            }

            candidates[index] = new ImeCandidate(text, reading, frequency);
        }

        EnsureEndOfStream(stream);
        return candidates;
    }

    private static Dictionary<string, int[]> ReadIndex(
        string path,
        ReadOnlySpan<byte> magic,
        int expectedCount,
        int candidateCount)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        ValidateHeader(reader, magic);
        var count = ReadBoundedCount(reader, DictionaryPackageFormat.MaxIndexKeys);
        if (count != expectedCount)
        {
            throw new InvalidDataException(DictionaryResources.InvalidPackageCount);
        }

        var index = new Dictionary<string, int[]>(count, StringComparer.Ordinal);
        for (var keyIndex = 0; keyIndex < count; keyIndex++)
        {
            var key = ReadString(reader);
            var candidateIdCount = ReadBoundedCount(reader, DictionaryPackageFormat.MaxCandidatesPerKey);
            var candidateIds = new int[candidateIdCount];
            for (var candidateIndex = 0; candidateIndex < candidateIds.Length; candidateIndex++)
            {
                var candidateId = reader.ReadInt32();
                if ((uint)candidateId >= (uint)candidateCount)
                {
                    throw new InvalidDataException(DictionaryResources.InvalidCandidateId);
                }

                candidateIds[candidateIndex] = candidateId;
            }

            if (key.Length == 0 || !index.TryAdd(key, candidateIds))
            {
                throw new InvalidDataException(DictionaryResources.InvalidIndexData);
            }
        }

        EnsureEndOfStream(stream);
        return index;
    }

    private static ShapeDictionaryEntry[] ReadShapeEntries(string path, int expectedCount)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        ValidateHeader(reader, DictionaryPackageFormat.ShapeIndexMagic);
        var count = ReadBoundedCount(reader, DictionaryPackageFormat.MaxCandidates);
        if (count != expectedCount)
        {
            throw new InvalidDataException(DictionaryResources.InvalidPackageCount);
        }

        var entries = new ShapeDictionaryEntry[count];
        for (var index = 0; index < entries.Length; index++)
        {
            var text = ReadString(reader);
            var shapeCode = ReadString(reader);
            var decomposition = ReadString(reader);
            if (text.Length == 0 || shapeCode.Length == 0 || decomposition.Length == 0)
            {
                throw new InvalidDataException(DictionaryResources.InvalidCandidateData);
            }

            entries[index] = new ShapeDictionaryEntry(text, shapeCode, decomposition);
        }

        EnsureEndOfStream(stream);
        return entries;
    }

    private static Dictionary<string, IReadOnlyList<ImeCandidate>> ReadSymbolEntries(string path, int expectedCount)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        ValidateHeader(reader, DictionaryPackageFormat.SymbolsMagic);
        var count = ReadBoundedCount(reader, DictionaryPackageFormat.MaxIndexKeys);
        if (count != expectedCount)
        {
            throw new InvalidDataException(DictionaryResources.InvalidPackageCount);
        }

        var entries = new Dictionary<string, IReadOnlyList<ImeCandidate>>(count, StringComparer.Ordinal);
        for (var index = 0; index < count; index++)
        {
            var input = ReadString(reader);
            var candidateCount = ReadBoundedCount(reader, DictionaryPackageFormat.MaxCandidatesPerKey);
            if (input.Length == 0 || candidateCount == 0)
            {
                throw new InvalidDataException(DictionaryResources.InvalidCandidateData);
            }

            var candidates = new ImeCandidate[candidateCount];
            for (var candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                var text = ReadString(reader);
                if (text.Length == 0)
                {
                    throw new InvalidDataException(DictionaryResources.InvalidCandidateData);
                }

                candidates[candidateIndex] = new ImeCandidate(text, input, candidateCount - candidateIndex);
            }

            if (!entries.TryAdd(input, candidates))
            {
                throw new InvalidDataException(DictionaryResources.InvalidIndexData);
            }
        }

        EnsureEndOfStream(stream);
        return entries;
    }

    private static void ValidateHeader(BinaryReader reader, ReadOnlySpan<byte> expectedMagic)
    {
        var magic = reader.ReadBytes(expectedMagic.Length);
        if (!magic.AsSpan().SequenceEqual(expectedMagic))
        {
            throw new InvalidDataException(DictionaryResources.InvalidShardMagic);
        }

        if (reader.ReadInt32() != DictionaryPackageFormat.ShardVersion)
        {
            throw new InvalidDataException(DictionaryResources.UnsupportedShardVersion);
        }
    }

    private static int ReadBoundedCount(BinaryReader reader, int maximum)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > maximum)
        {
            throw new InvalidDataException(DictionaryResources.PackageCountTooLarge);
        }

        return count;
    }

    private static string ReadString(BinaryReader reader)
    {
        var byteCount = ReadBoundedCount(reader, DictionaryPackageFormat.MaxStringUtf8Bytes);
        var bytes = reader.ReadBytes(byteCount);
        if (bytes.Length != byteCount)
        {
            throw new EndOfStreamException();
        }

        return new UTF8Encoding(false, true).GetString(bytes);
    }

    private static void EnsureEndOfStream(Stream stream)
    {
        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException(DictionaryResources.InvalidShardLength);
        }
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path))
        {
            return false;
        }

        return !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "" or "." or "..");
    }
}
