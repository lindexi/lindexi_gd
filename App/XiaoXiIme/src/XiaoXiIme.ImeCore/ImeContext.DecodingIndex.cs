using System.Runtime.CompilerServices;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    private DecodingIndex? _decodingIndex;
    private string _lastDecodeMetrics = "";
    private static readonly ConditionalWeakTable<ImeLexicon, Dictionary<int, DecodingIndex>> SharedIndexes = new();

    private static readonly ConditionalWeakTable<IReadOnlyList<ImePhoneticEntry>, Dictionary<int, DecodingIndex>>
        Topologies = new();

    /// <summary>Prepares a published dictionary on the owner's background worker.</summary>
    public static void PrepareLexicon(ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules)
    {
        ArgumentNullException.ThrowIfNull(lexicon);
        ArgumentNullException.ThrowIfNull(rules);
        _ = GetDecodingIndex(lexicon, rules);
    }

    private static DecodingIndex GetDecodingIndex
        (ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules, DecodingIndex? previous = null)
    {
        var indexes = SharedIndexes.GetValue(lexicon, _ => []);
        var mask = rules.Aggregate(0, (value, rule) => value | 1 << (int)rule);
        lock (indexes)
        {
            if (!indexes.TryGetValue(mask, out var index))
            {
                var topologies = Topologies.GetValue(lexicon.Entries, _ => []);
                lock (topologies)
                {
                    topologies.TryGetValue(mask, out var topology);
                    indexes.Add
                    (
                        mask,
                        index = new(lexicon, Array.AsReadOnly(rules.Distinct().Order().ToArray()), topology ?? previous)
                    );
                    topologies[mask] = index;
                }
            }

            return index;
        }
    }

    private sealed class DecodingIndex
    {
        internal readonly ImeLexicon Lexicon;
        internal readonly IReadOnlyList<ImeFuzzyRule> Rules;
        internal readonly Dictionary<ImePhoneticEntry, string[]> Syllables;
        internal readonly Dictionary<string, ImeProjectionEntry[]> Projections;
        internal readonly Dictionary<string, string[]> Shapes;
        internal readonly Dictionary<ImePhoneticEntry, ImeLookupAlias[]> Aliases;
        internal readonly Dictionary<string, int[]> Starts;
        internal readonly Dictionary<string, List<int>>? NaturalCodeInitialStarts;
        internal readonly double LogFrequencyMass;

        internal DecodingIndex(ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules, DecodingIndex? previous)
        {
            Lexicon = lexicon;
            Rules = rules;
            LogFrequencyMass = lexicon.RankingLogFrequencyMass ?? Math.Log
                (1d + lexicon.Entries.Sum(e => 1d + Math.Max(0, EntryFrequency(e))));
            if (previous is not null && ReferenceEquals(lexicon.Entries, previous.Lexicon.Entries) &&
                ReferenceEquals(lexicon.Projection, previous.Lexicon.Projection) &&
                ReferenceEquals(lexicon.ShapeEntries, previous.Lexicon.ShapeEntries) &&
                ReferenceEquals(lexicon.LookupAliases, previous.Lexicon.LookupAliases) && rules.SequenceEqual
                    (previous.Rules))
            {
                Projections = previous.Projections;
                Shapes = previous.Shapes;
                Starts = previous.Starts;
                NaturalCodeInitialStarts = previous.NaturalCodeInitialStarts;
                Syllables = previous.Syllables;
                Aliases = previous.Aliases;
                return;
            }

            // Legacy dictionary implementations may still replace entries when only ranking changes.
            if (previous is not null && ReferenceEquals(lexicon.Projection, previous.Lexicon.Projection) &&
                ReferenceEquals(lexicon.ShapeEntries, previous.Lexicon.ShapeEntries) &&
                ReferenceEquals(lexicon.LookupAliases, previous.Lexicon.LookupAliases) &&
                rules.SequenceEqual(previous.Rules) && lexicon.Entries.Count == previous.Lexicon.Entries.Count &&
                lexicon.Entries.Select
                ((entry, i) => entry.Text == previous.Lexicon.Entries[i].Text &&
                               entry.CanonicalReading == previous.Lexicon.Entries[i].CanonicalReading
                ).All(matches => matches))
            {
                Projections = previous.Projections;
                Shapes = previous.Shapes;
                Starts = previous.Starts;
                NaturalCodeInitialStarts = previous.NaturalCodeInitialStarts;
                Syllables = new(lexicon.Entries.Count);
                Aliases = new(lexicon.Entries.Count);
                for (var i = 0; i < lexicon.Entries.Count; i++)
                {
                    Syllables.Add(lexicon.Entries[i], previous.Syllables[previous.Lexicon.Entries[i]]);
                    Aliases.Add(lexicon.Entries[i], previous.Aliases[previous.Lexicon.Entries[i]]);
                }

                return;
            }

            Syllables = lexicon.Entries.ToDictionary
            (
                e => e,
                e => e.CanonicalReading.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            );
            var registered = lexicon.Projection.Entries.Select(p => p.Initial + p.Final).ToHashSet
                (StringComparer.Ordinal);
            foreach (var syllable in Syllables.Values.SelectMany(s => s).Distinct(StringComparer.Ordinal))
            foreach (var alias in ComponentAliases(syllable, rules))
            {
                if (!registered.Contains(alias))
                {
                    throw new NotSupportedException();
                }
            }

            var projectionOrder = lexicon.Projection.Entries.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);
            var byComponent = lexicon.Projection.Entries.ToLookup(p => p.Initial + p.Final, StringComparer.Ordinal);
            Projections = Syllables.Values.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToDictionary
            (
                s => s,
                s => ComponentAliases(s, rules).SelectMany(a => byComponent[a]).ToHashSet()
                    .OrderBy(p => projectionOrder[p]).ToArray(), StringComparer.Ordinal
            );
            if (lexicon.Projection.Name == NaturalCodeScheme)
            {
                NaturalCodeInitialStarts = new(StringComparer.Ordinal);
            }

            Shapes = lexicon.ShapeEntries.GroupBy(s => s.Text, StringComparer.Ordinal).ToDictionary
            (
                g => g.Key,
                g => g.Select(s => s.ShapeCode).Distinct().ToArray(), StringComparer.Ordinal
            );
            var aliases = lexicon.LookupAliases.ToLookup(a => (a.Text, a.CanonicalReading));
            Aliases = lexicon.Entries.ToDictionary(e => e, e => aliases[(e.Text, e.CanonicalReading)].ToArray());
            var starts = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var i = 0; i < lexicon.Entries.Count; i++)
            {
                var entry = lexicon.Entries[i];
                var projections = Projections[Syllables[entry][0]];
                AddStarts(starts, projections.Select(p => p.Input).Concat(Aliases[entry].Select(a => a.Input)), i);
                if (NaturalCodeInitialStarts is not null)
                {
                    AddStarts(NaturalCodeInitialStarts,
                        projections.Where(p => p.Initial.Length > 0).Select(p => p.Input[..1]), i);
                }
            }

            Starts = starts.ToDictionary(p => p.Key, p => p.Value.ToArray(), StringComparer.Ordinal);

            static void AddStarts(Dictionary<string, List<int>> index, IEnumerable<string> codes, int entryIndex)
            {
                foreach (var code in codes.Distinct(StringComparer.Ordinal))
                {
                    if (!index.TryGetValue(code, out var indices))
                    {
                        index.Add(code, indices = []);
                    }

                    indices.Add(entryIndex);
                }
            }
        }

        internal string[] CharacterShapes(string text) => Shapes.GetValueOrDefault(text) ?? [];
    }
}