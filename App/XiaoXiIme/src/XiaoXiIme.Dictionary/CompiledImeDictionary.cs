using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Provides synchronous in-memory queries over a loaded dictionary package.
/// </summary>
public sealed class CompiledImeDictionary : IImeDictionary, IShapeDictionary, ISymbolDictionary
{
    private ImeLexicon? _lexicon;
    private readonly ImeCandidate[] _candidates;
    private readonly IReadOnlyDictionary<string, int[]> _exactIndex;
    private readonly IReadOnlyDictionary<string, int[]> _prefixIndex;
    private readonly ShapeDictionaryEntry[] _shapeEntries;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ImeCandidate>> _symbols;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _shapeCodesByText;
    private readonly IReadOnlyDictionary<string, int[]> _textIndex;

    internal CompiledImeDictionary(
        ImeCandidate[] candidates,
        IReadOnlyDictionary<string, int[]> exactIndex,
        IReadOnlyDictionary<string, int[]> prefixIndex,
        ShapeDictionaryEntry[] shapeEntries,
        IReadOnlyDictionary<string, IReadOnlyList<ImeCandidate>> symbols,
        string inputScheme, ImeLexicon? lexicon = null)
    {
        _lexicon = lexicon;
        _candidates = candidates;
        _exactIndex = exactIndex;
        _prefixIndex = prefixIndex;
        _shapeEntries = shapeEntries;
        _symbols = symbols;
        InputScheme = inputScheme;
        _shapeCodesByText = shapeEntries
            .GroupBy(entry => entry.Text, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(entry => entry.ShapeCode).ToArray(),
                StringComparer.Ordinal);
        _textIndex = candidates
            .Select((candidate, index) => (candidate.Text, index))
            .GroupBy(entry => entry.Text, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.index).ToArray(),
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the encoding space recorded by the compiled package.
    /// </summary>
    public string InputScheme { get; }

    /// <inheritdoc />
    public ImeProjectionDefinition Projection => _lexicon?.Projection ?? ImeProjectionDefinition.ForScheme(InputScheme);

    /// <inheritdoc />
    public ImeLexicon GetLexicon()
    {
        if (_lexicon is not null) return _lexicon;
        var projection = ImeProjectionDefinition.ForScheme(InputScheme);
        var syllables = projection.Entries.Select(p => p.Initial + p.Final).ToHashSet(StringComparer.Ordinal);
        var entries = new List<ImePhoneticEntry>();
        var aliases = new List<ImeLookupAlias>();
        foreach (var candidate in _candidates)
        {
            // This first-party abbreviation is registered alongside its canonical reading in the term source.
            if (candidate.Text == "小希" && candidate.Reading == "xx")
            {
                if (!_candidates.Any(c => c.Text == "小希" && c.Reading == "xiao xi"))
                    entries.Add(new("小希", "xiao xi", candidate.Score));
                aliases.Add(new("小希", "xiao xi", "xx"));
                continue;
            }
            var reading = PhoneticInputProjection.CanonicalizeReading(candidate.Reading);
            if (!ImeLexiconValidator.IsReading(reading) || reading.Split(' ').Any(s => !syllables.Contains(s)))
                throw new NotSupportedException(DictionaryResources.InvalidReading);
            entries.Add(new(candidate.Text, reading, candidate.Score));
        }
        return _lexicon = new InMemoryImeDictionary(new ImeLexicon(entries, _shapeEntries, projection, 0)
        { LookupAliases = aliases }).GetLexicon();
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.Input) || query.MaxCount <= 0)
        {
            return [];
        }

        var input = DictionaryPackageFormat.NormalizeLookupKey(query.Input);
        var ranked = new List<DictionaryCandidate>(Math.Min(query.MaxCount, 32));
        var seen = new HashSet<int>();
        AddCandidates(_exactIndex, input, DictionaryCandidateMatchKind.Exact, ranked, seen);
        if (query.MatchMode == ImeDictionaryMatchMode.ExactAndPrefix)
        {
            AddCandidates(_prefixIndex, input, DictionaryCandidateMatchKind.Prefix, ranked, seen);
        }

        return DictionaryCandidateRanking.Rank(ranked, query.MaxCount);
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> QueryByText(string text, int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text) || maxCount <= 0)
        {
            return [];
        }

        if (!_textIndex.TryGetValue(text, out var candidateIds))
        {
            return [];
        }

        var ranked = new List<DictionaryCandidate>(candidateIds.Length);
        foreach (var candidateId in candidateIds)
        {
            var candidate = _candidates[candidateId];
            ranked.Add(new DictionaryCandidate(
                candidate.Text,
                candidate.Reading,
                candidate.Score,
                DictionaryCandidateSourceKind.System,
                DictionaryCandidateMatchKind.Exact));
        }

        return DictionaryCandidateRanking.RankReadings(ranked, maxCount);
    }

    /// <inheritdoc />
    public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9)
    {
        if (string.IsNullOrWhiteSpace(shapeCode) || maxCount <= 0)
        {
            return [];
        }

        var normalizedCode = shapeCode.Trim().ToLowerInvariant();
        return _shapeEntries
            .Where(entry => entry.ShapeCode.StartsWith(normalizedCode, StringComparison.Ordinal))
            .Take(maxCount)
            .ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> FilterByShape(
        IReadOnlyList<ImeCandidate> candidates,
        string shapeCode,
        int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (string.IsNullOrWhiteSpace(shapeCode) || maxCount <= 0)
        {
            return [];
        }

        var normalizedCode = shapeCode.Trim().ToLowerInvariant();
        return candidates
            .Where(candidate => _shapeCodesByText.TryGetValue(candidate.Text, out var codes)
                && codes.Any(code => code.StartsWith(normalizedCode, StringComparison.Ordinal)))
            .Take(maxCount)
            .ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> QuerySymbols(string input, int maxCount = 9)
    {
        if (string.IsNullOrWhiteSpace(input) || maxCount <= 0)
        {
            return [];
        }

        var normalizedInput = input.Trim().ToLowerInvariant();
        return _symbols.TryGetValue(normalizedInput, out var candidates)
            ? candidates.Take(maxCount).ToArray()
            : [];
    }

    private void AddCandidates(
        IReadOnlyDictionary<string, int[]> index,
        string input,
        DictionaryCandidateMatchKind matchKind,
        List<DictionaryCandidate> result,
        HashSet<int> seen)
    {
        if (!index.TryGetValue(input, out var candidateIds))
        {
            return;
        }

        foreach (var candidateId in candidateIds)
        {
            if (!seen.Add(candidateId))
            {
                continue;
            }

            var candidate = _candidates[candidateId];
            result.Add(new DictionaryCandidate(
                candidate.Text,
                candidate.Reading,
                candidate.Score,
                DictionaryCandidateSourceKind.System,
                matchKind));
        }
    }
}
