using YamlDotNet.RepresentationModel;

namespace XiaoXiIme.Dictionary.Tests;

public class DictionarySourceMetadataTests
{
    [Theory]
    [InlineData("name", "core")]
    [InlineData("version", "2026-01")]
    [InlineData("description", "First line\nsecond line\n")]
    [InlineData("sort", "by_weight")]
    public void ParseDocument_WhenHeaderExistsThenPreservesMetadata(string field, string expected)
    {
        using var reader = new StringReader("# source\n---\nname: core\nversion: '2026-01'\ndescription: |\n  First line\n  second line\nsort: by_weight\n...\n你\tni\t100\n");
        var document = PhoneticDictionarySourceParser.ParseDocument(reader, "core.phonetic.tsv");
        Assert.Equal(expected, ((YamlScalarNode)document.Metadata.Fields.Children[new YamlScalarNode(field)]).Value);
    }

    [Fact]
    public void ParseDocument_WhenShapeHasHeaderThenReturnsEntries()
    {
        using var reader = new StringReader("---\nname: shape\n...\n吖\tky\t口丫");
        Assert.Single(ShapeDictionarySourceParser.ParseDocument(reader, "core.shape.tsv").Entries);
    }

    [Fact]
    public void ParseDocument_WhenSymbolHasHeaderThenReturnsMetadata()
    {
        using var reader = new StringReader("---\nname: symbols\n...\n/test\tA\tB");
        Assert.Equal("symbols", SymbolDictionarySourceParser.ParseDocument(reader, "core.symbols.tsv").Metadata.Name);
    }

    [Fact]
    public void ParseDocument_WhenHeaderIsAbsentThenMetadataIsEmpty()
    {
        using var reader = new StringReader("你\tni\t100");
        Assert.Empty(PhoneticDictionarySourceParser.ParseDocument(reader, "core.phonetic.tsv").Metadata.Fields.Children);
    }

    [Fact]
    public void ParseDocument_WhenExtraMetadataIsNestedThenPreservesMapping()
    {
        using var reader = new StringReader("---\nsource:\n  author: SeWZC\n...\n你\tni\t100");
        var metadata = PhoneticDictionarySourceParser.ParseDocument(reader, "core.phonetic.tsv").Metadata;
        var source = (YamlMappingNode)metadata.Fields.Children[new YamlScalarNode("source")];
        Assert.Equal("SeWZC", ((YamlScalarNode)source.Children[new YamlScalarNode("author")]).Value);
    }

    [Theory]
    [InlineData("---\nname: core\n")]
    [InlineData("---\nname: [invalid\n...\n")]
    [InlineData("---\nsort: original\n...\n")]
    public void ParseDocument_WhenHeaderIsInvalidThenReportsSourceError(string source)
    {
        using var reader = new StringReader(source);
        Assert.Throws<DictionarySourceException>(() => PhoneticDictionarySourceParser.ParseDocument(reader, "core.phonetic.tsv"));
    }

    [Fact]
    public void Parse_WhenHeaderPrecedesInvalidEntryThenPreservesPhysicalLineNumber()
    {
        using var reader = new StringReader("---\nname: core\n...\nbad");
        var exception = Assert.Throws<PhoneticDictionarySourceException>(() => PhoneticDictionarySourceParser.Parse(reader, "core.phonetic.tsv"));
        Assert.Contains("(4)", exception.Message);
    }

    [Fact]
    public void Parse_WhenHeaderIsSerializedThenRoundTrips()
    {
        using var writer = new StringWriter();
        writer.WriteLine("---");
        new YamlStream(new YamlDocument(new YamlMappingNode { { "name", "core" }, { "sort", "by_weight" } })).Save(writer, false);
        writer.WriteLine("你\tni\t100");
        using var reader = new StringReader(writer.ToString());
        Assert.Single(PhoneticDictionarySourceParser.Parse(reader, "core.phonetic.tsv"));
    }
}
