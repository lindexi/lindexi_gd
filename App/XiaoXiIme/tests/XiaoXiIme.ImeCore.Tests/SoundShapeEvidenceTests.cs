using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeEvidenceTests
{
    [Theory]
    [InlineData("B02", "niRhcN", "False|False|True|False|False|True")]
    [InlineData("B03", "niRXhcNZ", "False|False|True|True|False|False|True|True")]
    [InlineData("B05", "nirhcn", "False|False|False|False|False|False")]
    public void CaseMarks_AreStoredPerOriginalCharacter(string id, string input, string expected)
    {
        var fixture = new InventedSoundShapeFixture("ABC");
        fixture.Type(input);

        Assert.True
        (
            expected == string.Join('|', fixture.Context.DetailedSnapshot.UppercaseMarks),
            $"{id}: original character marks differ"
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void E02_AdditionalShapeAliasDoesNotIncreaseFrequency(bool secondAlias)
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("N3");
        var aliases = lexicon.ShapeEntries.Where(entry => entry.Text != "你" || entry.ShapeCode == "rx").ToList();
        aliases.AddRange
        (
            lexicon.ShapeEntries.Where(entry => entry.Text == "你" && entry.ShapeCode == "re").Take(secondAlias ? 1 : 0)
        );
        var context = new ImeContext
        (
            new InMemoryImeDictionary(lexicon with { ShapeEntries = aliases }), InventedSoundShapeFixture.Options(),
            null
        );
        SoundShapeFuzzyTests.Type(context, "nir");

        Assert.Equal
        (
            new[] { ("你", 40) }, context.QueryInterpretations("nir").Candidates
                .Where(candidate => candidate.Interpretations.Any(path => path.Kind == ImeInterpretationKind.Complete))
                .Select(candidate => (candidate.Candidate.Text, candidate.Rank.Frequency))
        );
    }

    [Fact]
    public void E03_RepeatedAliasDoesNotDuplicateCandidateOrIncreaseFrequency()
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("AB");
        var context = new ImeContext
        (
            new InMemoryImeDictionary
            (
                lexicon with
                {
                    ShapeEntries = lexicon.ShapeEntries.Concat
                        ([new ShapeDictionaryEntry("好", "rz", "人工夹具")]).ToArray()
                }
            ),
            InventedSoundShapeFixture.Options(placement: ImeShapePlacement.Both), null
        );
        SoundShapeFuzzyTests.Type(context, "nihcr");

        Assert.Equal
        (
            new[] { ("泥好", 900), ("你好", 120) }, context.QueryInterpretations("nihcr").Candidates
                .Where(candidate => candidate.Interpretations.Any(path => path.Kind == ImeInterpretationKind.Complete))
                .Select(candidate => (candidate.Candidate.Text, candidate.Rank.Frequency))
        );
    }

    public static IEnumerable<object[]> ValidDimensions()
    {
        foreach (var casing in Enum.GetValues<ImeShapeCasePolicy>())
        {
            foreach (var placement in Enum.GetValues<ImeShapePlacement>())
            {
                foreach (var length in new[]
                         {
                             ImeShapeLengthPolicy.OptionalOne, ImeShapeLengthPolicy.OptionalTwo,
                             ImeShapeLengthPolicy.OptionalOneOrTwo
                         })
                {
                    yield return [casing, placement, length];
                }
            }

            yield return [casing, ImeShapePlacement.PerCharacter, ImeShapeLengthPolicy.RequiredTwo];
        }
    }

    [Theory]
    [MemberData(nameof(ValidDimensions))]
    public void Configuration_AllLegalDimensionsSelectAndLearn
        (ImeShapeCasePolicy casing, ImeShapePlacement placement, ImeShapeLengthPolicy length)
    {
        var fixture = new InventedSoundShapeFixture("N", InventedSoundShapeFixture.Options(casing, placement, length));
        var shape = length == ImeShapeLengthPolicy.OptionalOne ? "r" : "rx";
        var input = "ni" + (casing == ImeShapeCasePolicy.LowercaseOnly ? shape : shape.ToUpperInvariant());
        fixture.Type(input);
        var candidate = fixture.Context.QueryInterpretations(input).Candidates.Single
        (c => c.SelectionId.HasValue &&
              c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Complete)
        );
        var result = fixture.Context.SelectCandidate(candidate.SelectionId.GetValueOrDefault());

        Assert.Equal
        (
            ("你", "", "ni", input, 1), (result.CommitText, result.Snapshot.Composition.Reading,
                fixture.Learning.Single().Segments.Single().CanonicalReading, fixture.Learning.Single().OriginalInput,
                fixture.Learning.Count)
        );
    }

    [Theory]
    [MemberData(nameof(ValidDimensions))]
    public void Configuration_AllLegalThreeDimensionalCombinationsConstruct
        (ImeShapeCasePolicy casing, ImeShapePlacement placement, ImeShapeLengthPolicy length)
    {
        var fixture = new InventedSoundShapeFixture("N", InventedSoundShapeFixture.Options(casing, placement, length));

        Assert.Equal(0, fixture.Context.DetailedSnapshot.Page.TotalCount);
    }
}