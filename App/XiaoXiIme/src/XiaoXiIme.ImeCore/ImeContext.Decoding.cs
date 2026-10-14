using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    private sealed record DecodedPath(string Text, string Reading, int Frequency, int Source, ImeInterpretation Path);

    private List<ImeConversionCandidate> Decode(string input)
    {
        if (input.Length == 0) return [];
        var options = _options ?? throw new InvalidOperationException();
        var lexicon = _dictionary.GetLexicon();
        if (_lexiconVersion is { } version && version != lexicon.Version) _revision++;
        _lexiconVersion = lexicon.Version;
        var paths = new List<DecodedPath>();
        VisitSegments(0, "", "", int.MaxValue, 0, [], []);
        var grouped = paths.GroupBy(p => (p.Text, p.Path.Kind, p.Path.CoverageEnd, p.Path.RemainingInput,
            PendingReading: p.Path.Kind is ImeInterpretationKind.Prefix or ImeInterpretationKind.IncompleteShape ? p.Reading : "",
            PendingState: p.Path.Kind is ImeInterpretationKind.Prefix or ImeInterpretationKind.IncompleteShape
                ? string.Join('|', p.Path.Segments) + ":" + string.Join('|', p.Path.Evidence.Select(e =>
                    (e.Kind, e.Range, e.SyllableIndex, e.Input, e.CanonicalReading))) : ""));
        var semantic = grouped.OrderBy(g => g.Key.Text, StringComparer.Ordinal).ThenBy(g => g.Key.Kind)
            .ThenBy(g => g.Key.CoverageEnd).ThenBy(g => g.Key.RemainingInput, StringComparer.Ordinal)
            .ThenBy(g => g.Key.PendingReading, StringComparer.Ordinal)
            .ThenBy(g => g.Key.PendingState, StringComparer.Ordinal).ToArray();
        var results = new List<ImeConversionCandidate>();
        for (var ordinal = 0; ordinal < semantic.Length; ordinal++)
        {
            var group = semantic[ordinal];
            var ordered = group.OrderBy(p => p.Source).ThenBy(p => Strategy(p.Path))
                .ThenByDescending(p => p.Frequency).ThenBy(p => p.Path.Segments.Count)
                .ThenBy(p => p.Reading.Replace(" ", "", StringComparison.Ordinal).Length)
                .ThenBy(p => p.Reading, StringComparer.Ordinal)
                .ThenBy(p => string.Join('|', p.Path.Segments), StringComparer.Ordinal)
                .ThenBy(p => string.Join('|', p.Path.Evidence), StringComparer.Ordinal).ToArray();
            var best = ordered[0];
            var interpretations = ordered.Select(p => p.Path)
                .DistinctBy(p => (string.Join('|', p.Segments), string.Join('|', p.Evidence))).ToArray();
            var selectable = best.Path.Kind is ImeInterpretationKind.Complete or ImeInterpretationKind.Partial;
            results.Add(new(new(best.Text, best.Reading, best.Frequency),
                selectable && input == _reading ? new(_contextId, _revision, ordinal) : null, interpretations,
                new((int)best.Path.Kind, best.Path.CoverageEnd, best.Source, Strategy(best.Path), best.Frequency)));
        }
        return results.OrderBy(c => c.Rank.CompletionPriority).ThenByDescending(c => c.Rank.CoverageEnd)
            .ThenBy(c => c.Rank.SourcePriority).ThenBy(c => c.Rank.StrategyPriority).ThenByDescending(c => c.Rank.Frequency)
            .ThenBy(c => c.Interpretations[0].Segments.Count).ThenBy(c => c.Candidate.Reading.Replace(" ", "", StringComparison.Ordinal).Length)
            .ThenBy(c => c.Candidate.Text.Length).ThenBy(c => c.Candidate.Text, StringComparer.Ordinal)
            .ThenBy(c => c.Candidate.Reading, StringComparer.Ordinal).ToList();

        int Strategy(ImeInterpretation path)
        {
            var shape = path.Evidence.Any(e => e.Kind is ImeEvidenceKind.PerCharacterShape or ImeEvidenceKind.AfterWordShape);
            return options.SortPolicy switch
            {
                ImeCandidateSortPolicy.PhoneticFirst => shape ? 1 : 0,
                ImeCandidateSortPolicy.ShapeFirst => shape ? 0 : 1,
                _ => 0,
            };
        }

        void VisitSegments(int start, string text, string reading, int frequency, int source,
            IReadOnlyList<ImeConfirmedSegment> segments, IReadOnlyList<ImePathEvidence> evidence)
        {
            foreach (var entry in lexicon.Entries)
            {
                var syllables = entry.CanonicalReading.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                MatchSyllable(0, start, evidence);
                foreach (var alias in lexicon.LookupAliases.Where(a => a.Text == entry.Text && a.CanonicalReading == entry.CanonicalReading))
                {
                    if (Matches(start, alias.Input, false)) Finish(start + alias.Input.Length, evidence);
                }

                void AddPending(ImeInterpretationKind kind, IReadOnlyList<ImePathEvidence> ev)
                {
                    paths.Add(new(text + entry.Text, Join(reading, entry.CanonicalReading), Math.Min(frequency, EntryFrequency(entry)),
                        Math.Max(source, (int)entry.SourceKind), new(kind, input.Length, null, "", segments, ev)));
                }
                void Finish(int end, IReadOnlyList<ImePathEvidence> ev)
                {
                    var confirmed = segments.Append(new ImeConfirmedSegment(entry.Text, entry.CanonicalReading, start, end - start, text.Length, entry.Text.Length)).ToArray();
                    var complete = end == input.Length;
                    paths.Add(new(text + entry.Text, Join(reading, entry.CanonicalReading), Math.Min(frequency, EntryFrequency(entry)),
                        Math.Max(source, (int)entry.SourceKind), new(complete ? ImeInterpretationKind.Complete : ImeInterpretationKind.Partial,
                            end, new(0, end), input[end..], confirmed, ev)));
                    if (!complete) VisitSegments(end, text + entry.Text, Join(reading, entry.CanonicalReading),
                        Math.Min(frequency, EntryFrequency(entry)), Math.Max(source, (int)entry.SourceKind), confirmed, ev);
                }
                void MatchSyllable(int index, int position, IReadOnlyList<ImePathEvidence> ev)
                {
                    if (index == syllables.Length)
                    {
                        Finish(position, ev);
                        if (options.Placement is ImeShapePlacement.AfterWord or ImeShapePlacement.Both)
                            foreach (var alias in WordShapes(entry.Text)) MatchShape(alias, position, index - 1, ImeEvidenceKind.AfterWordShape, ev, end => Finish(end.Position, end.Evidence));
                        return;
                    }
                    if (position == input.Length) { AddPending(ImeInterpretationKind.Prefix, ev); return; }
                    var syllable = syllables[index];
                    var aliases = ComponentAliases(syllable, options.FuzzyRules).ToHashSet(StringComparer.Ordinal);
                    foreach (var projection in lexicon.Projection.Entries.Where(p => aliases.Contains(p.Initial + p.Final)))
                    {
                        var available = input.Length - position;
                        if (available < projection.Input.Length && Matches(position, projection.Input[..available], false))
                        {
                            AddPending(ImeInterpretationKind.Prefix, ev.Append(new(ImeEvidenceKind.Phonetic, new(position, available), index, input[position..], syllable, null)).ToArray());
                            continue;
                        }
                        if (!Matches(position, projection.Input, false)) continue;
                        var next = position + projection.Input.Length;
                        var phonetic = ev.Append(new ImePathEvidence(ImeEvidenceKind.Phonetic, new(position, projection.Input.Length), index, input.Substring(position, projection.Input.Length), syllable, null)).ToArray();
                        if (projection.Initial + projection.Final != syllable)
                            phonetic = phonetic.Append(new ImePathEvidence(ImeEvidenceKind.Fuzzy, new(position, projection.Input.Length), index,
                                projection.Initial + projection.Final, syllable, null)).ToArray();
                        if (options.LengthPolicy != ImeShapeLengthPolicy.RequiredTwo) MatchSyllable(index + 1, next, phonetic);
                        if (options.Placement is ImeShapePlacement.PerCharacter or ImeShapePlacement.Both && index < entry.Text.Length)
                        {
                            foreach (var alias in lexicon.ShapeEntries.Where(s => s.Text == entry.Text[index].ToString()).Select(s => s.ShapeCode).Distinct())
                                MatchShape(alias, next, index, ImeEvidenceKind.PerCharacterShape, phonetic, end => MatchSyllable(index + 1, end.Position, end.Evidence));
                        }
                    }
                }
                void MatchShape(string alias, int position, int index, ImeEvidenceKind kind, IReadOnlyList<ImePathEvidence> ev,
                    Action<(int Position, IReadOnlyList<ImePathEvidence> Evidence)> next)
                {
                    int[] lengths = options.LengthPolicy switch
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
                        var shapeEvidence = available == 0 ? ev : ev.Append(new ImePathEvidence(kind, new(position, available), index,
                            input.Substring(position, available), entry.CanonicalReading, alias)).ToArray();
                        if (available < length)
                        {
                            if (available > 0 || options.LengthPolicy == ImeShapeLengthPolicy.RequiredTwo) AddPending(ImeInterpretationKind.IncompleteShape, shapeEvidence);
                        }
                        else next((position + length, shapeEvidence));
                    }
                }
            }
        }
        IEnumerable<string> WordShapes(string text)
        {
            if (text.Length == 1) return lexicon.ShapeEntries.Where(s => s.Text == text).Select(s => s.ShapeCode).Distinct();
            return lexicon.ShapeEntries.Where(s => s.Text == text[..1]).SelectMany(first =>
                lexicon.ShapeEntries.Where(s => s.Text == text[^1..]).Select(last => first.ShapeCode[..1] + last.ShapeCode[..1])).Distinct();
        }
        bool Matches(int position, string code, bool shape)
        {
            if (position + code.Length > input.Length) return false;
            for (var i = 0; i < code.Length; i++)
            {
                var c = input[position + i];
                var upper = char.IsUpper(c);
                if (shape ? options.CasePolicy == ImeShapeCasePolicy.UppercaseOnly && !upper || options.CasePolicy == ImeShapeCasePolicy.LowercaseOnly && upper
                    : options.CasePolicy != ImeShapeCasePolicy.LowercaseOnly && upper) return false;
                if (char.ToLowerInvariant(c) != code[i]) return false;
            }
            return true;
        }
    }
    private static int EntryFrequency(ImePhoneticEntry entry) => entry.SourceKind == DictionaryCandidateSourceKind.User ? entry.UserFrequency : entry.BaseFrequency;
    private static string Join(string first, string second) => first.Length == 0 ? second : first + " " + second;
}
