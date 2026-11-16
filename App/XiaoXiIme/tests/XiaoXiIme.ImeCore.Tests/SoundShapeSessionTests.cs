using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeSessionTests
{
    [Theory]
    [InlineData("D07", 4, "r", "nihk", 1)]
    [InlineData("D08", 5, "", "nihkr", 0)]
    public void D07_D08_SelectionConsumesOnlyChosenBoundary
        (string id, int end, string remaining, string consumed, int caret)
    {
        var fixture = new InventedSoundShapeFixture("XBOUND", projection: "X");
        fixture.Type("nihkr");

        var result = fixture.Pick("你好", end, end == 4 ? ImeInterpretationKind.Partial : ImeInterpretationKind.Complete);

        Assert.True
        (
            ("你好", remaining, caret, consumed, "ni hao", 1) ==
            (result.CommitText, result.Snapshot.Composition.Reading, result.Snapshot.Composition.CaretIndex,
                fixture.Learning.Single().OriginalInput, fixture.Learning.Single().Segments.Single().CanonicalReading,
                fixture.Learning.Count),
            $"{id}: selection boundary or canonical learning differs"
        );
    }

    [Fact]
    public void D07_RemainingRHasNoCandidatesOrUppercaseMark()
    {
        var fixture = new InventedSoundShapeFixture("XBOUND", projection: "X");
        fixture.Type("nihkr");
        fixture.Pick("你好", 4, ImeInterpretationKind.Partial);

        Assert.Equal
        (
            ("r", "False", 0), (fixture.Context.Snapshot.Composition.Reading,
                string.Join('|', fixture.Context.DetailedSnapshot.UppercaseMarks),
                fixture.Context.DetailedSnapshot.Page.TotalCount)
        );
    }

    [Fact]
    public void D06_SameTextDifferentConsumptionHasDifferentSelectionIdentity()
    {
        var fixture = new InventedSoundShapeFixture("XBOUND", projection: "X");
        fixture.Type("nihkr");

        Assert.NotEqual
        (
            fixture.Find("你好", 5, ImeInterpretationKind.Complete).SelectionId,
            fixture.Find("你好", 4, ImeInterpretationKind.Partial).SelectionId
        );
    }

    [Theory]
    [InlineData("D01", "nirhc", "你让草", ImeInterpretationKind.Complete, 5)]
    [InlineData("D02", "nirhcc", "你好", ImeInterpretationKind.Partial, 5)]
    [InlineData("D09", "nirhcc", "你好", ImeInterpretationKind.Partial, 5)]
    public void D01_D02_D09_IndependentPathRemainsVisible
        (string id, string input, string text, ImeInterpretationKind kind, int end)
    {
        var fixture = new InventedSoundShapeFixture("PATH");
        fixture.Type(input);

        Assert.True
        (
            kind == fixture.Find(text, end, kind).Interpretations.First().Kind,
            $"{id}: independent path kind differs"
        );
    }

    [Theory]
    [InlineData("A09", "nihcrnma", 6, ImeShapePlacement.AfterWord, ImeShapeCasePolicy.UppercaseOrGuess)]
    [InlineData("G01", "niRXhcNZma", 8, ImeShapePlacement.PerCharacter, ImeShapeCasePolicy.UppercaseOnly)]
    public void A09_G01_LocalSelectionRequeriesRawRemainingInput
        (string id, string input, int end, ImeShapePlacement placement, ImeShapeCasePolicy casing)
    {
        var fixture = new InventedSoundShapeFixture("TAIL", InventedSoundShapeFixture.Options(casing, placement));
        fixture.Type(input);

        var result = fixture.Pick("你好", end, ImeInterpretationKind.Partial);

        Assert.True
        (
            ("你好", "ma", 2, "吗", "False|False") == (result.CommitText, result.Snapshot.Composition.Reading,
                result.Snapshot.Composition.CaretIndex, string.Join('|', fixture.Complete()),
                string.Join('|', fixture.Context.DetailedSnapshot.UppercaseMarks)),
            $"{id}: remaining input was not requeried verbatim"
        );
    }

    [Fact]
    public void G02_SecondSpaceLearnsOnlyRemainingRegisteredWord()
    {
        var fixture = new InventedSoundShapeFixture
            ("TAIL", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type("niRXhcNZma");
        fixture.Pick("你好", 8, ImeInterpretationKind.Partial);
        fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.Equal
        (
            new[] { ("你好", "ni hao", "niRXhcNZ"), ("吗", "ma", "ma") }, fixture.Learning.Select
            (value =>
                (value.Segments.Single().Text, value.Segments.Single().CanonicalReading, value.OriginalInput)
            )
        );
    }

    [Fact]
    public void A09_CompleteSequenceEmitsOneLearningEventWithTwoCanonicalSegments()
    {
        var fixture = new InventedSoundShapeFixture
            ("TAIL", InventedSoundShapeFixture.Options(placement: ImeShapePlacement.AfterWord));
        fixture.Type("nihcrnma");
        fixture.Pick("你好吗", 8);

        Assert.Equal
        (
            new[]
            {
                new ImeConfirmedSegment("你好", "ni hao", 0, 6, 0, 2), new ImeConfirmedSegment("吗", "ma", 6, 2, 2, 1)
            },
            fixture.Learning.Single().Segments
        );
    }

    [Theory]
    [InlineData("B07", "niR", "nR", "False|True", 1)]
    [InlineData("G03", "niRX", "niX", "False|False|True", 2)]
    public void B07_G03_EditPreservesOriginalCharacterMarks
        (string id, string input, string expected, string marks, int caret)
    {
        var fixture = new InventedSoundShapeFixture
            ("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type(input);
        fixture.Context.ProcessKey(ImeKey.MoveCompositionCaretLeft());
        fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.True
        (
            (expected, marks, caret, 0) == (fixture.Context.Snapshot.Composition.Reading,
                string.Join('|', fixture.Context.DetailedSnapshot.UppercaseMarks),
                fixture.Context.Snapshot.Composition.CaretIndex, fixture.Learning.Count),
            $"{id}: editing changed original marks or learned unexpectedly"
        );
    }

    [Fact]
    public void G04_DeletingLastShapeRebuildsEvidenceWithoutLearning()
    {
        var fixture = new InventedSoundShapeFixture
            ("AB", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type("niRhcN");
        fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.Equal
        (
            ("niRhc", "你好", 0),
            (fixture.Context.Snapshot.Composition.Reading, string.Join('|', fixture.Complete()), fixture.Learning.Count)
        );
    }

    [Theory]
    [InlineData("rx")]
    [InlineData("RX")]
    public void B08_B09_IndependentShapeQueryIsCaseInsensitiveAndReadOnly(string input)
    {
        var fixture = new InventedSoundShapeFixture
            ("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type("niR");
        var before = fixture.Context.DetailedSnapshot;

        var shapes = fixture.Context.QueryShape(input);

        Assert.Equal
        (
            new[] { ("你", "rx", "niR", before.Revision, 3, 0) }, shapes.Select
            (shape =>
                (shape.Text, shape.ShapeCode, fixture.Context.Snapshot.Composition.Reading,
                    fixture.Context.DetailedSnapshot.Revision,
                    fixture.Context.Snapshot.Composition.CaretIndex, fixture.Learning.Count)
            )
        );
    }

    [Fact]
    public void G05_QueryAndMovementDoNotLearnButMergedSelectionLearnsOnce()
    {
        var fixture = new InventedSoundShapeFixture
            ("AB", InventedSoundShapeFixture.Options(placement: ImeShapePlacement.Both));
        fixture.Type("nihcr");
        fixture.Context.QueryInterpretations("nihcr");
        fixture.Context.ProcessKey(ImeKey.NextCandidate());
        fixture.Pick("你好", 5);

        Assert.Equal
        (
            (1, "nihcr", "ni hao"), (fixture.Learning.Count, fixture.Learning.Single().OriginalInput,
                fixture.Learning.Single().Segments.Single().CanonicalReading)
        );
    }

    [Fact]
    public void G06_FuzzyCommitLearnsCanonicalReadingExactlyOnce()
    {
        var learning = new List<ImeDictionaryLearning>();
        var context = new ImeContext
        (
            new InMemoryImeDictionary(InventedSoundShapeFixture.Z()),
            InventedSoundShapeFixture.Options(rules: [ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn]), learning.Add
        );
        SoundShapeFuzzyTests.Type(context, "zan");
        context.SelectCandidate
        (
            context.QueryInterpretations("zan").Candidates.Single
            (candidate => candidate.Candidate.Text == "张"
                          && candidate.Interpretations.Any(path => path.Kind == ImeInterpretationKind.Complete)
            ).SelectionId!.Value
        );

        Assert.Equal
        (
            (1, "zan", new ImeConfirmedSegment("张", "zhang", 0, 3, 0, 1)),
            (learning.Count, learning.Single().OriginalInput, learning.Single().Segments.Single())
        );
    }

    [Fact]
    public void G07_ResetClearsAllCompositionStateAndInvalidatesSelection()
    {
        var fixture = new InventedSoundShapeFixture
            ("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type("niR");
        var old = fixture.Find("你", 3, ImeInterpretationKind.Complete).SelectionId!.Value;
        fixture.Context.Reset();
        var result = fixture.Context.SelectCandidate(old);

        Assert.Equal
        (
            (false, null, "", 0, 0, 0, 0), (result.Handled, result.CommitText, result.Snapshot.Composition.Reading,
                result.Snapshot.Composition.CaretIndex, fixture.Context.DetailedSnapshot.Page.TotalCount,
                fixture.Context.DetailedSnapshot.UppercaseMarks.Count, fixture.Learning.Count)
        );
    }

    [Fact]
    public void G08_ResetDoesNotLeakShapeConstraintAcrossContexts()
    {
        var first = new InventedSoundShapeFixture
            ("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        var second = new InventedSoundShapeFixture
            ("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        first.Type("niR");
        first.Context.Reset();
        first.Type("ni");
        second.Type("niR");

        Assert.Equal(("泥|妮|你", "你"), (string.Join('|', first.Complete()), string.Join('|', second.Complete())));
    }

    [Theory]
    [InlineData(ImeKeyKind.Enter, "niR")]
    [InlineData(ImeKeyKind.Escape, null)]
    public void G09_EnterOrEscapeNeverLearnsCandidate(ImeKeyKind key, string? expected)
    {
        var fixture = new InventedSoundShapeFixture
            ("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type("niR");

        var result = fixture.Context.ProcessKey(new ImeKey(key));

        Assert.Equal
            ((expected, "", 0), (result.CommitText, result.Snapshot.Composition.Reading, fixture.Learning.Count));
    }

    [Fact]
    public void Selection_QueryPreservesIdentityButEditInvalidatesOldIdentity()
    {
        var fixture = new InventedSoundShapeFixture("N3");
        fixture.Type("ni");
        var old = fixture.Find("你", 2, ImeInterpretationKind.Complete).SelectionId!.Value;
        fixture.Type("r");

        Assert.False(fixture.Context.SelectCandidate(old).Handled);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Selection_ForgedOrdinalPreservesCompositionAndLearning(int ordinal)
    {
        var fixture = new InventedSoundShapeFixture("N3");
        fixture.Type("ni");
        var valid = fixture.Find("你", 2, ImeInterpretationKind.Complete).SelectionId.GetValueOrDefault();
        var result = fixture.Context.SelectCandidate(valid with { Ordinal = ordinal });

        Assert.Equal
        (
            (false, null, "ni", 0), (result.Handled, result.CommitText,
                result.Snapshot.Composition.Reading, fixture.Learning.Count)
        );
    }

    [Fact]
    public void Selection_CrossContextIdentityCannotCommit()
    {
        var first = new InventedSoundShapeFixture("N3");
        var second = new InventedSoundShapeFixture("N3");
        first.Type("ni");
        second.Type("ni");

        Assert.False
        (
            second.Context.SelectCandidate
                (first.Find("你", 2, ImeInterpretationKind.Complete).SelectionId!.Value).Handled
        );
    }
}