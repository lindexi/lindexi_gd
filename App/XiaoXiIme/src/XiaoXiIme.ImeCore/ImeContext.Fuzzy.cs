using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    /// <summary>Prepares a rule change without mutating this context or learning.</summary>
    public ImeContext PrepareFuzzyRulesChange(IReadOnlyList<ImeFuzzyRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Any(r => !Enum.IsDefined(r)))
        {
            throw new ArgumentException(nameof(rules));
        }

        var prepared = new ImeContext
        (
            _dictionary,
            _options with { FuzzyRules = Array.AsReadOnly(rules.Distinct().Order().ToArray()) }, _learn
        )
        {
            _reading = _reading,
            _caretIndex = _caretIndex,
            _selection = _selection,
            _eatConfirmationSpace = _eatConfirmationSpace,
            _revision = _revision + 1
        };
        prepared.RefreshCandidates();
        if (_candidates.Count > _selection)
        {
            var selected = _candidates[_selection];
            var index = prepared._candidates.FindIndex(c => c.Text == selected.Text && c.Reading == selected.Reading);
            prepared._selection = index >= 0 ? index : 0;
            prepared.NormalizeCandidateWindow();
        }

        return prepared;
    }

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
                if (components.Contains(left))
                {
                    changed |= components.Add(right);
                }

                if (components.Contains(right))
                {
                    changed |= components.Add(left);
                }
            }
        } while (changed);

        return initials.SelectMany(i => finals.Select(f => i + f)).Order(StringComparer.Ordinal);
    }

    private static void ValidateProjection(ImeLexicon lexicon, IReadOnlyList<ImeFuzzyRule> rules)
    {
        _ = GetDecodingIndex(lexicon, rules);
    }
}