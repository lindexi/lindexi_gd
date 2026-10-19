using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeCompletenessTests
{
    [Fact]
    public void Pagination_All101SemanticCandidatesRemainReachable()
    {
        var context = CreateMany();
        SoundShapeFuzzyTests.Type(context, "ni");

        var page = context.QueryInterpretations("ni", offset: 100, limit: 1);

        Assert.Equal((101, false, "目"), (page.TotalCount, page.HasMore, page.Candidates.Single().Candidate.Text));
    }

    [Fact]
    public void Pagination_AllPagesMatchUntruncatedSemanticSet()
    {
        var context = CreateMany();
        var first = context.QueryInterpretations("ni", limit: 100);
        var last = context.QueryInterpretations("ni", offset: 100, limit: 100);

        Assert.Equal((101, true, false, 101), (first.TotalCount, first.HasMore, last.HasMore,
            first.Candidates.Concat(last.Candidates).Select(c => c.Candidate.Text).Distinct().Count()));
    }

    [Fact]
    public void E01_OneHundredHigherFrequencyEntriesCannotHideUniqueShapeTarget()
    {
        var context = CreateMany();
        SoundShapeFuzzyTests.Type(context, "niR");

        Assert.Equal(new[] { "目" }, context.QueryInterpretations("niR", limit: 1).Candidates.Select(candidate => candidate.Candidate.Text));
    }

    [Fact]
    public void Pagination_SelectionIdentityDoesNotDependOnPageOffset()
    {
        var context = CreateMany();
        SoundShapeFuzzyTests.Type(context, "ni");

        Assert.Equal(context.QueryInterpretations("ni", offset: 99, limit: 2).Candidates.Last().SelectionId,
            context.QueryInterpretations("ni", offset: 100, limit: 1).Candidates.Single().SelectionId);
    }

    [Fact]
    public void Unique_CountsAllCandidatesBeforeSnapshotPagination()
    {
        var context = CreateMany(ImeAutoCommitPolicy.Unique);
        context.ProcessKey(ImeKey.FromCharacter('n'));

        Assert.Null(context.ProcessKey(ImeKey.FromCharacter('i')).CommitText);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public void Pagination_InvalidBoundsThrow(int offset, int limit)
    {
        var fixture = new InventedSoundShapeFixture("N");

        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Context.QueryInterpretations("ni", offset, limit));
    }

    [Fact]
    public void Pagination_OffsetPastTotalIsEmpty()
    {
        var fixture = new InventedSoundShapeFixture("N");

        Assert.Empty(fixture.Context.QueryInterpretations("ni", offset: 1).Candidates);
    }

    [Fact]
    public void Query_EmptyInputHasZeroTotal()
    {
        var fixture = new InventedSoundShapeFixture("N");

        Assert.Equal(0, fixture.Context.QueryInterpretations("").TotalCount);
    }

    [Fact]
    public void Query_Over64CharactersIsRejected()
    {
        var fixture = new InventedSoundShapeFixture("N");

        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.Context.QueryInterpretations(new string('n', 65)));
    }

    [Theory]
    [InlineData("ni1")]
    [InlineData("你")]
    [InlineData("ni ")]
    public void Query_NonAsciiLetterIsRejected(string input)
    {
        var fixture = new InventedSoundShapeFixture("N");

        Assert.Throws<ArgumentException>(() => fixture.Context.QueryInterpretations(input));
    }

    [Fact]
    public void Input_65thLetterIsRejectedWithoutChangingRevisionOrRawInput()
    {
        var fixture = new InventedSoundShapeFixture("N");
        fixture.Type(new string('n', 64));
        var revision = fixture.Context.DetailedSnapshot.Revision;

        var result = fixture.Context.ProcessKey(ImeKey.FromCharacter('n'));

        Assert.Equal((false, new string('n', 64), 64, revision), (result.Handled,
            result.Snapshot.Composition.Reading, result.Snapshot.Composition.CaretIndex, fixture.Context.DetailedSnapshot.Revision));
    }

    [Fact]
    public void SetComposition_NonemptyReplacementPreservesExplicitCase()
    {
        var fixture = new InventedSoundShapeFixture("N");
        fixture.Type("ni");

        var result = fixture.Context.SetComposition("niRX");

        Assert.Equal((true, "niRX"), (result.Handled, result.Snapshot.Composition.Reading));
    }

    [Fact]
    public void SetComposition_IdenticalInputDoesNotChangeRevision()
    {
        var fixture = new InventedSoundShapeFixture("N");
        fixture.Type("ni");
        var revision = fixture.Context.DetailedSnapshot.Revision;
        fixture.Context.SetComposition("ni");

        Assert.Equal(revision, fixture.Context.DetailedSnapshot.Revision);
    }

    [Fact]
    public void SetComposition_EmptyInputClearsComposition()
    {
        var fixture = new InventedSoundShapeFixture("N");
        fixture.Type("ni");
        fixture.Context.SetComposition("");

        Assert.Equal("", fixture.Context.Snapshot.Composition.Reading);
    }

    [Theory]
    [InlineData("F", "niRhcN")]
    [InlineData("X", "nihkr")]
    public void CompileLexicon_RealRoundtripPreservesQuerySemantics(string projection, string input)
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("ABC", projection);
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.SoundShape", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.CompileLexicon(lexicon, path);
        var memory = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), null);
        var compiled = new ImeContext(DictionaryPackageLoader.Load(path), InventedSoundShapeFixture.Options(), null);

        Assert.Equal(Semantics(memory.QueryInterpretations(input)), Semantics(compiled.QueryInterpretations(input)));
    }

    [Theory]
    [InlineData("Full", "zuangsheng")]
    [InlineData("Xiaohe", "zlug")]
    public void CompileLexicon_TsCollisionAndFuzzyClosureMatchMemory(string projection, string input)
    {
        var lexicon = InventedSoundShapeFixture.TS(projection);
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.SoundShape", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.CompileLexicon(lexicon, path);
        var options = InventedSoundShapeFixture.Options(rules: [ImeFuzzyRule.ZhZ, ImeFuzzyRule.ShS, ImeFuzzyRule.EngEn, ImeFuzzyRule.UanUang]);
        var memory = new ImeContext(new InMemoryImeDictionary(lexicon), options, null);
        var compiled = new ImeContext(DictionaryPackageLoader.Load(path), options, null);

        Assert.Equal(Semantics(memory.QueryInterpretations(input)), Semantics(compiled.QueryInterpretations(input)));
    }

    [Fact]
    public void NewMode_UnsupportedOldMemoryConstructorIsExplicitlyRejected()
    {
        var dictionary = new InMemoryImeDictionary([new ImeCandidate("你", "ni", 40)]);

        Assert.Throws<NotSupportedException>(() => new ImeContext(dictionary, InventedSoundShapeFixture.Options(), null));
    }

    [Fact]
    public void PrefixOnly_SpaceDoesNotCommitOrLearn()
    {
        var fixture = new InventedSoundShapeFixture("A");
        fixture.Type("n");

        var result = fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.Equal((true, null, "n", 1, 0), (result.Handled, result.CommitText,
            result.Snapshot.Composition.Reading, result.Snapshot.Composition.CaretIndex, fixture.Learning.Count));
    }

    [Fact]
    public void HalfShapeOnly_SpaceDoesNotCommitOrLearn()
    {
        var fixture = new InventedSoundShapeFixture("N", InventedSoundShapeFixture.Options(length: ImeShapeLengthPolicy.RequiredTwo));
        fixture.Type("nir");

        var result = fixture.Context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.Equal((true, null, "nir", 3, 0), (result.Handled, result.CommitText,
            result.Snapshot.Composition.Reading, result.Snapshot.Composition.CaretIndex, fixture.Learning.Count));
    }

    private static ImeContext CreateMany(ImeAutoCommitPolicy auto = ImeAutoCommitPolicy.Off)
    {
        var entries = Enumerable.Range(0, 100).Select(i => new ImePhoneticEntry(((char)(0x4E00 + i)).ToString(), "ni", 1000 - i))
            .Append(new ImePhoneticEntry("目", "ni", 1)).ToArray();
        var lexicon = new ImeLexicon(entries, [new("目", "rx", "人工夹具")],
            InventedSoundShapeFixture.Lexicon("N").Projection, 1);
        return new ImeContext(new InMemoryImeDictionary(lexicon),
            InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly, auto: auto), null);
    }

    private static IEnumerable<string> Semantics(ImeConversionPage page) => page.Candidates.Select(candidate =>
        $"{candidate.Candidate.Text}|{candidate.Candidate.Reading}|{candidate.Rank}|" + string.Join(';', candidate.Interpretations.Select(path =>
            $"{path.Kind}:{path.CoverageEnd}:{path.ConsumedRange}:{path.RemainingInput}:" + string.Join(',', path.Evidence))));
}
