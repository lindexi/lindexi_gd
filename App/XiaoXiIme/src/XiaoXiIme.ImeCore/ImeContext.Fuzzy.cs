using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    private static IEnumerable<string> ComponentAliases(string syllable, IReadOnlyList<ImeFuzzyRule> rules)
    {
        var initial = new[] { "zh", "ch", "sh" }.FirstOrDefault(syllable.StartsWith)
            ?? ("bpmfdtnlgkhjqxrzcsyw".Contains(syllable[0]) ? syllable[..1] : "");
        var final = syllable[initial.Length..];
        var initials = new HashSet<string>(StringComparer.Ordinal) { initial };
        var finals = new HashSet<string>(StringComparer.Ordinal) { final };
        bool changed;
        do
        {
            changed = false;
            foreach (var rule in rules)
            {
                var (left, right, isInitial) = rule switch
                {
                    ImeFuzzyRule.ZhZ => ("zh", "z", true),
                    ImeFuzzyRule.ShS => ("sh", "s", true),
                    ImeFuzzyRule.ChC => ("ch", "c", true),
                    ImeFuzzyRule.AngAn => ("ang", "an", false),
                    ImeFuzzyRule.EngEn => ("eng", "en", false),
                    ImeFuzzyRule.InIng => ("in", "ing", false),
                    ImeFuzzyRule.IanIang => ("ian", "iang", false),
                    ImeFuzzyRule.UanUang => ("uan", "uang", false),
                    ImeFuzzyRule.AiAn => ("ai", "an", false),
                    ImeFuzzyRule.UnOng => ("un", "ong", false),
                    _ => throw new ArgumentException(nameof(rules), nameof(rules)),
                };
                var components = isInitial ? initials : finals;
                if (components.Contains(left)) changed |= components.Add(right);
                if (components.Contains(right)) changed |= components.Add(left);
            }
        } while (changed);
        return initials.SelectMany(i => finals.Select(f => i + f)).Order(StringComparer.Ordinal);
    }

    private static void ValidateProjection(ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules)
    {
        var registered = lexicon.Projection.Entries.Select(p => p.Initial + p.Final).ToHashSet(StringComparer.Ordinal);
        foreach (var syllable in lexicon.Entries.SelectMany(e => e.CanonicalReading.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Distinct(StringComparer.Ordinal))
        foreach (var alias in ComponentAliases(syllable, rules))
            if (!registered.Contains(alias)) throw new NotSupportedException();
    }
}
