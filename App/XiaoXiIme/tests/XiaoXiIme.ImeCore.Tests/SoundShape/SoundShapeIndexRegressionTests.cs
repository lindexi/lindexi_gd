using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore.Tests;

public class SoundShapeIndexRegressionTests
{
    [Fact]
    public void AliasWithDifferentFirstKeyContinuesIntoNextSegment()
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("A") with
        {
            Entries = [new("你好", "ni hao", 120), new("我", "wo", 70)],
            LookupAliases = [new("你好", "ni hao", "hw")],
        };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), null);
        Assert.Contains
        (
            context.QueryInterpretations("hwwo").Candidates, c => c.Candidate.Text == "你好我" &&
                                                                  c.Interpretations.Any
                                                                  (p => p.Kind == ImeInterpretationKind.Complete &&
                                                                        p.Segments.Select
                                                                            (s => s.CanonicalReading).SequenceEqual
                                                                            (new[] { "ni hao", "wo" })
                                                                  )
        );
    }

    [Fact]
    public void DifferentSegmentsAtSamePositionRetainBothEvidencePaths()
    {
        var lexicon = InventedSoundShapeFixture.Lexicon("A") with
        {
            Entries = [new("你好", "ni hao", 120), new("你", "ni", 40), new("好", "hao", 40), new("我", "wo", 70)],
        };
        var context = new ImeContext(new InMemoryImeDictionary(lexicon), InventedSoundShapeFixture.Options(), null);
        var candidate = context.QueryInterpretations("nihcwo").Candidates.Single
        (c => c.Candidate.Text == "你好我" &&
              c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Complete)
        );
        Assert.Equal
        (
            new[] { "ni hao|wo" }, candidate.Interpretations
                .Where(p => p.Kind == ImeInterpretationKind.Complete)
                .Select(p => string.Join('|', p.Segments.Select(s => s.CanonicalReading)))
                .Distinct().Order(StringComparer.Ordinal).ToArray()
        );
    }
}