using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;

namespace XiaoXiIme.ImeCore.Tests;

public class LegacyFuzzyTransactionTests
{
    [Theory]
    [InlineData(ImeFuzzyRule.ChC, "chan", "can")]
    [InlineData(ImeFuzzyRule.ShS, "shan", "san")]
    [InlineData(ImeFuzzyRule.ZhZ, "zhan", "zan")]
    [InlineData(ImeFuzzyRule.AngAn, "bang", "ban")]
    [InlineData(ImeFuzzyRule.EngEn, "beng", "ben")]
    [InlineData(ImeFuzzyRule.InIng, "bin", "bing")]
    [InlineData(ImeFuzzyRule.IanIang, "xian", "xiang")]
    [InlineData(ImeFuzzyRule.UanUang, "guan", "guang")]
    [InlineData(ImeFuzzyRule.AiAn, "bai", "ban")]
    [InlineData(ImeFuzzyRule.UnOng, "chun", "chong")]
    public void WhenPreparingRuleThenCandidateChangesWithoutLearning(ImeFuzzyRule rule, string canonical, string alias)
    {
        var dictionary = new InMemoryImeDictionary(new ImeLexicon([new("词", canonical, 123)], [], ImeProjectionDefinition.FullPinyin, 0));
        var learned = new List<ImeDictionaryLearning>();
        var context = new ImeContext(dictionary, learned.Add);
        context.SetComposition(alias);
        var prepared = context.PrepareFuzzyRulesChange([rule]);
        Assert.Contains(prepared.Snapshot.Candidates, c => c.Text == "词" && c.Reading == canonical);
    }

    [Fact]
    public void WhenPreparingThenOriginalAndLearningRemainUnchanged()
    {
        var dictionary = new InMemoryImeDictionary(new ImeLexicon([new("张", "zhang", 123)], [], ImeProjectionDefinition.FullPinyin, 0));
        var learned = new List<ImeDictionaryLearning>();
        var context = new ImeContext(dictionary, learned.Add);
        context.SetComposition("zan");
        var original = context.Snapshot;
        context.PrepareFuzzyRulesChange([ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn]);
        Assert.Equivalent(new { Snapshot = original, Learned = 0 }, new { Snapshot = context.Snapshot, Learned = learned.Count });
    }
}
