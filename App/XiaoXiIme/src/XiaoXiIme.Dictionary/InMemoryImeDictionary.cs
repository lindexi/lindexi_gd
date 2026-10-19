using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

public sealed class InMemoryImeDictionary : IImeDictionary, IShapeDictionary
{
    private readonly ImeLexicon? _lexicon;

    /// <summary>Creates a dictionary from complete canonical phonetic and shape data.</summary>
    public InMemoryImeDictionary(ImeLexicon lexicon)
        : this(CreateCandidates(lexicon))
    {
        _lexicon = lexicon with
        {
            Entries = Array.AsReadOnly(lexicon.Entries.GroupBy(e => (e.Text, e.CanonicalReading))
                .Select(g => g.OrderBy(e => e.SourceKind).ThenByDescending(e => e.BaseFrequency).First()).ToArray()),
            ShapeEntries = Array.AsReadOnly(lexicon.ShapeEntries.DistinctBy(e => (e.Text, e.ShapeCode)).ToArray()),
            LookupAliases = Array.AsReadOnly(lexicon.LookupAliases.ToArray()),
        };
    }

    private static IEnumerable<ImeCandidate> CreateCandidates(ImeLexicon lexicon)
    {
        ImeLexiconValidator.Validate(lexicon);
        return lexicon.Entries.Select(e => new ImeCandidate(e.Text, e.CanonicalReading, e.BaseFrequency));
    }

    /// <inheritdoc />
    public ImeProjectionDefinition? Projection => _lexicon?.Projection;

    /// <inheritdoc />
    public ImeLexicon GetLexicon() => _lexicon ?? throw new NotSupportedException();

    /// <inheritdoc />
    public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(shapeCode);
        return (_lexicon?.ShapeEntries ?? []).Where(e => e.ShapeCode.StartsWith(shapeCode.ToLowerInvariant(), StringComparison.Ordinal))
            .Take(Math.Max(0, maxCount)).ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> FilterByShape(IReadOnlyList<ImeCandidate> candidates, string shapeCode, int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(shapeCode);
        return candidates.Where(c => QueryShape(shapeCode, int.MaxValue).Any(s => c.Text.Contains(s.Text, StringComparison.Ordinal)))
            .Take(Math.Max(0, maxCount)).ToArray();
    }

    private readonly Dictionary<string, List<ImeCandidate>> _entries;
    private readonly Dictionary<string, List<ImeCandidate>> _prefixEntries;

    /// <summary>Creates a lookup-only dictionary. Engine use requires the canonical ImeLexicon constructor.</summary>
    public InMemoryImeDictionary(IEnumerable<ImeCandidate> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _entries = entries
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Reading) && !string.IsNullOrWhiteSpace(candidate.Text))
            .GroupBy(candidate => DictionaryPackageFormat.NormalizeLookupKey(candidate.Reading), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(candidate => candidate)
                    .ToList(),
                StringComparer.Ordinal);
        _prefixEntries = new(StringComparer.Ordinal);
        foreach (var (key, candidates) in _entries)
        {
            for (var length = 1; length < key.Length; length++)
            {
                var prefix = key[..length];
                if (!_prefixEntries.TryGetValue(prefix, out var matches))
                    _prefixEntries.Add(prefix, matches = []);
                matches.AddRange(candidates);
            }
        }
    }

    public static InMemoryImeDictionary CreateDefault() => CreateMinimalFallback();

    /// <summary>
    /// Creates the small built-in dictionary used only when the production dictionary is unavailable.
    /// </summary>
    public static InMemoryImeDictionary CreateMinimalFallback() => new(new ImeLexicon(
    [
        new("你", "ni", 100),
        new("呢", "ni", 60),
        new("好", "hao", 100),
        new("号", "hao", 60),
        new("小希", "xiao xi", 100),
        new("输入法", "shu ru fa", 100),
    ], [], ImeProjectionDefinition.FullPinyin, 1)
    {
        LookupAliases = [new("小希", "xiao xi", "xx")]
    });

    public IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return Query(new ImeDictionaryQuery(reading, maxCount));
    }

    public IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.Input) || query.MaxCount <= 0)
        {
            return [];
        }

        var input = DictionaryPackageFormat.NormalizeLookupKey(query.Input);
        var ranked = new List<DictionaryCandidate>();
        if (_entries.TryGetValue(input, out var exactCandidates))
        {
            ranked.AddRange(exactCandidates.Select(candidate => ToRanked(
                candidate,
                DictionaryCandidateMatchKind.Exact)));
        }

        if (query.MatchMode == ImeDictionaryMatchMode.ExactAndPrefix &&
            _prefixEntries.TryGetValue(input, out var prefixCandidates))
        {
            ranked.AddRange(prefixCandidates.Select(candidate => ToRanked(candidate, DictionaryCandidateMatchKind.Prefix)));
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

        var ranked = _entries.Values
            .SelectMany(candidates => candidates)
            .Where(candidate => string.Equals(candidate.Text, text, StringComparison.Ordinal))
            .Select(candidate => ToRanked(candidate, DictionaryCandidateMatchKind.Exact));
        return DictionaryCandidateRanking.RankReadings(ranked, maxCount);
    }

    private static DictionaryCandidate ToRanked(ImeCandidate candidate, DictionaryCandidateMatchKind matchKind)
    {
        return new DictionaryCandidate(
            candidate.Text,
            candidate.Reading,
            candidate.Score,
            DictionaryCandidateSourceKind.Fallback,
            matchKind);
    }
}
