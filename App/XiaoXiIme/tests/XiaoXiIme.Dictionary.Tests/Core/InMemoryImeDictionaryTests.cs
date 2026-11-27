using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class InMemoryImeDictionaryTests
{
    [Fact]
    public void CreateMinimalFallback_WhenQueriedByCanonicalReadingThenReturnsProjectTerm()
    {
        var dictionary = InMemoryImeDictionary.CreateMinimalFallback();

        Assert.Equal("小希", Assert.Single(dictionary.Query("xiaoxi")).Text);
    }

    [Fact]
    public void CreateMinimalFallback_WhenLexiconIsReadThenHasNoLookupAliases()
    {
        var dictionary = InMemoryImeDictionary.CreateMinimalFallback();

        Assert.Empty(dictionary.GetLexicon().LookupAliases);
    }

    [Fact]
    public void Query_ReturnsCandidatesByScore()
    {
        var dictionary = new InMemoryImeDictionary
        (
            [
                new ImeCandidate("呢", "ni", 20),
                new ImeCandidate("你", "ni", 100),
            ]
        );

        var candidates = dictionary.Query("ni");

        Assert.Collection
        (
            candidates,
            candidate => Assert.Equal("你", candidate.Text),
            candidate => Assert.Equal("呢", candidate.Text)
        );
    }

    [Fact]
    public void Query_UsesCaseInsensitiveReading()
    {
        var dictionary = new InMemoryImeDictionary([new ImeCandidate("你", "ni", 100)]);

        var candidates = dictionary.Query("NI");

        Assert.Single(candidates);
        Assert.Equal("你", candidates[0].Text);
    }

    [Fact]
    public void Query_WhenPhraseReadingContainsSyllableSpacesThenContinuousInputMatches()
    {
        var dictionary = new InMemoryImeDictionary([new ImeCandidate("你好", "ni hao", 100)]);

        var candidates = dictionary.Query("nihao");

        Assert.Single(candidates);
        Assert.Equal("你好", candidates[0].Text);
    }

    [Fact]
    public void Query_RespectsMaxCount()
    {
        var dictionary = new InMemoryImeDictionary
        (
            [
                new ImeCandidate("你", "ni", 100),
                new ImeCandidate("呢", "ni", 90),
            ]
        );

        var candidates = dictionary.Query("ni", maxCount: 1);

        Assert.Single(candidates);
        Assert.Equal("你", candidates[0].Text);
    }

    [Fact]
    public void Query_ReturnsEmptyForBlankUnknownOrNonPositiveMaxCount()
    {
        var dictionary = new InMemoryImeDictionary([new ImeCandidate("你", "ni", 100)]);

        Assert.Empty(dictionary.Query(""));
        Assert.Empty(dictionary.Query("   "));
        Assert.Empty(dictionary.Query("missing"));
        Assert.Empty(dictionary.Query("ni", maxCount: 0));
    }

    [Fact]
    public void QueryByText_WhenExactTextThenReturnsCanonicalReadingBeforeAbbreviation()
    {
        var dictionary = new InMemoryImeDictionary
        (
            [
                new ImeCandidate("你好", "nh", 100),
                new ImeCandidate("你好", "ni hao", 100),
                new ImeCandidate("你", "ni", 100),
            ]
        );

        var candidates = dictionary.QueryByText("你好");

        Assert.Collection
        (
            candidates,
            candidate =>
            {
                Assert.Equal("你好", candidate.Text);
                Assert.Equal("ni hao", candidate.Reading);
            },
            candidate =>
            {
                Assert.Equal("你好", candidate.Text);
                Assert.Equal("nh", candidate.Reading);
            }
        );
        Assert.Empty(dictionary.QueryByText(""));
        Assert.Empty(dictionary.QueryByText("missing"));
    }

    [Fact]
    public void Constructor_IgnoresEntriesWithoutTextOrReading()
    {
        var dictionary = new InMemoryImeDictionary
        (
            [
                new ImeCandidate("你", "ni", 100),
                new ImeCandidate("", "ni", 90),
                new ImeCandidate("呢", "", 80),
            ]
        );

        var candidates = dictionary.Query("ni");

        Assert.Single(candidates);
        Assert.Equal("你", candidates[0].Text);
    }
}