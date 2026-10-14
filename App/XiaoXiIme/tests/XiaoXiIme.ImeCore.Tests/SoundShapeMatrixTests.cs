using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeMatrixTests
{
    [Fact]
    public void E01_LowFrequencyShapeSurvivesLimitOne()
    {
        var fixture = new InventedSoundShapeFixture("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type("niR");

        var page = fixture.Context.QueryInterpretations("niR", limit: 1);

        Assert.Equal(new[] { ("你", "ni", 40, 3, "") }, page.Candidates.Select(candidate =>
            (candidate.Candidate.Text, candidate.Candidate.Reading, candidate.Rank.Frequency,
             candidate.Interpretations.First().CoverageEnd, candidate.Interpretations.First().RemainingInput)));
    }

    [Fact]
    public void E01_ShapeEvidenceUsesOriginalInputRange()
    {
        var fixture = new InventedSoundShapeFixture("N3", InventedSoundShapeFixture.Options(ImeShapeCasePolicy.UppercaseOnly));
        fixture.Type("niR");

        var evidence = fixture.Find("你", 3, ImeInterpretationKind.Complete).Interpretations.SelectMany(path => path.Evidence)
            .Where(item => item.Kind == ImeEvidenceKind.PerCharacterShape);

        Assert.Equal(new[] { (2, 1, "R", (string?)"rx"), (2, 1, "R", (string?)"re") }.OrderBy(item => item.Item4),
            evidence.Select(item => (item.Range.Start, item.Range.Length, item.Input, item.ShapeAlias)).Distinct().OrderBy(item => item.ShapeAlias));
    }

    [Theory]
    [InlineData("A01", "ABC", "nihc", "泥好|你号|你好")]
    [InlineData("A02", "ABC", "nirhc", "你号|你好")]
    [InlineData("A03", "ABC", "nirhcn", "你好")]
    [InlineData("A04", "ABC", "nirxhc", "你号|你好")]
    [InlineData("A05", "ABC", "nirxhcnz", "你好")]
    [InlineData("A06", "ABC", "nihcr", "你号|你好", "W")]
    [InlineData("A07", "ABC", "nihcrn", "你好", "W")]
    [InlineData("A08", "AB", "nirhc", "你好", "B")]
    [InlineData("A08", "AB", "nihcr", "泥好|你好", "B")]
    [InlineData("A09", "TAIL", "nihcrnma", "你好吗", "W")]
    [InlineData("B01", "ABC", "niRhcn", "", "C", "U")]
    [InlineData("B02", "ABC", "niRhcN", "你好", "C", "U")]
    [InlineData("B03", "ABC", "niRXhcNZ", "你好", "C", "U")]
    [InlineData("B04", "ABC", "nihcRN", "你好", "W", "U")]
    [InlineData("B05", "ABC", "niRhcN", "你好")]
    [InlineData("B05", "ABC", "nirhcn", "你好")]
    [InlineData("B06", "AB", "NIrHC", "你好", "C", "L")]
    [InlineData("B09", "N3", "RX", "", "C", "U")]
    [InlineData("C01", "N3", "ni", "泥|妮|你", "C", "UG", "O1")]
    [InlineData("C01", "N3", "nir", "你", "C", "UG", "O1")]
    [InlineData("C01", "N3", "nirx", "", "C", "UG", "O1")]
    [InlineData("C02", "N3", "ni", "泥|妮|你", "C", "UG", "O2")]
    [InlineData("C02", "N3", "nir", "", "C", "UG", "O2")]
    [InlineData("C02", "N3", "nirx", "你", "C", "UG", "O2")]
    [InlineData("C03", "N3", "ni", "泥|妮|你")]
    [InlineData("C03", "N3", "nir", "你")]
    [InlineData("C03", "N3", "nire", "你")]
    [InlineData("C04", "AB", "nihc", "", "C", "UG", "M2")]
    [InlineData("C05", "AB", "nirxhc", "", "C", "UG", "M2")]
    [InlineData("C06", "AB", "nirxhcnz", "你好", "C", "UG", "M2")]
    [InlineData("C07", "N3", "nir", "", "C", "UG", "M2")]
    [InlineData("C07", "N3", "nirs", "", "C", "UG", "M2")]
    [InlineData("C08", "AB", "nihcr", "", "W", "UG", "O2")]
    [InlineData("C08", "AB", "nihcrn", "你好", "W", "UG", "O2")]
    [InlineData("D01", "PATH", "nirhc", "你好")]
    [InlineData("D02", "PATH", "nirhcc", "你让草")]
    [InlineData("D03", "ABC", "nidhc", "泥好")]
    [InlineData("D04", "ABC", "nihcd", "泥好", "W")]
    [InlineData("D05", "AB", "nihcr", "泥好|你好", "B")]
    [InlineData("D06", "XBOUND", "nihkr", "你好", "C", "UG", "O12", "X")]
    [InlineData("D09", "PATH", "nirhcc", "你让草")]
    public void Matrix_CompleteSetHasFrozenOrder(string id, string set, string input, string expected,
        string placement = "C", string casing = "UG", string length = "O12", string projection = "F")
    {
        var options = InventedSoundShapeFixture.Options(Case(casing), Placement(placement), Length(length));
        var fixture = new InventedSoundShapeFixture(set, options, projection);
        fixture.Type(input);

        Assert.True(expected.Split('|', StringSplitOptions.RemoveEmptyEntries).SequenceEqual(fixture.Complete()),
            $"{id}: unexpected complete candidate order for {input}");
    }

    [Theory]
    [InlineData("C02", "N3", "nir", "你", "C", "O2")]
    [InlineData("C05", "AB", "nirxhc", "你好", "C", "M2")]
    [InlineData("C07", "N3", "nir", "你", "C", "M2")]
    [InlineData("C08", "AB", "nihcr", "你好", "W", "O2")]
    public void Matrix_HalfShapeCannotAuthorizeConsumption(string id, string set, string input, string text, string placement, string length)
    {
        var fixture = new InventedSoundShapeFixture(set, InventedSoundShapeFixture.Options(placement: Placement(placement), length: Length(length)));
        fixture.Type(input);
        var candidate = fixture.Find(text, input.Length, ImeInterpretationKind.IncompleteShape);

        Assert.True(new[] { ((ImeSelectionId?)null, (ImeInputRange?)null, "") }.SequenceEqual(candidate.Interpretations
            .Where(path => path.Kind == ImeInterpretationKind.IncompleteShape)
            .Select(path => (candidate.SelectionId, path.ConsumedRange, path.RemainingInput)).Distinct()),
            $"{id}: incomplete shape must not authorize consumption");
    }

    [Theory]
    [InlineData("W", "M2", false, "F")]
    [InlineData("B", "M2", false, "F")]
    [InlineData("C", "O12", true, "F")]
    [InlineData("C", "M2", true, "Full")]
    public void C09_InvalidConfigurationIsRejected(string placement, string length, bool four, string projection)
    {
        var dictionary = new InMemoryImeDictionary(InventedSoundShapeFixture.Lexicon("N", projection));
        var options = InventedSoundShapeFixture.Options(placement: Placement(placement), length: Length(length),
            auto: four ? ImeAutoCommitPolicy.Four : ImeAutoCommitPolicy.Off);

        Assert.Throws<ArgumentException>(() => new ImeContext(dictionary, options, null));
    }

    [Theory]
    [InlineData(ImeShapeCasePolicy.UppercaseOnly, ImeShapePlacement.PerCharacter, ImeShapeLengthPolicy.RequiredTwo)]
    [InlineData(ImeShapeCasePolicy.UppercaseOrGuess, ImeShapePlacement.PerCharacter, ImeShapeLengthPolicy.RequiredTwo)]
    [InlineData(ImeShapeCasePolicy.LowercaseOnly, ImeShapePlacement.PerCharacter, ImeShapeLengthPolicy.RequiredTwo)]
    [InlineData(ImeShapeCasePolicy.UppercaseOnly, ImeShapePlacement.AfterWord, ImeShapeLengthPolicy.OptionalOne)]
    [InlineData(ImeShapeCasePolicy.UppercaseOrGuess, ImeShapePlacement.Both, ImeShapeLengthPolicy.OptionalTwo)]
    [InlineData(ImeShapeCasePolicy.LowercaseOnly, ImeShapePlacement.Both, ImeShapeLengthPolicy.OptionalOneOrTwo)]
    public void Configuration_PreservesExplicitDimensions(ImeShapeCasePolicy casing, ImeShapePlacement placement, ImeShapeLengthPolicy length)
    {
        var options = InventedSoundShapeFixture.Options(casing, placement, length);
        var context = new ImeContext(new InMemoryImeDictionary(InventedSoundShapeFixture.Lexicon("N")), options, null);

        Assert.Empty(context.DetailedSnapshot.UppercaseMarks);
    }

    [Theory]
    [InlineData(ImeCandidateSortPolicy.PhoneticFirst, 120, "你好哪|你好")]
    [InlineData(ImeCandidateSortPolicy.ShapeFirst, 120, "你好|你好哪")]
    [InlineData(ImeCandidateSortPolicy.Automatic, 9000, "你好|你好哪")]
    [InlineData(ImeCandidateSortPolicy.Automatic, 1, "你好哪|你好")]
    public void E04_E07_ConflictUsesFrozenSort(ImeCandidateSortPolicy sort, int frequency, string expected)
    {
        var fixture = new InventedSoundShapeFixture("CONFLICT", InventedSoundShapeFixture.Options(sort: sort), aFrequency: frequency);
        fixture.Type("nihcnz");

        Assert.Equal(expected.Split('|'), fixture.Complete());
    }

    [Theory]
    [InlineData(ImeCandidateSortPolicy.PhoneticFirst)]
    [InlineData(ImeCandidateSortPolicy.ShapeFirst)]
    [InlineData(ImeCandidateSortPolicy.Automatic)]
    public void E08_PurePhoneticOrderDoesNotRewardAliases(ImeCandidateSortPolicy sort)
    {
        var fixture = new InventedSoundShapeFixture("AB", InventedSoundShapeFixture.Options(sort: sort));
        fixture.Type("nihc");

        Assert.Equal(new[] { "泥好", "你好" }, fixture.Complete());
    }

    [Theory]
    [InlineData("A08", "AB", "nihcr")]
    [InlineData("D05", "AB", "nihcr")]
    public void Matrix_MergedCandidateRetainsBothAttachments(string id, string set, string input)
    {
        var fixture = new InventedSoundShapeFixture(set, InventedSoundShapeFixture.Options(placement: ImeShapePlacement.Both));
        fixture.Type(input);
        var candidate = fixture.Find("你好", 5, ImeInterpretationKind.Complete);

        Assert.True(new[] { ImeEvidenceKind.PerCharacterShape, ImeEvidenceKind.AfterWordShape }.Order().SequenceEqual(
            candidate.Interpretations.SelectMany(path => path.Evidence).Where(item => item.ShapeAlias != null).Select(item => item.Kind).Distinct().Order()),
            $"{id}: both attachments must survive");
    }

    private static ImeShapeCasePolicy Case(string value) => value switch
    {
        "U" => ImeShapeCasePolicy.UppercaseOnly, "L" => ImeShapeCasePolicy.LowercaseOnly, _ => ImeShapeCasePolicy.UppercaseOrGuess,
    };
    private static ImeShapePlacement Placement(string value) => value switch
    {
        "W" => ImeShapePlacement.AfterWord, "B" => ImeShapePlacement.Both, _ => ImeShapePlacement.PerCharacter,
    };
    private static ImeShapeLengthPolicy Length(string value) => value switch
    {
        "O1" => ImeShapeLengthPolicy.OptionalOne, "O2" => ImeShapeLengthPolicy.OptionalTwo,
        "M2" => ImeShapeLengthPolicy.RequiredTwo, _ => ImeShapeLengthPolicy.OptionalOneOrTwo,
    };
}
