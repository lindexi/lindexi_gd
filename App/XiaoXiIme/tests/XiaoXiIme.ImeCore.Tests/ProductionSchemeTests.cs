using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class ProductionSchemeTests
{
    public static IEnumerable<object[]> RuleCases()
    {
        string[][] pairs =
        [
            ["ChC", "can", "chan"], ["ShS", "san", "shan"], ["ZhZ", "zan", "zhan"],
            ["AngAn", "ban", "bang"], ["EngEn", "ben", "beng"], ["InIng", "bin", "bing"],
            ["IanIang", "xian", "xiang"], ["UanUang", "zhuan", "zhuang"],
            ["AiAn", "cai", "can"], ["UnOng", "chun", "chong"]
        ];
        foreach (var scheme in new[] { "fullPinyin", "xiaoheDoublePinyin", "sogouDoublePinyin", "microsoftDoublePinyin", "abcDoublePinyin", "jiajiaDoublePinyin", "naturalCodeDoublePinyin" })
        foreach (var pair in pairs)
        {
            yield return [scheme, pair[0], pair[1], pair[2]];
            yield return [scheme, pair[0], pair[2], pair[1]];
        }
    }

    [Theory]
    [MemberData(nameof(RuleCases))]
    public void EachProductionSchemeSupportsRuntimeRuleInBothDirections(string scheme, string rule, string reading, string alternative)
    {
        var projection = ImeProjectionDefinition.ForScheme(scheme);
        var input = projection.Entries.First(entry => entry.Initial + entry.Final == alternative).Input;
        var context = new ImeContext(new InMemoryImeDictionary(new ImeLexicon([new("字", reading, 1)], [], projection, 1)), Options(), null);
        foreach (var character in input) Assert.True(context.ProcessKey(ImeKey.FromCharacter(character)).Handled);
        Assert.DoesNotContain(context.QueryInterpretations(input).Candidates, HasFuzzy);
        context.UpdateFuzzyRules([Enum.Parse<ImeFuzzyRule>(rule)]);
        Assert.Contains(context.QueryInterpretations(input).Candidates, HasFuzzy);
        context.UpdateFuzzyRules([]);
        Assert.DoesNotContain(context.QueryInterpretations(input).Candidates, HasFuzzy);
        static bool HasFuzzy(ImeConversionCandidate candidate) => candidate.Interpretations.Any(p => p.Evidence.Any(e => e.Kind == ImeEvidenceKind.Fuzzy));
        Assert.Equal(input, context.Snapshot.Composition.Reading);
    }

    [Theory]
    [InlineData("sogouDoublePinyin")]
    [InlineData("microsoftDoublePinyin")]
    public void SemicolonCanBeTypedAndReplacedInActiveScheme(string scheme)
    {
        var context = new ImeContext(new InMemoryImeDictionary(new ImeLexicon([new("冰", "bing", 1)], [], ImeProjectionDefinition.ForScheme(scheme), 1)), Options(), null);
        context.ProcessKey(ImeKey.FromCharacter('b'));
        Assert.True(context.ProcessKey(ImeKey.FromCharacter(';')).Handled);
        context.SetComposition("b;");
        Assert.Contains(context.QueryInterpretations("b;").Candidates, candidate => candidate.Candidate.Text == "冰");
    }

    [Theory]
    [InlineData("fullPinyin")]
    [InlineData("xiaoheDoublePinyin")]
    [InlineData("abcDoublePinyin")]
    [InlineData("jiajiaDoublePinyin")]
    [InlineData("naturalCodeDoublePinyin")]
    public void SemicolonRemainsUnhandledOutsideSemicolonSchemes(string scheme)
    {
        var context = new ImeContext(new InMemoryImeDictionary(new ImeLexicon([new("字", "zi", 1)], [], ImeProjectionDefinition.ForScheme(scheme), 1)));
        Assert.False(context.ProcessKey(ImeKey.FromCharacter(';')).Handled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ni")]
    public void FullPinyinSnapshotDoesNotClaimSemicolon(string reading)
    {
        var context = new ImeContext(CanonicalDictionaryFixture.Create([new ImeCandidate("你", "ni", 1)]));
        context.SetComposition(reading);

        Assert.False(context.Snapshot.AcceptsSemicolon);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ni")]
    public void FullPinyinLeavesSemicolonUnhandled(string reading)
    {
        var context = new ImeContext(CanonicalDictionaryFixture.Create([new ImeCandidate("你", "ni", 1)]));
        context.SetComposition(reading);

        Assert.False(context.ProcessKey(ImeKey.FromCharacter(';')).Handled);
    }

    private static ImeInputOptions Options() => new() { CasePolicy = ImeShapeCasePolicy.UppercaseOnly, Placement = ImeShapePlacement.PerCharacter, LengthPolicy = ImeShapeLengthPolicy.OptionalOneOrTwo };

    private static bool IsComplete(ImeConversionCandidate candidate) => candidate.Interpretations.Any(path => path.Kind == ImeInterpretationKind.Complete);
}
