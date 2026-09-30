using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Stores user selection frequencies and applies them as a ranking layer over a system dictionary.
/// </summary>
public sealed class UserDictionary : IImeDictionary, IShapeDictionary, ISymbolDictionary
{
    private readonly IImeDictionary _systemDictionary;
    private readonly Dictionary<UserDictionaryKey, int> _frequencies;
    private readonly object _syncRoot = new();

    public UserDictionary(IImeDictionary systemDictionary, IEnumerable<UserDictionaryEntry>? entries = null)
    {
        _systemDictionary = systemDictionary ?? throw new ArgumentNullException(nameof(systemDictionary));
        _frequencies = new Dictionary<UserDictionaryKey, int>();

        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Text) || string.IsNullOrWhiteSpace(entry.Reading) || entry.SelectionCount <= 0)
            {
                continue;
            }

            var key = new UserDictionaryKey(entry.Text, DictionaryPackageFormat.NormalizeLookupKey(entry.Reading));
            _frequencies[key] = Math.Max(_frequencies.GetValueOrDefault(key), entry.SelectionCount);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return Query(new ImeDictionaryQuery(reading, maxCount));
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
        var exactSystem = _systemDictionary.Query(new ImeDictionaryQuery(query.Input, query.MaxCount));
        var systemCandidates = query.MatchMode == ImeDictionaryMatchMode.ExactAndPrefix
            ? _systemDictionary.Query(query)
            : exactSystem;
        var exactTexts = exactSystem
            .Select(candidate => candidate.Text)
            .ToHashSet(StringComparer.Ordinal);
        var systemSourceKind = _systemDictionary is InMemoryImeDictionary
            ? DictionaryCandidateSourceKind.Fallback
            : DictionaryCandidateSourceKind.System;
        lock (_syncRoot)
        {
            var ranked = new List<DictionaryCandidate>(systemCandidates.Count + 4);
            foreach (var candidate in systemCandidates)
            {
                ranked.Add(new DictionaryCandidate(
                    candidate.Text,
                    candidate.Reading,
                    candidate.Score,
                    systemSourceKind,
                    exactTexts.Contains(candidate.Text)
                        ? DictionaryCandidateMatchKind.Exact
                        : DictionaryCandidateMatchKind.Prefix,
                    _frequencies.GetValueOrDefault(new UserDictionaryKey(candidate.Text, input))));
            }

            foreach (var entry in _frequencies)
            {
                if (!string.Equals(entry.Key.Reading, input, StringComparison.Ordinal))
                {
                    continue;
                }

                ranked.Add(new DictionaryCandidate(
                    entry.Key.Text,
                    entry.Key.Reading,
                    BaseFrequency: 0,
                    DictionaryCandidateSourceKind.User,
                    DictionaryCandidateMatchKind.Exact,
                    entry.Value));
            }

            return DictionaryCandidateRanking.Rank(ranked, query.MaxCount);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> QueryByText(string text, int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text) || maxCount <= 0)
        {
            return [];
        }

        var systemCandidates = _systemDictionary.QueryByText(text, maxCount);
        var systemSourceKind = _systemDictionary is InMemoryImeDictionary
            ? DictionaryCandidateSourceKind.Fallback
            : DictionaryCandidateSourceKind.System;
        lock (_syncRoot)
        {
            var ranked = new List<DictionaryCandidate>(systemCandidates.Count + 4);
            foreach (var candidate in systemCandidates)
            {
                ranked.Add(new DictionaryCandidate(
                    candidate.Text,
                    candidate.Reading,
                    candidate.Score,
                    systemSourceKind,
                    DictionaryCandidateMatchKind.Exact,
                    _frequencies.GetValueOrDefault(new UserDictionaryKey(
                        candidate.Text,
                        DictionaryPackageFormat.NormalizeLookupKey(candidate.Reading)))));
            }

            foreach (var entry in _frequencies)
            {
                if (!string.Equals(entry.Key.Text, text, StringComparison.Ordinal))
                {
                    continue;
                }

                ranked.Add(new DictionaryCandidate(
                    entry.Key.Text,
                    entry.Key.Reading,
                    BaseFrequency: 0,
                    DictionaryCandidateSourceKind.User,
                    DictionaryCandidateMatchKind.Exact,
                    entry.Value));
            }

            return DictionaryCandidateRanking.RankReadings(ranked, maxCount);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9)
    {
        return _systemDictionary is IShapeDictionary shapeDictionary
            ? shapeDictionary.QueryShape(shapeCode, maxCount)
            : [];
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> FilterByShape(
        IReadOnlyList<ImeCandidate> candidates,
        string shapeCode,
        int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return _systemDictionary is IShapeDictionary shapeDictionary
            ? shapeDictionary.FilterByShape(candidates, shapeCode, maxCount)
            : [];
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> QuerySymbols(string input, int maxCount = 9)
    {
        return _systemDictionary is ISymbolDictionary symbolDictionary
            ? symbolDictionary.QuerySymbols(input, maxCount)
            : [];
    }

    /// <summary>
    /// Records a user selection and returns its updated selection count.
    /// </summary>
    public int Learn(ImeDictionaryLearning learning)
    {
        ArgumentNullException.ThrowIfNull(learning);
        ArgumentNullException.ThrowIfNull(learning.Candidate);
        if (string.IsNullOrWhiteSpace(learning.Candidate.Text))
        {
            throw new ArgumentException(DictionaryResources.EmptyText, nameof(learning));
        }

        if (string.IsNullOrWhiteSpace(learning.OriginalInput))
        {
            throw new ArgumentException(DictionaryResources.EmptyReading, nameof(learning));
        }

        var key = new UserDictionaryKey(
            learning.Candidate.Text,
            DictionaryPackageFormat.NormalizeLookupKey(learning.OriginalInput));
        lock (_syncRoot)
        {
            var current = _frequencies.GetValueOrDefault(key);
            var updated = current == int.MaxValue ? int.MaxValue : current + 1;
            _frequencies[key] = updated;
            return updated;
        }
    }

    /// <summary>
    /// Removes a previously learned user word.
    /// </summary>
    public bool Forget(string text, string reading)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(reading))
        {
            return false;
        }

        var key = new UserDictionaryKey(text, DictionaryPackageFormat.NormalizeLookupKey(reading));
        lock (_syncRoot)
        {
            return _frequencies.Remove(key);
        }
    }

    /// <summary>
    /// Returns a stable snapshot suitable for persistence.
    /// </summary>
    public IReadOnlyList<UserDictionaryEntry> GetEntries(string? reading = null, string? text = null)
    {
        var normalizedReading = string.IsNullOrWhiteSpace(reading)
            ? null
            : DictionaryPackageFormat.NormalizeLookupKey(reading);
        lock (_syncRoot)
        {
            return _frequencies
                .Where(entry =>
                    (normalizedReading is null || string.Equals(entry.Key.Reading, normalizedReading, StringComparison.Ordinal))
                    && (string.IsNullOrWhiteSpace(text) || string.Equals(entry.Key.Text, text, StringComparison.Ordinal)))
                .OrderBy(entry => entry.Key.Reading, StringComparer.Ordinal)
                .ThenBy(entry => entry.Key.Text, StringComparer.Ordinal)
                .Select(entry => new UserDictionaryEntry(entry.Key.Text, entry.Key.Reading, entry.Value))
                .ToArray();
        }
    }

    private readonly record struct UserDictionaryKey(string Text, string Reading);
}

/// <summary>
/// Represents one persisted user dictionary entry.
/// </summary>
public sealed record UserDictionaryEntry(string Text, string Reading, int SelectionCount);
