using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    private sealed record DecodedPath(string Text, string Reading, int Frequency, int Source,
        ImeInterpretation Path, int Missing, double Weight);

    private List<ImeConversionCandidate> _decodedCandidates = [];
    private List<ImeConversionCandidate> _displayedCandidates = [];
    private ImeLexicon? _displayedLexicon;
    private string? _decodedInput;
    private ImeLexicon? _decodedLexicon;
    private ImeInputOptions? _decodedOptions;
    private IReadOnlyList<ImeFuzzyRule>? _decodedRules;
    private long _decodedRevision = -1;

    private List<ImeConversionCandidate> Decode(string input)
    {
        var lexicon = _dictionary.GetLexicon();
        var rules = _options.FuzzyRules;
        if (_decodedInput == input && ReferenceEquals(_decodedLexicon, lexicon) &&
            ReferenceEquals(_decodedOptions, _options) && ReferenceEquals(_decodedRules, rules) &&
            _decodedRevision == _revision) return _decodedCandidates;
        var options = _options;
        var fullPinyin = lexicon.Projection.Name == ImeProjectionDefinition.FullPinyin.Name;
        _decodingIndex = GetDecodingIndex(lexicon, rules);
        var indexData = _decodingIndex;
        var edges = new Dictionary<int, List<DecodedPath>>();
        long entryVisits = 0, syllableVisits = 0;

        List<DecodedPath> Edges(int start)
        {
            if (edges.TryGetValue(start, out var cached)) return cached;
            var result = new List<DecodedPath>();
            edges.Add(start, result);
            var indices = new HashSet<int>();
            foreach (var pair in indexData.Starts)
            {
                var available = input.Length - start;
                if (Matches(start, pair.Key[..Math.Min(available, pair.Key.Length)], false))
                    indices.UnionWith(pair.Value);
            }
            foreach (var entryIndex in indices.Order())
            {
                var entry = lexicon.Entries[entryIndex];
                entryVisits++;
                var syllables = indexData.Syllables[entry];
                MatchSyllable(0, start, [], 0);
                foreach (var alias in indexData.Aliases[entry])
                    if (Matches(start, alias.Input, false))
                        Finish(start + alias.Input.Length,
                            [new(ImeEvidenceKind.LookupAlias, new(start, alias.Input.Length), 0,
                                input.Substring(start, alias.Input.Length), entry.CanonicalReading, null)], 0);

                void Pending(IReadOnlyList<ImePathEvidence> evidence, int missing)
                {
                    result.Add(new(entry.Text, entry.CanonicalReading, EntryFrequency(entry), (int)entry.SourceKind,
                        new(ImeInterpretationKind.IncompleteShape, input.Length, null, "", [], evidence), missing, 0));
                }
                void Finish(int end, IReadOnlyList<ImePathEvidence> evidence, int missing)
                {
                    var segment = new ImeConfirmedSegment(entry.Text, entry.CanonicalReading, start, end - start, 0, entry.Text.Length);
                    result.Add(new(entry.Text, entry.CanonicalReading, EntryFrequency(entry), (int)entry.SourceKind,
                        new(end == input.Length ? ImeInterpretationKind.Complete : ImeInterpretationKind.Partial,
                            end, new(start, end - start), input[end..], [segment], evidence), missing,
                        Math.Log(1d + Math.Max(0, EntryFrequency(entry))) - indexData.LogFrequencyMass));
                }
                void MatchSyllable(int syllableIndex, int position, IReadOnlyList<ImePathEvidence> evidence, int missing)
                {
                    syllableVisits++;
                    if (syllableIndex == syllables.Length)
                    {
                        Finish(position, evidence, missing);
                        if (options?.Placement is ImeShapePlacement.AfterWord or ImeShapePlacement.Both)
                            foreach (var alias in WordShapes(entry.Text))
                                MatchShape(alias, position, syllableIndex - 1, ImeEvidenceKind.AfterWordShape,
                                    evidence, missing, (end, ev) => Finish(end, ev, missing));
                        return;
                    }
                    if (position == input.Length) return;
                    var syllable = syllables[syllableIndex];
                    foreach (var projection in indexData.Projections[syllable])
                    {
                        if (Matches(position, projection.Input, false))
                            Advance(projection.Input.Length, 0);
                        if (fullPinyin || syllableIndex == syllables.Length - 1)
                        {
                            var limit = Math.Min(projection.Input.Length - 1, input.Length - position);
                            for (var length = 1; length <= limit; length++)
                            {
                                if (!Matches(position, projection.Input[..length], false)) continue;
                                if (!fullPinyin && position + length < input.Length &&
                                    lexicon.Projection.Entries.Any(p => p.Input.Length > length && Matches(position, p.Input, false))) continue;
                                if (fullPinyin)
                                {
                                    if (syllableIndex == syllables.Length - 1 && position + length == input.Length)
                                    {
                                        var phonetic = evidence.Append(new ImePathEvidence(ImeEvidenceKind.Phonetic,
                                            new(position, length), syllableIndex, input.Substring(position, length), syllable, null)).ToArray();
                                        if (projection.Initial + projection.Final != syllable)
                                            phonetic = phonetic.Append(new ImePathEvidence(ImeEvidenceKind.Fuzzy,
                                                new(position, length), syllableIndex, projection.Initial + projection.Final, syllable, null)).ToArray();
                                        if (options?.LengthPolicy == ImeShapeLengthPolicy.RequiredTwo)
                                            Pending(phonetic, missing + projection.Input.Length - length);
                                        else Finish(input.Length, phonetic, missing + projection.Input.Length - length);
                                    }
                                }
                                else Advance(length, projection.Input.Length - length);
                            }
                        }

                        void Advance(int length, int absent)
                        {
                            var phonetic = evidence.Append(new ImePathEvidence(ImeEvidenceKind.Phonetic,
                                new(position, length), syllableIndex, input.Substring(position, length), syllable, null)).ToArray();
                            if (projection.Initial + projection.Final != syllable)
                                phonetic = phonetic.Append(new ImePathEvidence(ImeEvidenceKind.Fuzzy,
                                    new(position, length), syllableIndex, projection.Initial + projection.Final, syllable, null)).ToArray();
                            var next = position + length;
                            if (options?.LengthPolicy != ImeShapeLengthPolicy.RequiredTwo)
                                MatchSyllable(syllableIndex + 1, next, phonetic, missing + absent);
                            if (options?.Placement is ImeShapePlacement.PerCharacter or ImeShapePlacement.Both && syllableIndex < entry.Text.Length)
                                foreach (var alias in indexData.CharacterShapes(entry.Text[syllableIndex].ToString()))
                                    MatchShape(alias, next, syllableIndex, ImeEvidenceKind.PerCharacterShape, phonetic,
                                        missing + absent, (end, ev) => MatchSyllable(syllableIndex + 1, end, ev, missing + absent));
                        }
                    }
                }
                void MatchShape(string alias, int position, int syllableIndex, ImeEvidenceKind kind,
                    IReadOnlyList<ImePathEvidence> evidence, int missing, Action<int, IReadOnlyList<ImePathEvidence>> next)
                {
                    int[] lengths = options?.LengthPolicy switch
                    {
                        ImeShapeLengthPolicy.OptionalOne => [1],
                        ImeShapeLengthPolicy.OptionalTwo or ImeShapeLengthPolicy.RequiredTwo => [2],
                        _ => [1, 2],
                    };
                    foreach (var length in lengths)
                    {
                        if (alias.Length < length) continue;
                        var available = Math.Min(length, input.Length - position);
                        if (!Matches(position, alias[..available], true)) continue;
                        var ev = available == 0 ? evidence : evidence.Append(new ImePathEvidence(kind,
                            new(position, available), syllableIndex, input.Substring(position, available), entry.CanonicalReading, alias)).ToArray();
                        if (available == length) next(position + length, ev);
                        else if (syllableIndex == syllables.Length - 1 &&
                            (available > 0 || options?.LengthPolicy == ImeShapeLengthPolicy.RequiredTwo)) Pending(ev, missing);
                    }
                }
            }
            return result;
        }

        var states = new Dictionary<(int Source, bool Shape), DecodedPath>[input.Length + 1];
        DecodedPath? bestSentence = null;
        var paths = input.Length == 0 ? [] : Edges(0).ToList();
        if (options.EnableStandaloneShape && input.Length > 0 && input.All(char.IsUpper))
        {
            foreach (var shape in lexicon.ShapeEntries.Where(s => s.ShapeCode.StartsWith(input.ToLowerInvariant(), StringComparison.Ordinal)))
                paths.Add(new(shape.Text, shape.ShapeCode, 0, 0,
                    new(ImeInterpretationKind.Complete, input.Length, new(0, input.Length), "", [],
                        [new(ImeEvidenceKind.PerCharacterShape, new(0, input.Length), 0, input, "", shape.ShapeCode)]), 0, 0));
        }
        for (var start = 0; start < input.Length; start++)
        {
            if (start > 0 && states[start] is null) continue;
            IEnumerable<DecodedPath?> prefixes = start == 0 ? [null] : states[start].Values;
            foreach (var prefix in prefixes)
            foreach (var edge in Edges(start))
            {
                if (edge.Path.ConsumedRange is null) continue;
                var path = prefix is null ? edge : Combine(prefix, edge);
                var end = path.Path.CoverageEnd;
                if (end == input.Length && path.Path.Segments.Count > 1 &&
                    (bestSentence is null || Compare(path, bestSentence) < 0))
                    bestSentence = path;
                var state = states[end] ??= [];
                var key = (path.Source, HasShape(path.Path));
                if (!state.TryGetValue(key, out var previous) || Compare(path, previous) < 0)
                    state[key] = path;
            }
        }
        if (bestSentence is not null) paths.Add(bestSentence);
        var orderedGroups = paths.GroupBy(p => (p.Text, p.Reading, p.Path.Kind, p.Path.CoverageEnd,
                p.Path.RemainingInput, Segments: string.Join('|', p.Path.Segments)))
            .Select(g => g.Order(Comparer<DecodedPath>.Create(Compare)).ToArray())
            .OrderBy(g => g[0], Comparer<DecodedPath>.Create(Compare)).ToArray();
        var results = new List<ImeConversionCandidate>();
        for (var ordinal = 0; ordinal < orderedGroups.Length; ordinal++)
        {
            var group = orderedGroups[ordinal];
            var primary = group[0];
            var selectable = primary.Path.ConsumedRange.HasValue;
            results.Add(new(new(primary.Text, primary.Reading, primary.Frequency),
                selectable && input == _reading ? new(_contextId, _revision, ordinal) : null,
                group.Select(p => p.Path).DistinctBy(p => string.Join('|', p.Evidence)).ToArray(),
                new(selectable ? 0 : 1, primary.Path.CoverageEnd, primary.Source, Strategy(primary.Path), primary.Frequency)));
        }
        _lastDecodeMetrics = $"positions={edges.Count}; entryVisits={entryVisits}; syllableVisits={syllableVisits}; paths={paths.Count}; candidates={results.Count}";
        _decodedInput = input;
        _decodedLexicon = lexicon;
        _decodedOptions = _options;
        _decodedRules = rules;
        _decodedRevision = _revision;
        return _decodedCandidates = results;

        int Compare(DecodedPath left, DecodedPath right)
        {
            var compare = (left.Path.ConsumedRange is null).CompareTo(right.Path.ConsumedRange is null);
            if (compare != 0) return compare;
            compare = right.Path.CoverageEnd.CompareTo(left.Path.CoverageEnd);
            if (compare != 0) return compare;
            compare = left.Missing.CompareTo(right.Missing);
            if (compare != 0) return compare;
            compare = left.Source.CompareTo(right.Source);
            if (compare != 0) return compare;
            compare = Strategy(left.Path).CompareTo(Strategy(right.Path));
            if (compare != 0) return compare;
            compare = right.Weight.CompareTo(left.Weight);
            if (compare != 0) return compare;
            compare = left.Path.Segments.Count.CompareTo(right.Path.Segments.Count);
            if (compare != 0) return compare;
            compare = string.CompareOrdinal(left.Reading, right.Reading);
            if (compare != 0) return compare;
            compare = string.CompareOrdinal(left.Text, right.Text);
            if (compare != 0) return compare;
            compare = string.CompareOrdinal(string.Join('|', left.Path.Segments), string.Join('|', right.Path.Segments));
            return compare != 0 ? compare : string.CompareOrdinal(string.Join('|', left.Path.Evidence), string.Join('|', right.Path.Evidence));
        }
        int Strategy(ImeInterpretation path) => options?.SortPolicy switch
        {
            ImeCandidateSortPolicy.PhoneticFirst => HasShape(path) ? 1 : 0,
            ImeCandidateSortPolicy.ShapeFirst => HasShape(path) ? 0 : 1,
            _ => 0,
        };
        IEnumerable<string> WordShapes(string text) => text.Length == 1 ? indexData.CharacterShapes(text) :
            indexData.CharacterShapes(text[..1]).SelectMany(first =>
                indexData.CharacterShapes(text[^1..]).Select(last => first[..1] + last[..1])).Distinct();
        bool Matches(int position, string code, bool shape)
        {
            if (position + code.Length > input.Length) return false;
            for (var i = 0; i < code.Length; i++)
            {
                var c = input[position + i];
                var upper = char.IsUpper(c);
                if (options is not null && (shape
                    ? options.CasePolicy == ImeShapeCasePolicy.UppercaseOnly && !upper || options.CasePolicy == ImeShapeCasePolicy.LowercaseOnly && upper
                    : options.CasePolicy != ImeShapeCasePolicy.LowercaseOnly && upper)) return false;
                if (char.ToLowerInvariant(c) != code[i]) return false;
            }
            return true;
        }
    }

    private static bool HasShape(ImeInterpretation path) => path.Evidence.Any(e =>
        e.Kind is ImeEvidenceKind.PerCharacterShape or ImeEvidenceKind.AfterWordShape);

    private static DecodedPath Combine(DecodedPath head, DecodedPath tail)
    {
        var confirmed = head.Path.Segments.Concat(tail.Path.Segments.Select(s => s with
        {
            OutputStart = s.OutputStart + head.Text.Length
        })).ToArray();
        return new(head.Text + tail.Text, Join(head.Reading, tail.Reading), Math.Min(head.Frequency, tail.Frequency),
            Math.Max(head.Source, tail.Source), tail.Path with
            {
                ConsumedRange = tail.Path.ConsumedRange is null ? null : new(0, tail.Path.CoverageEnd),
                Segments = confirmed,
                Evidence = head.Path.Evidence.Concat(tail.Path.Evidence).ToArray()
            }, head.Missing + tail.Missing, head.Weight + tail.Weight);
    }

    private static int EntryFrequency(ImePhoneticEntry entry) => entry.BaseFrequency + entry.UserFrequency;
    private static string Join(string first, string second) => first.Length == 0 ? second : first + " " + second;
}
