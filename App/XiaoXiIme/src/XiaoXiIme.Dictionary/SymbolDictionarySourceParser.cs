namespace XiaoXiIme.Dictionary;

/// <summary>
/// Parses XiaoXiIme symbol TSV source files into normalized entries.
/// </summary>
public static class SymbolDictionarySourceParser
{
    public const int MaxLineLength = 16 * 1024;
    public const int MaxInputLength = 64;
    public const int MaxCandidateLength = 256;
    public const int MaxCandidatesPerInput = 1024;

    /// <summary>
    /// Parses, merges, and deterministically sorts symbol source entries.
    /// </summary>
    public static IReadOnlyList<SymbolDictionaryEntry> Parse(TextReader reader, string filePath)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(nameof(filePath), nameof(filePath));
        }

        var entries = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var seenCandidates = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (line.Length > MaxLineLength)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.LineTooLong);
            }

            if (string.IsNullOrWhiteSpace(line) || line.AsSpan().TrimStart().StartsWith(ImeDictionarySourceFormat.CommentMarker))
            {
                continue;
            }

            var columns = line.Split(ImeDictionarySourceFormat.ColumnSeparator);
            if (columns.Length < 2)
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidSymbolColumnCount);
            }

            var input = columns[0].Trim().ToLowerInvariant();
            if (input.Length is 0 or > MaxInputLength || input.Any(character => character > 0x7F || char.IsWhiteSpace(character)))
            {
                throw CreateException(filePath, lineNumber, DictionaryResources.InvalidSymbolInput);
            }

            if (!entries.TryGetValue(input, out var candidates))
            {
                candidates = [];
                entries.Add(input, candidates);
                seenCandidates.Add(input, new HashSet<string>(StringComparer.Ordinal));
            }

            foreach (var column in columns.Skip(1))
            {
                var candidate = column.Trim();
                if (candidate.Length is 0 or > MaxCandidateLength)
                {
                    throw CreateException(filePath, lineNumber, DictionaryResources.InvalidSymbolCandidate);
                }

                if (seenCandidates[input].Add(candidate))
                {
                    candidates.Add(candidate);
                    if (candidates.Count > MaxCandidatesPerInput)
                    {
                        throw CreateException(filePath, lineNumber, DictionaryResources.PackageCountTooLarge);
                    }
                }
            }
        }

        return entries
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new SymbolDictionaryEntry(entry.Key, entry.Value.ToArray()))
            .ToArray();
    }

    private static DictionarySourceException CreateException(string filePath, int lineNumber, string reason)
    {
        return new DictionarySourceException(filePath, lineNumber, reason);
    }
}
