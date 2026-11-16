using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class UnifiedPrefixTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryRegisteredFirstSegmentSurvivesWholeWord(bool whole)
    {
        var context = Create(whole);
        context.SetComposition("uohwrfuo");
        Assert.Contains(context.Snapshot.Candidates, c => c.Text == "说话");
    }

    [Theory]
    [InlineData("说话人", "uo")]
    [InlineData("说话", "rfuo")]
    [InlineData("说", "hwrfuo")]
    public void LocalSelectionConsumesOnlyItsBoundary(string text, string remaining)
    {
        var context = Create();
        context.SetComposition("uohwrfuo");
        var index = context.Snapshot.Candidates.ToList().FindIndex(c => c.Text == text);
        while (context.Snapshot.CandidateWindow.PageStart + context.Snapshot.CandidateWindow.PageSize <= index)
        {
            context.ProcessKey(new(ImeKeyKind.NextCandidatePage));
        }

        var result = context.ProcessKey(ImeKey.SelectCandidate(index - context.Snapshot.CandidateWindow.PageStart));
        Assert.Equal((text, remaining), (result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShSControlsCanonicalSuoCandidates(bool enabled)
    {
        var context = Create();
        if (enabled)
        {
            context = context.PrepareFuzzyRulesChange([ImeFuzzyRule.ShS]);
        }

        context.SetComposition("uohwrfuo");
        Assert.Equal
        (
            enabled ? new[] { "所", "索", "梭" } : [],
            context.Snapshot.Candidates.Where(c => c.Reading == "suo").Select(c => c.Text)
        );
    }

    [Fact]
    public void WholeWordSelectionConsumesEightKeys()
    {
        var context = Create(true);
        context.SetComposition("uohwrfuo");
        var candidate = context.QueryInterpretations("uohwrfuo").Candidates
            .Single
            (c => c.Candidate.Text == "说话人说" &&
                  c.Interpretations.Any(p => p.Segments.Count == 1)
            );

        var result = context.SelectCandidate(candidate.SelectionId.GetValueOrDefault());

        Assert.Equal(("说话人说", ""), (result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void QueryingAnotherInputDoesNotReplaceNumericSelection()
    {
        var context = Create();
        context.SetComposition("uohwrfuo");
        var expected = context.Snapshot.Candidates[0].Text;
        context.QueryInterpretations("rf");

        var result = context.ProcessKey(ImeKey.SelectCandidate(0));

        Assert.Equal(expected, result.CommitText);
    }

    [Fact]
    public void QueryingAnotherInputDoesNotInvalidateDisplayedIdentity()
    {
        var context = Create();
        context.SetComposition("uohwrfuo");
        var candidate = context.QueryInterpretations("uohwrfuo").Candidates
            .First(c => c.SelectionId.HasValue);
        context.QueryInterpretations("rf");

        var result = context.SelectCandidate(candidate.SelectionId.GetValueOrDefault());

        Assert.Equal(candidate.Candidate.Text, result.CommitText);
    }

    [Fact]
    public void VersionChangeRejectsSelectionWithoutCompositionOrLearningMutation()
    {
        var dictionary = new UserDictionary
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [new("你", "ni", 1)], [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );
        var learning = new List<ImeDictionaryLearning>();
        var context = new ImeContext(dictionary, learning.Add);
        context.SetComposition("ni");
        var id = context.QueryInterpretations("ni").Candidates.First
            (c => c.SelectionId.HasValue).SelectionId.GetValueOrDefault();
        dictionary.Learn
        (
            new(new ImeCandidate("你", "ni", 1), "ni")
                { Segments = [new("你", "ni", 0, 2, 0, 1)] }
        );
        context.QueryInterpretations("ni");

        var result = context.SelectCandidate(id);

        Assert.Equal
        (
            (false, (string?)null, "ni", 0),
            (result.Handled, result.CommitText, result.Snapshot.Composition.Reading, learning.Count)
        );
    }

    [Fact]
    public void PagesBeyondOneHundredRetainEveryWordEdge()
    {
        var entries = Enumerable.Range(0, 150).Select(i => new ImePhoneticEntry($"词{i:D3}", "ni", 150 - i)).ToArray();
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    entries, [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );
        context.SetComposition("ni");
        var first = context.QueryInterpretations("ni");
        var last = context.QueryInterpretations("ni", 100);

        Assert.Equal
            (entries.Select(e => e.Text), first.Candidates.Concat(last.Candidates).Select(c => c.Candidate.Text));
    }

    [Theory]
    [InlineData("n")]
    [InlineData("ni")]
    public void IncompleteFullPinyinCannotInventTheNextSyllable(string input)
    {
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [new("你好", "ni hao", 1)], [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );

        Assert.Empty(context.QueryInterpretations(input).Candidates);
    }

    private static ImeContext Create(bool whole = false)
    {
        List<ImePhoneticEntry> entries =
        [
            new("说", "shuo", 1000), new("花", "hua", 900), new("人", "ren", 800),
            new("说话", "shuo hua", 300), new("说话人", "shuo hua ren", 100),
            new("所", "suo", 80), new("索", "suo", 70), new("梭", "suo", 60)
        ];
        if (whole)
        {
            entries.Add(new("说话人说", "shuo hua ren shuo", 1));
        }

        return new
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    entries, [],
                    ImeProjectionDefinition.ForScheme("naturalCodeDoublePinyin"), 1
                )
            )
        );
    }
}