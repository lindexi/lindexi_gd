using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

public sealed class InMemoryImeDictionary : IImeDictionary
{
    private readonly Dictionary<string, List<ImeCandidate>> _entries;

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
    }

    public static InMemoryImeDictionary CreateDefault() => CreateMinimalFallback();

    /// <summary>
    /// Creates the small built-in dictionary used only when the production dictionary is unavailable.
    /// </summary>
    public static InMemoryImeDictionary CreateMinimalFallback() => new(
    [
        new("你", "ni", 100),
        new("呢", "ni", 60),
        new("好", "hao", 100),
        new("号", "hao", 60),
        new("小希", "xx", 100),
        new("小希", "xiaoxi", 100),
        new("输入法", "shurufa", 100),
    ]);

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

        if (query.MatchMode == ImeDictionaryMatchMode.ExactAndPrefix)
        {
            ranked.AddRange(_entries
                .Where(entry =>
                    !string.Equals(entry.Key, input, StringComparison.Ordinal)
                    && entry.Key.StartsWith(input, StringComparison.Ordinal))
                .SelectMany(entry => entry.Value)
                .Select(candidate => ToRanked(candidate, DictionaryCandidateMatchKind.Prefix)));
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
