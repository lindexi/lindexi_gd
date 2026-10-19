using System.Runtime.CompilerServices;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    private DecodingIndex? _decodingIndex;
    private string _lastDecodeMetrics = "";
    private static readonly ConditionalWeakTable<ImeLexicon, Dictionary<int, DecodingIndex>> SharedIndexes = new();

    private static DecodingIndex GetDecodingIndex(ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules)
    {
        var indexes = SharedIndexes.GetValue(lexicon, _ => []);
        var mask = rules.Aggregate(0, (value, rule) => value | 1 << (int)rule);
        lock (indexes)
        {
            if (!indexes.TryGetValue(mask, out var index))
                indexes.Add(mask, index = new(lexicon, Array.AsReadOnly(rules.Distinct().Order().ToArray())));
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
        internal readonly double LogFrequencyMass;

        internal DecodingIndex(ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules)
        {
            Lexicon = lexicon;
            Rules = rules;
            LogFrequencyMass = Math.Log(1d + lexicon.Entries.Sum(e => 1d + Math.Max(0, EntryFrequency(e))));
            Syllables = lexicon.Entries.ToDictionary(e => e,
                e => e.CanonicalReading.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var registered = lexicon.Projection.Entries.Select(p => p.Initial + p.Final).ToHashSet(StringComparer.Ordinal);
            foreach (var syllable in Syllables.Values.SelectMany(s => s).Distinct(StringComparer.Ordinal))
            foreach (var alias in ComponentAliases(syllable, rules))
                if (!registered.Contains(alias)) throw new NotSupportedException();
            var projectionOrder = lexicon.Projection.Entries.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);
            var byComponent = lexicon.Projection.Entries.ToLookup(p => p.Initial + p.Final, StringComparer.Ordinal);
            Projections = Syllables.Values.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToDictionary(s => s,
                s => ComponentAliases(s, rules).SelectMany(a => byComponent[a]).ToHashSet()
                    .OrderBy(p => projectionOrder[p]).ToArray(), StringComparer.Ordinal);
            Shapes = lexicon.ShapeEntries.GroupBy(s => s.Text, StringComparer.Ordinal).ToDictionary(g => g.Key,
                g => g.Select(s => s.ShapeCode).Distinct().ToArray(), StringComparer.Ordinal);
            var aliases = lexicon.LookupAliases.ToLookup(a => (a.Text, a.CanonicalReading));
            Aliases = lexicon.Entries.ToDictionary(e => e, e => aliases[(e.Text, e.CanonicalReading)].ToArray());
            var starts = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var i = 0; i < lexicon.Entries.Count; i++)
            {
                var entry = lexicon.Entries[i];
                foreach (var code in Projections[Syllables[entry][0]].Select(p => p.Input)
                    .Concat(Aliases[entry].Select(a => a.Input)).Distinct(StringComparer.Ordinal))
                {
                    if (!starts.TryGetValue(code, out var indices)) starts.Add(code, indices = []);
                    indices.Add(i);
                }
            }
            Starts = starts.ToDictionary(p => p.Key, p => p.Value.ToArray(), StringComparer.Ordinal);
        }

        internal string[] CharacterShapes(string text) => Shapes.GetValueOrDefault(text) ?? [];
    }
}
