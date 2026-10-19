using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore.Tests;

public class PredictionExclusionTests
{
    [Theory]
    [InlineData("mz", "梅", "没有", false)]
    [InlineData("mz", "梅", "没有", true)]
    [InlineData("uohw", "说话", "说话了", false)]
    [InlineData("uohw", "说话", "说话了", true)]
    [InlineData("m", "梅", "没有", false)]
    [InlineData("mzyz", "梅", "没有", false)]
    [InlineData("mzyb", "没有", "没有了", false)]
    public void OnlyInputBackedSyllablesAreDisplayed(string input, string included, string excluded, bool fuzzy)
    {
        var dictionary = Create();
        var context = new ImeContext(dictionary);
        if (fuzzy) context = context.PrepareFuzzyRulesChange([ImeFuzzyRule.ZhZ]);
        var candidates = context.QueryConversionList(input);
        Assert.True(candidates.Any(c => c.Text == included) && candidates.All(c => c.Text != excluded),
            string.Join("|", candidates.Select(c => c.Text + ":" + c.Reading)));
    }

    [Theory]
    [InlineData("n", "你")]
    [InlineData("ni", "你")]
    [InlineData("nih", "你好")]
    [InlineData("niR", "你")]
    [InlineData("niRX", "你")]
    [InlineData("niRXh", "你好")]
    public void NewModeDisplaysOnlyWordsWithEvidenceForEverySyllable(string input, string expected)
    {
        var lexicon = new ImeLexicon(
            [new("你", "ni", 1), new("你好", "ni hao", 100), new("你好啊", "ni hao a", 1000)],
            [new("你", "rx", "fixture"), new("好", "nz", "fixture")],
            new("fixture", [new("n", "i", "ni"), new("h", "ao", "hc"), new("", "a", "aa")]), 1);
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), null);
        var candidates = context.QueryInterpretations(input).Candidates;
        Assert.Contains(candidates, c => c.Candidate.Text == expected);
        Assert.DoesNotContain(candidates, c => c.Candidate.Reading.Split(' ').Length > expected.Length);
    }

    [Fact]
    public void HistoricalConsumedCodeLearningCannotCreateAnAlias()
    {
        var dictionary = new UserDictionary(Create(), [new("没有", "mz", 1000000)]);
        var context = new ImeContext(dictionary);
        Assert.DoesNotContain(context.QueryConversionList("mz"), c => c.Text == "没有");
    }

    [Fact]
    public void ExplicitAliasRemainsSelectable()
    {
        var lexicon = Create().GetLexicon() with { LookupAliases = [new("没有", "mei you", "my")] };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon));
        Assert.Contains(context.QueryConversionList("my"), c => c.Text == "没有");
    }

    private static InMemoryImeDictionary Create() => new(new ImeLexicon(
        [new("梅", "mei", 1), new("没有", "mei you", 100000), new("美国", "mei guo", 90000),
         new("每天", "mei tian", 80000), new("没有了", "mei you le", 90000),
         new("说话", "shuo hua", 1), new("说话了", "shuo hua le", 100000),
         new("说话的人", "shuo hua de ren", 90000), new("方式", "fang shi", 100000)],
        [], ImeProjectionDefinition.ForScheme("naturalCodeDoublePinyin"), 1));
}
