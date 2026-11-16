using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class SoundShapeLexiconContractTests
{
    [Fact]
    public void InMemory_LexiconRetainsCanonicalReadingAndFixedFrequency()
    {
        var dictionary = new InMemoryImeDictionary(Create("F"));

        Assert.Equal
        (
            new[] { ("你", "ni", 40), ("你好", "ni hao", 120) }, dictionary.GetLexicon().Entries
                .Select(entry => (entry.Text, entry.CanonicalReading, entry.BaseFrequency)).OrderBy
                    (entry => entry.Text, StringComparer.Ordinal)
        );
    }

    [Theory]
    [InlineData("F")]
    [InlineData("X")]
    public void CompileLexicon_LoadRestoresCustomProjection(string name)
    {
        var lexicon = Create(name);
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.SoundShape", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.CompileLexicon(lexicon, path);

        var loaded = DictionaryPackageLoader.Load(path).GetLexicon();

        Assert.Equal
        (
            (lexicon.Projection.Name, string.Join('|', lexicon.Projection.Entries), true),
            (loaded.Projection.Name, string.Join('|', loaded.Projection.Entries), loaded.Projection.IsFixedTwoKey)
        );
    }

    [Fact]
    public void CompileLexicon_LoadRestoresCanonicalEntriesShapesAndAliases()
    {
        var lexicon = Create("F");
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.SoundShape", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.CompileLexicon(lexicon, path);

        var loaded = DictionaryPackageLoader.Load(path).GetLexicon();

        Assert.Equal
        (
            (string.Join('|', lexicon.Entries), string.Join('|', lexicon.ShapeEntries),
                string.Join('|', lexicon.LookupAliases)),
            (string.Join('|', loaded.Entries), string.Join('|', loaded.ShapeEntries),
                string.Join('|', loaded.LookupAliases))
        );
    }

    [Fact]
    public void InMemory_DuplicateCanonicalEntryDoesNotAddFrequency()
    {
        var lexicon = Create("F");
        var dictionary = new InMemoryImeDictionary
        (
            lexicon with
            {
                Entries = [new("你", "ni", 40), new("你", "ni", 30)], LookupAliases = []
            }
        );

        Assert.Equal(40, dictionary.GetLexicon().Entries.Single().BaseFrequency);
    }

    [Fact]
    public void InMemory_DuplicateShapeAliasIsNormalized()
    {
        var lexicon = Create("F");
        var dictionary = new InMemoryImeDictionary
        (
            lexicon with
            {
                ShapeEntries = [new("你", "rx", "人工"), new("你", "rx", "人工")]
            }
        );

        Assert.Single(dictionary.GetLexicon().ShapeEntries);
    }

    [Fact]
    public void OldMemoryConstructor_CannotInventCanonicalReadingFromAbbreviation()
    {
        var dictionary = new InMemoryImeDictionary([new ImeCandidate("小希", "xx", 100)]);

        Assert.Throws<NotSupportedException>(() => dictionary.GetLexicon());
    }

    private static ImeLexicon Create(string name) => new
        (
            [new("你", "ni", 40), new("你好", "ni hao", 120)],
            [new("你", "rx", "人工"), new("你", "re", "人工"), new("好", "nz", "人工"), new("好", "rz", "人工")],
            new ImeProjectionDefinition(name, [new("n", "i", "ni"), new("h", "ao", name == "X" ? "hk" : "hc")]), 1
        )
        { LookupAliases = [new("你好", "ni hao", "nh")] };
}