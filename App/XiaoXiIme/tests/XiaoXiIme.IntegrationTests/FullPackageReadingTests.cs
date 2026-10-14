using XiaoXiIme.Dictionary;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class FullPackageReadingTests(ProductionDictionaryFixture fixture)
{
    [Theory]
    [InlineData("fullPinyin")]
    [InlineData("xiaoheDoublePinyin")]
    public void CompleteProductionPackagePreservesCanonicalEntriesAndRegisteredAlias(string scheme)
    {
        var dictionary = DictionaryPackageLoader.Load(DictionaryPackageLocations.ResolvePackageDirectory(
            inputScheme: scheme, baseDirectory: fixture.HostRoot));
        var lexicon = dictionary.GetLexicon();
        var root = Path.Combine(AppContext.BaseDirectory, "DictionarySources", "phonetic");
        var expected = Directory.GetFiles(root, "*.phonetic.tsv").SelectMany(path =>
        {
            using var reader = File.OpenText(path);
            return PhoneticDictionarySourceParser.Parse(reader, path);
        }).Where(e => e.Reading != "xx").Select(e => (e.Text, e.Reading)).Distinct().OrderBy(e => e.Text, StringComparer.Ordinal)
            .ThenBy(e => e.Reading, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, lexicon.Entries.Select(e => (e.Text, Reading: e.CanonicalReading))
            .OrderBy(e => e.Text, StringComparer.Ordinal).ThenBy(e => e.Reading, StringComparer.Ordinal));
    }

    [Fact]
    public void ProductionRawReadingsContainOnlyTheRegisteredProjectAbbreviation()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "DictionarySources", "phonetic");
        var syllables = ImeProjectionDefinition.XiaoheDoublePinyin.Entries
            .Select(e => e.Initial + e.Final).ToHashSet(StringComparer.Ordinal);
        var nonphonetic = Directory.GetFiles(root, "*.phonetic.tsv").SelectMany(File.ReadLines)
            .Select(line => line.Split('\t')).Where(columns => columns.Length == 3)
            .Where(columns => columns[1].Split(' ').Any(s => !syllables.Contains(s)))
            .Select(columns => string.Join('|', columns)).ToArray();
        Assert.Equal(new[] { "小希|xx|10000000" }, nonphonetic);
    }

    [Fact]
    public void LegacyProjectAbbreviationHasExplicitCanonicalAssociation()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Alias", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(XiaoXiImeProjectTerms.Parse(), path);
        var dictionary = DictionaryPackageLoader.Load(path);
        Assert.Contains(new ImeLookupAlias("小希", "xiao xi", "xx"), dictionary.GetLexicon().LookupAliases);
    }
}
