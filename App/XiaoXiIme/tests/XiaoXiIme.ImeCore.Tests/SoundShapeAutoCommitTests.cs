using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeAutoCommitTests
{
    [Fact]
    public void H01_OffWaitsForManualSpaceWithoutSettingEatToken()
    {
        var fixture = new InventedSoundShapeFixture("AB", InventedSoundShapeFixture.Options(eat: true));
        var typed = fixture.Type("niRhcN");

        Assert.Null(typed.CommitText);
    }

    [Theory]
    [InlineData(ImeAutoCommitPolicy.Four, "n", null)]
    [InlineData(ImeAutoCommitPolicy.Four, "ni", null)]
    [InlineData(ImeAutoCommitPolicy.Four, "niR", null)]
    [InlineData(ImeAutoCommitPolicy.Four, "niRX", "你")]
    [InlineData(ImeAutoCommitPolicy.Unique, "ni", null)]
    [InlineData(ImeAutoCommitPolicy.Unique, "niR", null)]
    [InlineData(ImeAutoCommitPolicy.Unique, "niRX", "你")]
    public void H02_H03_AutomaticCommitRequiresCompleteShape(ImeAutoCommitPolicy policy, string input, string? expected)
    {
        var fixture = Four("N", policy);

        var result = fixture.Type(input);

        Assert.Equal(expected, result.CommitText);
    }

    [Theory]
    [InlineData("NP", "niRX", "拟")]
    [InlineData("NA", "niRX", "你")]
    [InlineData("A", "niRX", null)]
    [InlineData("A", "niRXhcNZ", "你好")]
    [InlineData("T", "niRXhcNZ", null)]
    [InlineData("T", "niRXhcNZwoWT", "你好我")]
    public void Four_ChoosesRankedRegisteredWordAndNeverCommitsHalfWord(string set, string input, string? expected)
    {
        var fixture = Four(set);

        var result = fixture.Type(input);

        Assert.Equal(expected, result.CommitText);
    }

    [Fact]
    public void H02_AutomaticCommitEmitsOneLearningEvent()
    {
        var fixture = Four("N");
        fixture.Type("niRX");

        Assert.Equal((1, "niRX", "ni"), (fixture.Learning.Count, fixture.Learning.Single().OriginalInput,
            fixture.Learning.Single().Segments.Single().CanonicalReading));
    }

    [Theory]
    [InlineData(ImeAutoCommitPolicy.Unique, "你好")]
    [InlineData(ImeAutoCommitPolicy.Off, null)]
    public void H04_UniqueMaySacrificeLongerPrefix(ImeAutoCommitPolicy policy, string? expected)
    {
        var fixture = new InventedSoundShapeFixture("AV", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly, auto: policy), "Full");

        var result = fixture.Type("nihao");

        Assert.Equal(expected, result.CommitText);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void H05_EatTokenConsumesOnlyFirstConfirmationSpace(int spaces, bool expected)
    {
        var fixture = Four("N");
        fixture.Type("niRX");
        var result = fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space));
        for (var index = 1; index < spaces; index++)
        {
            result = fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space));
        }

        Assert.Equal((expected, null, 1), (result.Handled, result.CommitText, fixture.Learning.Count));
    }

    [Theory]
    [InlineData(ImeKeyKind.MoveCompositionCaretLeft)]
    [InlineData(ImeKeyKind.Escape)]
    [InlineData(ImeKeyKind.Other)]
    public void H06_NonSpaceEvenUnhandledOperationClearsEatToken(ImeKeyKind key)
    {
        var fixture = Four("N");
        fixture.Type("niRX");
        fixture.Context.ProcessKey(new ImeKey(key));

        Assert.False(fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space)).Handled);
    }

    [Fact]
    public void H06_ResetClearsEatToken()
    {
        var fixture = Four("N");
        fixture.Type("niRX");
        fixture.Context.Reset();

        Assert.False(fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space)).Handled);
    }

    [Fact]
    public void H06_LetterClearsTokenEvenAfterNewCompositionIsCancelled()
    {
        var fixture = Four("N");
        fixture.Type("niRX");
        fixture.Type("m");
        fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Escape));

        Assert.False(fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space)).Handled);
    }

    [Theory]
    [InlineData(ImeAutoCommitPolicy.Four, false, "niRX")]
    [InlineData(ImeAutoCommitPolicy.Off, true, "niR")]
    public void H07_ManualOrEatDisabledCommitDoesNotSwallowSpace(ImeAutoCommitPolicy policy, bool eat, string input)
    {
        var length = policy == ImeAutoCommitPolicy.Four ? ImeShapeLengthPolicy.RequiredTwo : ImeShapeLengthPolicy.OptionalOneOrTwo;
        var fixture = new InventedSoundShapeFixture("N", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly,
            length: length, auto: policy, eat: eat));
        fixture.Type(input);
        fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.False(fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space)).Handled);
    }

    [Theory]
    [InlineData("Full", "nihao", "泥好|你号|你好", 5)]
    [InlineData("Xiaohe", "nihc", "泥好|你号|你好", 4)]
    [InlineData("Full", "niRhaoN", "你好", 7)]
    [InlineData("Xiaohe", "niRhcN", "你好", 6)]
    public void H08_RealProjectionIsSeparateFromInventedFX(string projection, string input, string expected, int end)
    {
        var fixture = new InventedSoundShapeFixture("ABC", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly), projection);
        fixture.Type(input);

        Assert.Equal(expected.Split('|').Select(text => (text, "ni hao", end)), fixture.Context.QueryInterpretations(input).Candidates
            .Where(candidate => candidate.Interpretations.Any(path => path.Kind == ImeInterpretationKind.Complete))
            .Select(candidate => (candidate.Candidate.Text, candidate.Candidate.Reading, candidate.Interpretations.First().CoverageEnd)));
    }

    [Theory]
    [InlineData(false, null, "xx")]
    [InlineData(true, "小希", "")]
    public void H09_NewModeXxTriggerIsExplicit(bool enabled, string? expected, string remaining)
    {
        var lexicon = new ImeLexicon([new("小希", "xiao xi", 100)], [], ImeProjectionDefinition.FullPinyin, 1)
        { LookupAliases = [new("小希", "xiao xi", "xx")] };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon),
            InventedSoundShapeFixture.Options() with { EnableLegacyXx = enabled }, null);
        context.ProcessKey(ImeKey.FromCharacter('x'));

        var result = context.ProcessKey(ImeKey.FromCharacter('x'));

        Assert.Equal((expected, remaining), (result.CommitText, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void H09_DisabledXxCanStillBeManuallySelectedWithCanonicalLearning()
    {
        var learning = new List<ImeDictionaryLearning>();
        var lexicon = new ImeLexicon([new("小希", "xiao xi", 100)], [], ImeProjectionDefinition.FullPinyin, 1)
        { LookupAliases = [new("小希", "xiao xi", "xx")] };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), learning.Add);
        SoundShapeFuzzyTests.Type(context, "xx");
        context.SelectCandidate(context.QueryInterpretations("xx").Candidates.Single().SelectionId!.Value);

        Assert.Equal((1, "xiao xi"), (learning.Count, learning.Single().Segments.Single().CanonicalReading));
    }

    private static InventedSoundShapeFixture Four(string set, ImeAutoCommitPolicy policy = ImeAutoCommitPolicy.Four) =>
        new(set, InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly,
            length: ImeShapeLengthPolicy.RequiredTwo, auto: policy, eat: true));
}
