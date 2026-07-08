using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class InMemoryImeDictionaryTests
{
    [Fact]
    public void Query_ReturnsCandidatesByScore()
    {
        var dictionary = new InMemoryImeDictionary(
        [
            new ImeCandidate("呢", "ni", 20),
            new ImeCandidate("你", "ni", 100),
        ]);

        var candidates = dictionary.Query("ni");

        Assert.Collection(
            candidates,
            candidate => Assert.Equal("你", candidate.Text),
            candidate => Assert.Equal("呢", candidate.Text));
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
    public void Query_RespectsMaxCount()
    {
        var dictionary = new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("呢", "ni", 90),
        ]);

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
    public void Constructor_IgnoresEntriesWithoutTextOrReading()
    {
        var dictionary = new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("", "ni", 90),
            new ImeCandidate("呢", "", 80),
        ]);

        var candidates = dictionary.Query("ni");

        Assert.Single(candidates);
        Assert.Equal("你", candidates[0].Text);
    }
}

