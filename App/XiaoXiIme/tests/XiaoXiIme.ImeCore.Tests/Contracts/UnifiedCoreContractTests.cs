using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class UnifiedCoreContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EqualWeightSentenceUsesOrdinalTieBreakRegardlessOfLoadOrder(bool reverse)
    {
        ImePhoneticEntry[] entries = [new("乙", "ni", 100), new("甲", "ni", 100), new("好", "hao", 100)];
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    reverse ? entries.Reverse().ToArray() : entries, [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );

        Assert.Equal
        (
            "乙好", context.QueryInterpretations("nihao").Candidates
                .Single(c => c.Interpretations.Any(p => p.Segments.Count > 1)).Candidate.Text
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteWholeWordRetainsEveryRegisteredFirstEdge(bool reverse)
    {
        ImePhoneticEntry[] entries =
        [
            new("你", "ni", 1), new("你好", "ni hao", 1),
            new("你好我", "ni hao wo", 1), new("好", "hao", 1), new("我", "wo", 1)
        ];
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    reverse ? entries.Reverse().ToArray() : entries, [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );

        Assert.Equal
        (
            new[] { 2, 5, 7 }, context.QueryInterpretations("nihaowo").Candidates
                .SelectMany(c => c.Interpretations).Where(p => p.Segments.Count == 1)
                .Select(p => p.CoverageEnd).Distinct().Order().ToArray()
        );
    }

    [Fact]
    public void OrdinaryEngineExplicitlyRejectsLookupOnlyDictionary()
    {
        var dictionary = new InMemoryImeDictionary([new ImeCandidate("你好", "nihao", 100)]);

        Assert.Throws<NotSupportedException>(() => new ImeContext(dictionary));
    }

    [Fact]
    public void LookupOnlyDictionaryDoesNotClaimCanonicalData()
    {
        var dictionary = new InMemoryImeDictionary([new ImeCandidate("你好", "nihao", 100)]);

        Assert.Throws<NotSupportedException>(() => dictionary.GetLexicon());
    }

    [Fact]
    public void DefaultAndExplicitPoliciesShareDecodeAndLearning()
    {
        var dictionary = InMemoryImeDictionary.CreateMinimalFallback();
        var ordinaryLearning = new List<ImeDictionaryLearning>();
        var explicitLearning = new List<ImeDictionaryLearning>();
        var ordinary = new ImeContext(dictionary, ordinaryLearning.Add);
        var configured = new ImeContext
        (
            dictionary, new ImeInputOptions
            {
                CasePolicy = ImeShapeCasePolicy.UppercaseOnly,
                Placement = ImeShapePlacement.Both,
                LengthPolicy = ImeShapeLengthPolicy.OptionalOneOrTwo,
                EnableStandaloneShape = true
            }, explicitLearning.Add
        );
        ordinary.SetComposition("nihao");
        configured.SetComposition("nihao");
        ordinary.ProcessKey(ImeKey.SelectCandidate(0));
        configured.ProcessKey(ImeKey.SelectCandidate(0));

        Assert.Equal
        (
            ordinaryLearning.Select(l => (l.Candidate, l.OriginalInput, string.Join('|', l.Segments))),
            explicitLearning.Select(l => (l.Candidate, l.OriginalInput, string.Join('|', l.Segments)))
        );
    }
}