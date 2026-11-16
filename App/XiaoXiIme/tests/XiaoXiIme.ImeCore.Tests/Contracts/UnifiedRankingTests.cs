using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class UnifiedRankingTests
{
    [Theory]
    [InlineData("fullPinyin", "shijie", false)]
    [InlineData("fullPinyin", "shijie", true)]
    [InlineData("xiaoheDoublePinyin", "uijp", false)]
    [InlineData("xiaoheDoublePinyin", "uijp", true)]
    public void RegisteredCompletePhraseOutranksFrequentFragmentAssembly(string scheme, string input, bool reverse)
    {
        var context = Create(scheme, 68347, reverse);

        Assert.Equal("世界", context.QueryInterpretations(input).Candidates[0].Candidate.Text);
    }

    [Fact]
    public void CompleteSyllablesOutrankHigherFrequencyCompletion()
    {
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [new("世界", "shi jie", 1), new("是金额", "shi jin e", 2000000)], [],
                    ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );

        Assert.Equal("世界", context.QueryInterpretations("shijie").Candidates[0].Candidate.Text);
    }

    [Fact]
    public void RareWholeEntryDoesNotHaveUnconditionalLengthPriority()
    {
        var context = Create("fullPinyin", 1, false);

        Assert.NotEqual("世界", context.QueryInterpretations("shijie").Candidates[0].Candidate.Text);
    }

    [Fact]
    public void SentenceRankingUsesAllSegmentsRatherThanMinimumFrequency()
    {
        // Both paths have minimum 10. Raw products also favor extra high-frequency tokens;
        // a normalized lexical model must charge each independent word decision.
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [
                        new("甲乙", "ni hao", 100), new("甲", "ni", 1000),
                        new("丙", "hao", 10), new("丁", "ma", 10)
                    ], [],
                    ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );

        Assert.Equal("甲乙丁", context.QueryInterpretations("nihaoma").Candidates[0].Candidate.Text);
    }

    [Fact]
    public void EqualMinimumFrequencyPathsAreDistinguishedByOtherSegments()
    {
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [
                        new("甲乙", "ni hao", 100), new("丙", "ma", 10),
                        new("丁", "ni", 10), new("戊己", "hao ma", 1000)
                    ], [],
                    ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );

        Assert.Equal("丁戊己", context.QueryInterpretations("nihaoma").Candidates[0].Candidate.Text);
    }

    private static ImeContext Create(string scheme, int wholeFrequency, bool reverse)
    {
        ImePhoneticEntry[] entries =
        [
            new("世界", "shi jie", wholeFrequency), new("是", "shi", 1799848),
            new("及", "ji", 92194), new("额", "e", 4967), new("届", "jie", 8712)
        ];
        return new
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    reverse ? entries.Reverse().ToArray() : entries, [], ImeProjectionDefinition.ForScheme(scheme), 1
                )
            )
        );
    }
}