using System.Text;
using System.Text.Json;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Compiles normalized phonetic entries into a XiaoXiIme runtime dictionary package.
/// </summary>
public static class DictionaryPackageCompiler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Compiles entries into a package directory and returns the written manifest.
    /// </summary>
    public static DictionaryPackageManifest Compile(
        IEnumerable<PhoneticDictionaryEntry> entries,
        string outputDirectory,
        DictionaryPackageParameters? parameters = null,
        IReadOnlyList<DictionaryPackageSource>? sources = null,
        IEnumerable<ShapeDictionaryEntry>? shapeEntries = null,
        IEnumerable<SymbolDictionaryEntry>? symbolEntries = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException(nameof(outputDirectory), nameof(outputDirectory));
        }

        parameters ??= new DictionaryPackageParameters();
        ValidateParameters(parameters);
        var normalizedEntries = NormalizeEntries(entries);
        var lookupKeys = BuildLookupKeys(normalizedEntries, parameters.InputScheme);
        var exactIndex = BuildExactIndex(lookupKeys);
        var prefixIndex = parameters.EnablePrefixIndex
            ? BuildPrefixIndex(normalizedEntries, lookupKeys, parameters.MaxPrefixCandidatesPerKey)
            : new SortedDictionary<string, int[]>(StringComparer.Ordinal);
        var normalizedShapeEntries = NormalizeShapeEntries(shapeEntries ?? []);
        var normalizedSymbolEntries = NormalizeSymbolEntries(symbolEntries ?? []);

        Directory.CreateDirectory(outputDirectory);
        var candidatesPath = Path.Combine(outputDirectory, DictionaryPackageFormat.CandidatesFileName);
        var exactIndexPath = Path.Combine(outputDirectory, DictionaryPackageFormat.ExactIndexFileName);
        var prefixIndexPath = Path.Combine(outputDirectory, DictionaryPackageFormat.PrefixIndexFileName);
        var shapeIndexPath = Path.Combine(outputDirectory, DictionaryPackageFormat.ShapeIndexFileName);
        var symbolsPath = Path.Combine(outputDirectory, DictionaryPackageFormat.SymbolsFileName);

        WriteCandidates(candidatesPath, normalizedEntries);
        WriteIndex(exactIndexPath, DictionaryPackageFormat.ExactIndexMagic, exactIndex);
        WriteIndex(prefixIndexPath, DictionaryPackageFormat.PrefixIndexMagic, prefixIndex);
        WriteShapeEntries(shapeIndexPath, normalizedShapeEntries);
        WriteSymbolEntries(symbolsPath, normalizedSymbolEntries);

        var shards = new[]
        {
            CreateShard(DictionaryPackageFormat.CandidatesRole, candidatesPath),
            CreateShard(DictionaryPackageFormat.ExactIndexRole, exactIndexPath),
            CreateShard(DictionaryPackageFormat.PrefixIndexRole, prefixIndexPath),
            CreateShard(DictionaryPackageFormat.ShapeIndexRole, shapeIndexPath),
            CreateShard(DictionaryPackageFormat.SymbolsRole, symbolsPath),
        };
        var manifest = new DictionaryPackageManifest
        {
            CreatedBy = "XiaoXiIme.DictionaryCompiler",
            CompilerVersion = DictionaryPackageManifest.CurrentCompilerVersion,
            Sources = sources ?? [],
            Parameters = parameters,
            Shards = shards,
            Counts = new DictionaryPackageCounts
            {
                Candidates = normalizedEntries.Count,
                ExactKeys = exactIndex.Count,
                PrefixKeys = prefixIndex.Count,
                ShapeEntries = normalizedShapeEntries.Count,
                SymbolInputs = normalizedSymbolEntries.Count,
            },
        };

        var manifestJson = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(
            Path.Combine(outputDirectory, DictionaryPackageFormat.ManifestFileName),
            manifestJson,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return manifest;
    }

    private static IReadOnlyList<PhoneticDictionaryEntry> NormalizeEntries(IEnumerable<PhoneticDictionaryEntry> entries)
    {
        var mergedEntries = new Dictionary<(string Text, string Reading), int>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (string.IsNullOrWhiteSpace(entry.Text) || string.IsNullOrWhiteSpace(entry.Reading) || entry.Frequency < 0)
            {
                throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(entries));
            }

            var text = entry.Text.Trim();
            var reading = DictionaryPackageFormat.NormalizeInput(entry.Reading);
            if (Encoding.UTF8.GetByteCount(text) > DictionaryPackageFormat.MaxStringUtf8Bytes
                || Encoding.UTF8.GetByteCount(reading) > DictionaryPackageFormat.MaxStringUtf8Bytes)
            {
                throw new ArgumentException(DictionaryResources.PackageStringTooLong, nameof(entries));
            }

            var key = (text, reading);
            if (!mergedEntries.TryGetValue(key, out var frequency) || entry.Frequency > frequency)
            {
                mergedEntries[key] = entry.Frequency;
            }
        }

        if (mergedEntries.Count > DictionaryPackageFormat.MaxCandidates)
        {
            throw new ArgumentException(DictionaryResources.PackageCountTooLarge, nameof(entries));
        }

        return mergedEntries
            .Select(item => new PhoneticDictionaryEntry(item.Key.Text, item.Key.Reading, item.Value))
            .OrderBy(item => item.Reading, StringComparer.Ordinal)
            .ThenByDescending(item => item.Frequency)
            .ThenBy(item => item.Text, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] BuildLookupKeys(
        IReadOnlyList<PhoneticDictionaryEntry> entries,
        string inputScheme)
    {
        var lookupKeys = new string[entries.Count];
        for (var id = 0; id < entries.Count; id++)
        {
            if (!PhoneticInputProjection.TryProjectReading(entries[id].Reading, inputScheme, out lookupKeys[id]))
            {
                throw new ArgumentException(
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        DictionaryResources.UnsupportedInputSchemeReading,
                        entries[id].Reading,
                        inputScheme),
                    nameof(entries));
            }
        }

        return lookupKeys;
    }

    private static SortedDictionary<string, int[]> BuildExactIndex(IReadOnlyList<string> lookupKeys)
    {
        return new SortedDictionary<string, int[]>(
            lookupKeys.Select((lookupKey, id) => (lookupKey, id))
                .GroupBy(item => item.lookupKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Select(item => item.id).ToArray(), StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    private static SortedDictionary<string, int[]> BuildPrefixIndex(
        IReadOnlyList<PhoneticDictionaryEntry> entries,
        IReadOnlyList<string> lookupKeys,
        int maxCandidatesPerKey)
    {
        var prefixes = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var id = 0; id < entries.Count; id++)
        {
            var reading = lookupKeys[id];
            for (var length = 1; length < reading.Length; length++)
            {
                var prefix = reading[..length];

                if (!prefixes.TryGetValue(prefix, out var candidateIds))
                {
                    candidateIds = [];
                    prefixes.Add(prefix, candidateIds);
                }

                candidateIds.Add(id);
            }
        }

        return new SortedDictionary<string, int[]>(
            prefixes.ToDictionary(
                item => item.Key,
                item => item.Value
                    .OrderByDescending(id => entries[id].Frequency)
                    .ThenBy(id => lookupKeys[id].Length)
                    .ThenBy(id => entries[id].Text.Length)
                    .ThenBy(id => entries[id].Text, StringComparer.Ordinal)
                    .Take(maxCandidatesPerKey)
                    .ToArray(),
                StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    private static IReadOnlyList<ShapeDictionaryEntry> NormalizeShapeEntries(IEnumerable<ShapeDictionaryEntry> entries)
    {
        var normalized = new Dictionary<(string Text, string ShapeCode), string>();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var text = entry.Text.Trim();
            var shapeCode = entry.ShapeCode.Trim().ToLowerInvariant();
            var decomposition = entry.Decomposition.Trim();
            if (text.Length == 0 || shapeCode.Length == 0 || decomposition.Length == 0)
            {
                throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(entries));
            }

            normalized[(text, shapeCode)] = decomposition;
        }

        return normalized
            .Select(item => new ShapeDictionaryEntry(item.Key.Text, item.Key.ShapeCode, item.Value))
            .OrderBy(item => item.ShapeCode, StringComparer.Ordinal)
            .ThenBy(item => item.Text, StringComparer.Ordinal)
            .ThenBy(item => item.Decomposition, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<SymbolDictionaryEntry> NormalizeSymbolEntries(IEnumerable<SymbolDictionaryEntry> entries)
    {
        var normalized = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var input = entry.Input.Trim().ToLowerInvariant();
            if (input.Length == 0 || entry.Candidates.Count == 0)
            {
                throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(entries));
            }

            if (!normalized.TryGetValue(input, out var candidates))
            {
                candidates = [];
                normalized.Add(input, candidates);
            }

            foreach (var candidate in entry.Candidates.Select(candidate => candidate.Trim()))
            {
                if (candidate.Length == 0)
                {
                    throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(entries));
                }

                if (!candidates.Contains(candidate, StringComparer.Ordinal))
                {
                    candidates.Add(candidate);
                }
            }
        }

        return normalized
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => new SymbolDictionaryEntry(item.Key, item.Value.ToArray()))
            .ToArray();
    }

    private static void WriteCandidates(string path, IReadOnlyList<PhoneticDictionaryEntry> entries)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        DictionaryPackageFormat.WriteHeader(writer, DictionaryPackageFormat.CandidatesMagic);
        writer.Write(entries.Count);
        foreach (var entry in entries)
        {
            DictionaryPackageFormat.WriteString(writer, entry.Text);
            DictionaryPackageFormat.WriteString(writer, entry.Reading);
            writer.Write(entry.Frequency);
        }
    }

    private static void WriteIndex(string path, ReadOnlySpan<byte> magic, IReadOnlyDictionary<string, int[]> index)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        DictionaryPackageFormat.WriteHeader(writer, magic);
        writer.Write(index.Count);
        foreach (var item in index)
        {
            DictionaryPackageFormat.WriteString(writer, item.Key);
            writer.Write(item.Value.Length);
            foreach (var candidateId in item.Value)
            {
                writer.Write(candidateId);
            }
        }
    }

    private static void WriteShapeEntries(string path, IReadOnlyList<ShapeDictionaryEntry> entries)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        DictionaryPackageFormat.WriteHeader(writer, DictionaryPackageFormat.ShapeIndexMagic);
        writer.Write(entries.Count);
        foreach (var entry in entries)
        {
            DictionaryPackageFormat.WriteString(writer, entry.Text);
            DictionaryPackageFormat.WriteString(writer, entry.ShapeCode);
            DictionaryPackageFormat.WriteString(writer, entry.Decomposition);
        }
    }

    private static void WriteSymbolEntries(string path, IReadOnlyList<SymbolDictionaryEntry> entries)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        DictionaryPackageFormat.WriteHeader(writer, DictionaryPackageFormat.SymbolsMagic);
        writer.Write(entries.Count);
        foreach (var entry in entries)
        {
            DictionaryPackageFormat.WriteString(writer, entry.Input);
            writer.Write(entry.Candidates.Count);
            foreach (var candidate in entry.Candidates)
            {
                DictionaryPackageFormat.WriteString(writer, candidate);
            }
        }
    }

    private static DictionaryPackageShard CreateShard(string role, string path)
    {
        return new DictionaryPackageShard(role, Path.GetFileName(path), new FileInfo(path).Length);
    }

    private static void ValidateParameters(DictionaryPackageParameters parameters)
    {
        if ((!string.Equals(parameters.InputScheme, DictionaryPackageFormat.FullPinyinInputScheme, StringComparison.Ordinal)
                && !string.Equals(parameters.InputScheme, DictionaryPackageFormat.XiaoheDoublePinyinInputScheme, StringComparison.Ordinal))
            || parameters.MaxPrefixCandidatesPerKey <= 0
            || parameters.MaxPrefixCandidatesPerKey > DictionaryPackageFormat.MaxCandidatesPerKey)
        {
            throw new ArgumentException(DictionaryResources.InvalidCompilerParameters, nameof(parameters));
        }
    }
}
