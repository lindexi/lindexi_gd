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
        foreach (var scheme in new[]
                 {
                     "fullPinyin", "xiaoheDoublePinyin", "sogouDoublePinyin", "microsoftDoublePinyin",
                     "abcDoublePinyin", "jiajiaDoublePinyin", "naturalCodeDoublePinyin"
                 })
        foreach (var pair in pairs)
        {
            yield return [scheme, pair[0], pair[1], pair[2]];
            yield return [scheme, pair[0], pair[2], pair[1]];
        }
    }

    [Theory]
    [MemberData(nameof(RuleCases))]
    public void EachProductionSchemeSupportsRuntimeRuleInBothDirections
        (string scheme, string rule, string reading, string alternative)
    {
        var projection = ImeProjectionDefinition.ForScheme(scheme);
        var input = projection.Entries.First(entry => entry.Initial + entry.Final == alternative).Input;
        var context = new ImeContext
            (new InMemoryImeDictionary(new ImeLexicon([new("字", reading, 1)], [], projection, 1)), Options(), null);
        foreach (var character in input)
        {
            Assert.True(context.ProcessKey(ImeKey.FromCharacter(character)).Handled);
        }

        Assert.DoesNotContain(context.QueryInterpretations(input).Candidates, HasFuzzy);
        context.UpdateFuzzyRules([Enum.Parse<ImeFuzzyRule>(rule)]);
        Assert.Contains(context.QueryInterpretations(input).Candidates, HasFuzzy);
        context.UpdateFuzzyRules([]);
        Assert.DoesNotContain(context.QueryInterpretations(input).Candidates, HasFuzzy);

        static bool HasFuzzy(ImeConversionCandidate candidate) => candidate.Interpretations.Any
            (p => p.Evidence.Any(e => e.Kind == ImeEvidenceKind.Fuzzy));

        Assert.Equal(input, context.Snapshot.Composition.Reading);
    }

    [Theory]
    [InlineData("sogouDoublePinyin")]
    [InlineData("microsoftDoublePinyin")]
    public void SemicolonCanBeTypedAndReplacedInActiveScheme(string scheme)
    {
        var context = new ImeContext
        (
            new InMemoryImeDictionary
                (new ImeLexicon([new("冰", "bing", 1)], [], ImeProjectionDefinition.ForScheme(scheme), 1)), Options(),
            null
        );
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
        var context = new ImeContext
        (
            new InMemoryImeDictionary
                (new ImeLexicon([new("字", "zi", 1)], [], ImeProjectionDefinition.ForScheme(scheme), 1))
        );
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
    [InlineData("", false)]
    [InlineData("ni", true)]
    public void FullPinyinOwnsSemicolonOnlyDuringComposition(string reading, bool expected)
    {
        var context = new ImeContext(CanonicalDictionaryFixture.Create([new ImeCandidate("你", "ni", 1)]));
        context.SetComposition(reading);

        Assert.Equal(expected, context.ProcessKey(ImeKey.FromCharacter(';')).Handled);
    }

    [Theory]
    [InlineData("dd")]
    [InlineData("d'd")]
    public void NaturalCodeFindsPhraseByTwoInitials(string input)
    {
        var projection = ImeProjectionDefinition.ForScheme("naturalCodeDoublePinyin");
        var context = new ImeContext
            (new InMemoryImeDictionary(new ImeLexicon([new("\u5730\u70B9", "di dian", 1)], [], projection, 1)));

        Assert.Equal("\u5730\u70B9", Assert.Single(context.QueryConversionList(input)).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NaturalCodeDoesNotSplitLegalSyllableWhenLexiconChanges(bool includeSyllable)
    {
        ImePhoneticEntry[] entries = includeSyllable
            ? [new("\u5355", "dan", 1), new("\u5730\u57FA", "di ji", 10000)]
            : [new("\u5730\u57FA", "di ji", 10000)];
        var context = CreateSchemeContext("naturalCodeDoublePinyin", entries);

        Assert.Equal(includeSyllable ? new[] { "\u5355" } : [],
            context.QueryConversionList("dj").Select(candidate => candidate.Text).ToArray());
        Assert.Equal("\u5730\u57FA", Assert.Single(context.QueryInterpretations("d'j").Candidates, IsComplete).Candidate.Text);
    }

    [Theory]
    [InlineData("dd", "", 2)]
    [InlineData("d'd", "", 3)]
    [InlineData("ddni", "ni", 2)]
    [InlineData("d'd'ni", "ni", 4)]
    public void NaturalCodeSelectionConsumesOnlyOriginalInputRange(string input, string remaining, int consumed)
    {
        var context = CreateSchemeContext("naturalCodeDoublePinyin", new("\u5730\u70B9", "di dian", 1),
            new("\u4F60", "ni", 1));
        foreach (var character in input)
        {
            Assert.True(context.ProcessKey(ImeKey.FromCharacter(character)).Handled);
        }

        Assert.Equal(input, context.Snapshot.Composition.Reading);
        Assert.Contains(context.Snapshot.Candidates, candidate => candidate.Text == "\u5730\u70B9");
        var candidate = Assert.Single(context.DetailedSnapshot.Page.Candidates, c => c.Candidate.Text == "\u5730\u70B9");
        Assert.Equal(new ImeInputRange(0, consumed), candidate.Interpretations[0].ConsumedRange);
        Assert.Equal(new[] { new ImeInputRange(0, 1), new ImeInputRange(input[1] == '\'' ? 2 : 1, 1) },
            candidate.Interpretations[0].Evidence.Select(e => e.Range));

        var result = context.SelectCandidate(Assert.IsType<ImeSelectionId>(candidate.SelectionId));

        Assert.Equal((true, "\u5730\u70B9", remaining, remaining.Length),
            (result.Handled, result.CommitText, result.Snapshot.Composition.Reading, result.Snapshot.Composition.CaretIndex));
    }

    [Theory]
    [InlineData("naturalCodeDoublePinyin", "dd", "di dian")]
    [InlineData("xiaoheDoublePinyin", "bq", "bi qi")]
    [InlineData("sogouDoublePinyin", "dd", "di dian")]
    [InlineData("microsoftDoublePinyin", "dd", "di dian")]
    [InlineData("abcDoublePinyin", "br", "bi ri")]
    [InlineData("jiajiaDoublePinyin", "bn", "bi ni")]
    public void InvalidDoubleKeyFallsBackToTwoInitials(string scheme, string input, string reading)
    {
        var context = CreateSchemeContext(scheme, new ImePhoneticEntry("\u7532\u4E59", reading, 1));

        Assert.Equal("\u7532\u4E59", Assert.Single(context.QueryInterpretations(input).Candidates, IsComplete).Candidate.Text);
    }

    [Theory]
    [InlineData("naturalCodeDoublePinyin", "dan", "di ji")]
    [InlineData("xiaoheDoublePinyin", "dan", "di ji")]
    [InlineData("sogouDoublePinyin", "dan", "di ji")]
    [InlineData("microsoftDoublePinyin", "dan", "di ji")]
    [InlineData("abcDoublePinyin", "dan", "di ji")]
    [InlineData("jiajiaDoublePinyin", "dian", "di ji")]
    public void LegalDoubleKeyIsNotSplitEvenWithoutMatchingLexiconEntry(string scheme, string syllable, string reading)
    {
        var phrase = new ImePhoneticEntry("\u5730\u57FA", reading, 10000);
        var withoutSyllable = CreateSchemeContext(scheme, phrase);
        var withSyllable = CreateSchemeContext(scheme, phrase, new("\u7532", syllable, 1));

        Assert.Empty(withoutSyllable.QueryConversionList("dj"));
        Assert.Equal("\u7532", Assert.Single(withSyllable.QueryConversionList("dj")).Text);
        Assert.Equal(phrase.Text, Assert.Single(withSyllable.QueryInterpretations("d'j").Candidates, IsComplete).Candidate.Text);
    }

    [Theory]
    [InlineData("naturalCodeDoublePinyin", "di", "dm", "di dian")]
    [InlineData("xiaoheDoublePinyin", "bi", "qi", "bi qi")]
    [InlineData("sogouDoublePinyin", "di", "dm", "di dian")]
    [InlineData("microsoftDoublePinyin", "di", "dm", "di dian")]
    [InlineData("abcDoublePinyin", "bi", "ri", "bi ri")]
    [InlineData("jiajiaDoublePinyin", "bi", "ni", "bi ni")]
    public void DoublePinyinPreservesFullAndMixedInput(string scheme, string head, string tail, string reading)
    {
        var context = CreateSchemeContext(scheme, new ImePhoneticEntry("\u7532\u4E59", reading, 1));

        foreach (var input in new[] { head + tail, head + tail[..1], head[..1] + tail })
        {
            Assert.Equal("\u7532\u4E59", Assert.Single(context.QueryInterpretations(input).Candidates, IsComplete).Candidate.Text);
        }
    }

    [Theory]
    [InlineData("naturalCodeDoublePinyin", "d'd'ni", "di dian")]
    [InlineData("xiaoheDoublePinyin", "b'q'ni", "bi qi")]
    [InlineData("sogouDoublePinyin", "d'd'ni", "di dian")]
    [InlineData("microsoftDoublePinyin", "d'd'ni", "di dian")]
    [InlineData("abcDoublePinyin", "b'r'ni", "bi ri")]
    [InlineData("jiajiaDoublePinyin", "b'n'ni", "bi ni")]
    public void ExplicitInitialSelectionPreservesOriginalSpans(string scheme, string input, string reading)
    {
        var context = CreateSchemeContext(scheme, new("\u7532\u4E59", reading, 1), new("\u4F60", "ni", 1));
        foreach (var character in input)
        {
            Assert.True(context.ProcessKey(ImeKey.FromCharacter(character)).Handled);
        }

        Assert.Equal(input, context.Snapshot.Composition.Reading);
        var candidate = Assert.Single(context.DetailedSnapshot.Page.Candidates, c => c.Candidate.Text == "\u7532\u4E59");
        var path = candidate.Interpretations[0];
        Assert.Equal(new ImeInputRange(0, 4), path.ConsumedRange);
        Assert.Equal(new[] { new ImeInputRange(0, 1), new ImeInputRange(2, 1) }, path.Evidence.Select(e => e.Range));

        var result = context.SelectCandidate(Assert.IsType<ImeSelectionId>(candidate.SelectionId));

        Assert.Equal((true, "\u7532\u4E59", "ni"), (result.Handled, result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Theory]
    [InlineData("naturalCodeDoublePinyin", "dd", "di dian", "di")]
    [InlineData("xiaoheDoublePinyin", "bq", "bi qi", "di")]
    [InlineData("sogouDoublePinyin", "dd", "di dian", "di")]
    [InlineData("microsoftDoublePinyin", "dd", "di dian", "di")]
    [InlineData("abcDoublePinyin", "br", "bi ri", "ri")]
    [InlineData("jiajiaDoublePinyin", "bn", "bi ni", "bi")]
    public void ImplicitInitialSelectionPreservesOriginalSpans(string scheme, string initials, string reading, string tail)
    {
        var projection = ImeProjectionDefinition.ForScheme(scheme);
        Assert.All(initials, key => Assert.True(projection.IsInitialKey(key)));
        Assert.False(projection.IsCompleteSyllableCode(initials));
        Assert.False(projection.IsCompleteSyllableCode(initials[1..] + tail[..1]));
        var phrase = new ImePhoneticEntry("\u7532\u4E59", reading, 1);
        var context = CreateSchemeContext(scheme, phrase, new("\u4E19", tail, 1));
        var input = initials + tail;
        foreach (var character in input)
        {
            Assert.True(context.ProcessKey(ImeKey.FromCharacter(character)).Handled);
        }

        Assert.Equal(input, context.Snapshot.Composition.Reading);
        var candidate = Assert.Single(context.DetailedSnapshot.Page.Candidates, c => c.Candidate.Text == phrase.Text);
        var path = Assert.Single(candidate.Interpretations);
        Assert.Equal(ImeInterpretationKind.Partial, path.Kind);
        Assert.Equal(new ImeInputRange(0, 2), path.ConsumedRange);
        Assert.Equal(new[] { new ImeInputRange(0, 1), new ImeInputRange(1, 1) }, path.Evidence.Select(e => e.Range));

        var result = context.SelectCandidate(Assert.IsType<ImeSelectionId>(candidate.SelectionId));

        Assert.Equal((true, phrase.Text, tail, tail.Length),
            (result.Handled, result.CommitText, result.Snapshot.Composition.Reading, result.Snapshot.Composition.CaretIndex));
    }

    private static ImeContext CreateSchemeContext(string scheme, params ImePhoneticEntry[] entries) =>
        new(new InMemoryImeDictionary(new ImeLexicon(entries, [], ImeProjectionDefinition.ForScheme(scheme), 1)));

    private static ImeInputOptions Options() => new()
    {
        CasePolicy = ImeShapeCasePolicy.UppercaseOnly, Placement = ImeShapePlacement.PerCharacter,
        LengthPolicy = ImeShapeLengthPolicy.OptionalOneOrTwo
    };

    private static bool IsComplete(ImeConversionCandidate candidate) => candidate.Interpretations.Any
        (path => path.Kind == ImeInterpretationKind.Complete);
}