using XiaoXiIme.Dictionary;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class FullPackageReadingTests(ProductionDictionaryFixture fixture)
{
    [Theory]
    [InlineData("fullPinyin")]
    [InlineData("xiaoheDoublePinyin")]
    [InlineData("sogouDoublePinyin")]
    [InlineData("microsoftDoublePinyin")]
    [InlineData("abcDoublePinyin")]
    [InlineData("jiajiaDoublePinyin")]
    [InlineData("naturalCodeDoublePinyin")]
    public void CompleteProductionPackagePreservesCanonicalEntries(string scheme)
    {
        var dictionary = DictionaryPackageLoader.Load
        (
            DictionaryPackageLocations.ResolvePackageDirectory
            (
                inputScheme: scheme, baseDirectory: fixture.HostRoot
            )
        );
        var lexicon = dictionary.GetLexicon();
        var root = Path.Combine(AppContext.BaseDirectory, "DictionarySources", "phonetic");
        var expected = Directory.GetFiles(root, "*.phonetic.tsv").SelectMany
            (path =>
                {
                    using var reader = File.OpenText(path);
                    return PhoneticDictionarySourceParser.Parse(reader, path);
                }
            ).Select(e => (e.Text, e.Reading)).Distinct().OrderBy
                (e => e.Text, StringComparer.Ordinal)
            .ThenBy(e => e.Reading, StringComparer.Ordinal).ToArray();
        Assert.Equal
        (
            expected, lexicon.Entries.Select(e => (e.Text, Reading: e.CanonicalReading))
                .OrderBy(e => e.Text, StringComparer.Ordinal).ThenBy(e => e.Reading, StringComparer.Ordinal)
        );
    }

    [Fact]
    public void ProductionRawReadingsContainOnlyCanonicalSyllables()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "DictionarySources", "phonetic");
        var syllables = ImeProjectionDefinition.XiaoheDoublePinyin.Entries
            .Select(e => e.Initial + e.Final).ToHashSet(StringComparer.Ordinal);
        var nonphonetic = Directory.GetFiles(root, "*.phonetic.tsv").SelectMany(File.ReadLines)
            .Select(line => line.Split('\t')).Where(columns => columns.Length == 3)
            .Where(columns => columns[1].Split(' ').Any(s => !syllables.Contains(s)))
            .Select(columns => string.Join('|', columns)).ToArray();
        Assert.Empty(nonphonetic);
    }
}