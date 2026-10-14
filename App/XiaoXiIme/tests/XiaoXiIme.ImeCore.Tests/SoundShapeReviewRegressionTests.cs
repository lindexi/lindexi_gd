using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeReviewRegressionTests
{
    [Fact]
    public void R02_WholeWordAndSegmentedPathsBothSurvive()
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("A") with
        { Entries = [new("你好", "ni hao", 120), new("你", "ni", 40), new("好", "hao", 40)] };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), null);
        Assert.Equal(new[] { 1, 2 }, context.QueryInterpretations("nihc").Candidates.Single(c => c.Candidate.Text == "你好")
            .Interpretations.Select(p => p.Segments.Count).Distinct().Order().ToArray());
    }

    [Fact]
    public void R02_BestInterpretationMatchesCandidateAndLearning()
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("N") with
        {
            Entries = [new("你", "ni", 40), new("你", "nin", 500)],
            Projection = new("collision", [new("n", "in", "ni"), new("n", "i", "ni")]),
        };
        var learning = new List<ImeDictionaryLearning>();
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), learning.Add);
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('i'));
        var candidate = context.QueryInterpretations("ni").Candidates.Single(c => c.SelectionId.HasValue);
        context.SelectCandidate(candidate.SelectionId.GetValueOrDefault());
        Assert.Equal(("nin", "nin"), (candidate.Candidate.Reading, learning.Single().Segments.Single().CanonicalReading));
    }

    [Theory]
    [InlineData("n", ImeInterpretationKind.Prefix)]
    [InlineData("niR", ImeInterpretationKind.IncompleteShape)]
    public void R03_PendingHomographsKeepCanonicalReading(string input, ImeInterpretationKind kind)
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("N") with
        {
            Entries = [new("你", "ni", 40), new("你", "nin", 500)],
            Projection = new("collision", [new("n", "i", "ni"), new("n", "in", "ni")]),
        };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), null);
        Assert.Equal(new[] { "ni", "nin" }, context.QueryInterpretations(input).Candidates
            .Where(c => c.Interpretations[0].Kind == kind).Select(c => c.Candidate.Reading).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void R03_PendingWholeWordAndSegmentedStatesStaySeparate()
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("A") with
        { Entries = [new("你好", "ni hao", 120), new("你", "ni", 40), new("好", "hao", 40)] };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), null);
        Assert.Equal(new[] { 0, 1 }, context.QueryInterpretations("nih").Candidates
            .Where(c => c.Candidate.Text == "你好" && c.Interpretations[0].Kind == ImeInterpretationKind.Prefix)
            .Select(c => c.Interpretations[0].Segments.Count).Order().ToArray());
    }

    [Fact]
    public void R04_FourCommitsCompleteMultiSegmentPath()
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("A") with
        { Entries = [new("你", "ni", 40), new("好", "hao", 40)] };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon),
            InventedSoundShapeFixture.Options(length: ImeShapeLengthPolicy.RequiredTwo, auto: ImeAutoCommitPolicy.Four), null);
        foreach (var character in "hcN") context.ProcessKey(ImeKey.FromCharacter(character));
        for (var i = 0; i < 3; i++) context.ProcessKey(ImeKey.MoveCompositionCaretLeft());
        foreach (var character in "niRX") context.ProcessKey(ImeKey.FromCharacter(character));
        for (var i = 0; i < 3; i++) context.ProcessKey(ImeKey.MoveCompositionCaretRight());
        Assert.Equal("你好", context.ProcessKey(ImeKey.FromCharacter('Z')).CommitText);
    }

    [Fact]
    public void R05_XxShortcutSetsEatConfirmationToken()
    {
        var lexicon = new ImeLexicon([new("小希", "xiao xi", 40)], [], ImeProjectionDefinition.FullPinyin, 1)
        { LookupAliases = [new("小希", "xiao xi", "xx")] };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon),
            InventedSoundShapeFixture.Options(eat: true) with { EnableLegacyXx = true }, null);
        context.ProcessKey(ImeKey.FromCharacter('x'));
        context.ProcessKey(ImeKey.FromCharacter('x'));
        Assert.True(context.ProcessKey(new ImeKey(ImeKeyKind.Space)).Handled);
    }

    [Fact]
    public void R01_LearnedSystemEntryRanksAheadOfUnlearnedSystemEntry()
    {
        var dictionary = new UserDictionary(new InMemoryImeDictionary(InventedSoundShapeFixture.Lexicon("N") with { Entries = [new("你", "ni", 40), new("泥", "ni", 500)] }));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("你", "ni", 40), "ni"));
        var context = new ImeContext(dictionary, InventedSoundShapeFixture.Options(), null);

        var candidates = context.QueryInterpretations("ni").Candidates;

        Assert.Equal("你", candidates[0].Candidate.Text);
    }
}
