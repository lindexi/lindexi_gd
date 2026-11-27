using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore.Tests;

public class ScreenshotFuzzyRuleTests
{
    public static IEnumerable<object[]> ComponentCases()
    {
        string[][] pairs =
        [
            ["ChC", "can", "chan", "cj", "ij"],
            ["ShS", "san", "shan", "sj", "uj"],
            ["ZhZ", "zan", "zhan", "zj", "vj"],
            ["AngAn", "ban", "bang", "bj", "bh"],
            ["EngEn", "ben", "beng", "bf", "bg"],
            ["InIng", "bin", "bing", "bb", "bk"],
            ["IanIang", "xian", "xiang", "xm", "xl"],
            ["UanUang", "zhuan", "zhuang", "vr", "vl"],
            ["AiAn", "cai", "can", "cd", "cj"],
            ["UnOng", "chun", "chong", "iy", "is"],
        ];
        foreach (var pair in pairs)
        foreach (var doublePinyin in new[] { false, true })
        {
            yield return [pair[0], doublePinyin, pair[1], doublePinyin ? pair[4] : pair[2]];
            yield return [pair[0], doublePinyin, pair[2], doublePinyin ? pair[3] : pair[1]];
        }
    }

    [Theory]
    [MemberData(nameof(ComponentCases))]
    public void UpdatingEachScreenshotRuleImmediatelyEnablesAndDisablesComponentAlias
    (
        string ruleName, bool doublePinyin, string reading, string alias
    )
    {
        var rule = Enum.Parse<ImeFuzzyRule>(ruleName);
        var context = Create(doublePinyin, reading);
        SoundShapeFuzzyTests.Type(context, alias);
        Assert.False(HasFuzzy());

        context.UpdateFuzzyRules([rule]);
        Assert.True(HasFuzzy());

        context.UpdateFuzzyRules([]);
        Assert.False(HasFuzzy());

        bool HasFuzzy() => context.QueryInterpretations(alias).Candidates.Any
            (c => c.Interpretations.Any(p => p.Evidence.Any(e => e.Kind == ImeEvidenceKind.Fuzzy)));

        Assert.Equal(alias, context.Snapshot.Composition.Reading);
    }

    [Theory]
    [InlineData(false, "bai", "AngAn", "AiAn")]
    [InlineData(false, "bai", "AiAn", "AngAn")]
    [InlineData(true, "bd", "AngAn", "AiAn")]
    [InlineData(true, "bd", "AiAn", "AngAn")]
    public void OverlappingFinalRulesReachOrderIndependentTransitiveClosure
    (
        bool doublePinyin, string input, string first, string second
    )
    {
        var context = Create(doublePinyin, "bang");
        context.UpdateFuzzyRules([Enum.Parse<ImeFuzzyRule>(first), Enum.Parse<ImeFuzzyRule>(second)]);

        Assert.True(HasComplete(context, input));
    }

    [Theory]
    [InlineData(false, "xiang", "InIng")]
    [InlineData(false, "xiang", "AngAn")]
    [InlineData(true, "xl", "InIng")]
    [InlineData(true, "xl", "AngAn")]
    public void FinalRulesDoNotReplaceSubstringsOfOtherComponents(bool doublePinyin, string input, string ruleName)
    {
        var context = Create(doublePinyin, "xian");
        context.UpdateFuzzyRules([Enum.Parse<ImeFuzzyRule>(ruleName)]);

        Assert.False(HasComplete(context, input));
    }

    private static ImeContext Create(bool doublePinyin, string reading)
    {
        var lexicon = new ImeLexicon
        (
            [new("字", reading, 1)], [],
            doublePinyin ? ImeProjectionDefinition.XiaoheDoublePinyin : ImeProjectionDefinition.FullPinyin, 1
        );
        return new
        (
            new InMemoryImeDictionary(lexicon), new ImeInputOptions
            {
                CasePolicy = ImeShapeCasePolicy.UppercaseOnly,
                Placement = ImeShapePlacement.PerCharacter,
                LengthPolicy = ImeShapeLengthPolicy.OptionalOneOrTwo,
            }, null
        );
    }

    private static bool HasComplete(ImeContext context, string input) => context.QueryInterpretations(input).Candidates
        .Any(c => c.Candidate.Text == "字" && c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Complete));
}