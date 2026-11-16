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
    private long _version;
    private ImeLexicon? _cachedSystemLexicon;
    private ImeLexicon? _cachedLexicon;
    private long _cachedUserVersion;

    /// <inheritdoc />
    public ImeProjectionDefinition? Projection => _systemDictionary.Projection;

    /// <inheritdoc />
    public ImeLexicon GetLexicon() => Volatile.Read(ref _publishedLexicon) ?? BuildLexicon();

    private ImeLexicon? _publishedLexicon;
    private Action<ImeLexicon>? _preparePublication;

    /// <summary>Gets whether input reads only prepared snapshots.</summary>
    public bool IsBackgroundPublicationEnabled => _preparePublication is not null;

    /// <summary>Enables prepared snapshot publication before input sessions are created.</summary>
    public void EnableBackgroundPublication(Action<ImeLexicon> preparePublication)
    {
        ArgumentNullException.ThrowIfNull(preparePublication);
        lock (_publicationRoot)
        {
            _preparePublication = preparePublication;
            PrepareAndPublishCore();
        }
    }

    private readonly object _publicationRoot = new();

    /// <summary>Builds ranking data and prepares all required derived data before atomic publication.</summary>
    public void PrepareAndPublish()
    {
        lock (_publicationRoot) PrepareAndPublishCore();
    }

    private void PrepareAndPublishCore()
    {
        var system = _systemDictionary.GetLexicon();
        ImeLexicon next;
        lock (_syncRoot)
        {
            var frequencies = _frequencies.ToDictionary(p => (p.Key.Text,
                PhoneticInputProjection.CanonicalizeReading(p.Key.Reading)), p => p.Value);
            var entries = system.Entries;
            var registered = entries.Select(e => (e.Text, e.CanonicalReading)).ToHashSet();
            var syllables = system.Projection.Entries.Select(p => p.Initial + p.Final).ToHashSet(StringComparer.Ordinal);
            var additions = frequencies.Keys.Where(k => !registered.Contains(k) &&
                k.Item2.Split(' ').All(syllables.Contains)).Select(k => new ImePhoneticEntry(k.Text, k.Item2, 0,
                    DictionaryCandidateSourceKind.User)).ToArray();
            if (additions.Length > 0) entries = Array.AsReadOnly(entries.Concat(additions).ToArray());
            var previous = _publishedLexicon;
            if (previous is not null && previous.Entries.Count == entries.Count &&
                previous.Entries.Select((e, i) => e == entries[i]).All(equal => equal)) entries = previous.Entries;
            next = system with
            {
                Entries = entries,
                Version = unchecked(system.Version + _version),
                UserFrequencies = new System.Collections.ObjectModel.ReadOnlyDictionary<(string, string), int>(frequencies),
                RankingLogFrequencyMass = Math.Log(1d + entries.Sum(e => 1d + Math.Min(int.MaxValue,
                    (long)e.BaseFrequency + frequencies.GetValueOrDefault((e.Text, e.CanonicalReading), e.UserFrequency)))),
            };
        }
        _preparePublication?.Invoke(next);
        Volatile.Write(ref _publishedLexicon, next);
    }

    /// <summary>Applies a bounded batch of already confirmed canonical selection counts.</summary>
    public void LearnBatch(IReadOnlyList<UserDictionaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        lock (_syncRoot)
        {
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Text) || string.IsNullOrWhiteSpace(entry.Reading) || entry.SelectionCount <= 0)
                    throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(entries));
                var key = new UserDictionaryKey(entry.Text, DictionaryPackageFormat.NormalizeLookupKey(entry.Reading));
                _frequencies[key] = (int)Math.Min(int.MaxValue, (long)_frequencies.GetValueOrDefault(key) + entry.SelectionCount);
            }
            _version++;
        }
    }

    private ImeLexicon BuildLexicon()
    {
        var system = _systemDictionary.GetLexicon();
        lock (_syncRoot)
        {
            if (ReferenceEquals(system, _cachedSystemLexicon) && _cachedUserVersion == _version && _cachedLexicon is not null)
                return _cachedLexicon;
            _cachedSystemLexicon = system;
            _cachedUserVersion = _version;
            return _cachedLexicon = system with
            {
                Version = unchecked(system.Version + _version),
                Entries = Array.AsReadOnly(system.Entries.Select(e => e with
                {
                    SourceKind = _frequencies.GetValueOrDefault(new UserDictionaryKey(e.Text,
                        DictionaryPackageFormat.NormalizeLookupKey(e.CanonicalReading))) > 0
                        ? DictionaryCandidateSourceKind.User : e.SourceKind,
                    UserFrequency = _frequencies.GetValueOrDefault(new UserDictionaryKey(e.Text,
                        DictionaryPackageFormat.NormalizeLookupKey(e.CanonicalReading)), e.UserFrequency),
                }).Concat(_frequencies.Where(pair =>
                    PhoneticInputProjection.CanonicalizeReading(pair.Key.Reading).Split(' ').All(syllable =>
                        system.Projection.Entries.Any(p => p.Initial + p.Final == syllable)) &&
                    !system.Entries.Any(e => e.Text == pair.Key.Text &&
                    DictionaryPackageFormat.NormalizeLookupKey(e.CanonicalReading) == pair.Key.Reading))
                    .Select(pair => new ImePhoneticEntry(pair.Key.Text,
                        PhoneticInputProjection.CanonicalizeReading(pair.Key.Reading), 0,
                        DictionaryCandidateSourceKind.User, pair.Value))).ToArray()),
            };
        }
    }

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

        ArgumentNullException.ThrowIfNull(learning.Segments);
        if (learning.Segments.Count > 0)
        {
            var keys = new List<UserDictionaryKey>();
            var registered = _systemDictionary.GetLexicon().Entries;
            var outputEnd = 0;
            var inputEnd = 0;
            foreach (var segment in learning.Segments)
            {
                if (segment is null || string.IsNullOrWhiteSpace(segment.Text) ||
                    string.IsNullOrWhiteSpace(segment.CanonicalReading) ||
                    !registered.Any(e => e.Text == segment.Text && e.CanonicalReading == segment.CanonicalReading) ||
                    segment.InputStart != inputEnd || segment.InputLength <= 0 ||
                    segment.InputStart > learning.OriginalInput.Length - segment.InputLength ||
                    segment.OutputStart != outputEnd || segment.OutputLength != segment.Text.Length ||
                    segment.OutputStart > learning.Candidate.Text.Length - segment.OutputLength ||
                    !learning.Candidate.Text.AsSpan(segment.OutputStart, segment.OutputLength).SequenceEqual(segment.Text))
                    throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(learning));
                keys.Add(new UserDictionaryKey(segment.Text,
                    DictionaryPackageFormat.NormalizeLookupKey(segment.CanonicalReading)));
                inputEnd = segment.InputStart + segment.InputLength;
                outputEnd = segment.OutputStart + segment.OutputLength;
            }
            if (outputEnd != learning.Candidate.Text.Length || inputEnd != learning.OriginalInput.Length)
                throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(learning));
            lock (_syncRoot)
            {
                var result = 0;
                foreach (var segmentKey in keys)
                {
                    var current = _frequencies.GetValueOrDefault(segmentKey);
                    result = current == int.MaxValue ? int.MaxValue : current + 1;
                    _frequencies[segmentKey] = result;
                }
                _version++;
                return result;
            }
        }

        var key = new UserDictionaryKey(
            learning.Candidate.Text,
            DictionaryPackageFormat.NormalizeLookupKey(learning.OriginalInput));
        lock (_syncRoot)
        {
            var current = _frequencies.GetValueOrDefault(key);
            var updated = current == int.MaxValue ? int.MaxValue : current + 1;
            _frequencies[key] = updated;
            _version++;
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
            var removed = _frequencies.Remove(key);
            if (removed) _version++;
            return removed;
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