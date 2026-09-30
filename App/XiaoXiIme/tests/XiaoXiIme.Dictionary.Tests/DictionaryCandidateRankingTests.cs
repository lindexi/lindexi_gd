using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class DictionaryCandidateRankingTests
{
    [Fact]
    public void Rank_WhenLayersMixThenOrdersUserExactThenSystemExactThenPrefixThenFallback()
    {
        var ranked = DictionaryCandidateRanking.Rank(
            [
                new DictionaryCandidate("号", "hao", 60, DictionaryCandidateSourceKind.Fallback, DictionaryCandidateMatchKind.Exact),
                new DictionaryCandidate("你好", "ni hao", 200, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Prefix),
                new DictionaryCandidate("你", "ni", 100, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Exact),
                new DictionaryCandidate("呢", "ni", 0, DictionaryCandidateSourceKind.User, DictionaryCandidateMatchKind.Exact, UserFrequency: 3),
            ],
            maxCount: 9);

        Assert.Collection(
            ranked,
            candidate => Assert.Equal("呢", candidate.Text),
            candidate => Assert.Equal("你", candidate.Text),
            candidate => Assert.Equal("你好", candidate.Text),
            candidate => Assert.Equal("号", candidate.Text));
    }

    [Fact]
    public void Rank_WhenSameTextAppearsInUserAndSystemThenKeepsUserCandidate()
    {
        var ranked = DictionaryCandidateRanking.Rank(
            [
                new DictionaryCandidate("你", "ni", 100, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Exact),
                new DictionaryCandidate("你", "ni", 0, DictionaryCandidateSourceKind.User, DictionaryCandidateMatchKind.Exact, UserFrequency: 2),
            ],
            maxCount: 9);

        Assert.Equal(new ImeCandidate("你", "ni", 2), Assert.Single(ranked));
    }

    [Fact]
    public void Rank_WhenSameLayerThenUsesFrequencyThenShorterEncodingThenShorterTextThenOrdinalText()
    {
        var ranked = DictionaryCandidateRanking.Rank(
            [
                new DictionaryCandidate("你们", "ni men", 80, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Prefix),
                new DictionaryCandidate("你好", "ni hao", 80, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Prefix),
                new DictionaryCandidate("泥", "ni", 80, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Prefix),
            ],
            maxCount: 9);

        Assert.Collection(
            ranked,
            candidate => Assert.Equal("泥", candidate.Text),
            candidate => Assert.Equal("你们", candidate.Text),
            candidate => Assert.Equal("你好", candidate.Text));
    }

    [Fact]
    public void Rank_WhenEncodingLengthTiesThenPrefersShorterTextBeforeOrdinal()
    {
        var ranked = DictionaryCandidateRanking.Rank(
            [
                new DictionaryCandidate("你好吗", "nihao", 80, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Prefix),
                new DictionaryCandidate("你们", "nihao", 80, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Prefix),
            ],
            maxCount: 9);

        Assert.Collection(
            ranked,
            candidate => Assert.Equal("你们", candidate.Text),
            candidate => Assert.Equal("你好吗", candidate.Text));
    }

    [Fact]
    public void Rank_WhenMaxCountIsNonPositiveThenReturnsEmpty()
    {
        Assert.Empty(DictionaryCandidateRanking.Rank(
            [new DictionaryCandidate("你", "ni", 100, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Exact)],
            maxCount: 0));
    }

    [Fact]
    public void RankReadings_WhenSameTextHasAbbreviationThenPrefersLongerCanonicalReading()
    {
        var ranked = DictionaryCandidateRanking.RankReadings(
            [
                new DictionaryCandidate("小希", "xx", 100, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Exact),
                new DictionaryCandidate("小希", "xiao xi", 100, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Exact),
            ],
            maxCount: 9);

        Assert.Collection(
            ranked,
            candidate =>
            {
                Assert.Equal("小希", candidate.Text);
                Assert.Equal("xiao xi", candidate.Reading);
            },
            candidate =>
            {
                Assert.Equal("小希", candidate.Text);
                Assert.Equal("xx", candidate.Reading);
            });
    }

    [Fact]
    public void RankReadings_WhenMaxCountIsNonPositiveThenReturnsEmpty()
    {
        Assert.Empty(DictionaryCandidateRanking.RankReadings(
            [new DictionaryCandidate("你", "ni", 100, DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Exact)],
            maxCount: 0));
    }
}
