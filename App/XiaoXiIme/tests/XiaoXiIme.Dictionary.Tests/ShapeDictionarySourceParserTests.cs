namespace XiaoXiIme.Dictionary.Tests;

public class ShapeDictionarySourceParserTests
{
    [Fact]
    public void Parse_WhenSourceIsValidThenNormalizesAndSortsEntries()
    {
        using var reader = new StringReader("好\tVBG\t女子\n你\twqiy\t亻尔");

        var entries = ShapeDictionarySourceParser.Parse(reader, "core.shape.tsv");

        Assert.Equal(["好:vbg", "你:wqiy"], entries.Select(entry => $"{entry.Text}:{entry.ShapeCode}"));
    }

    [Theory]
    [InlineData("〇")]
    [InlineData("\U00020000")]
    [InlineData("\U00030EDD")]
    [InlineData("\U000323AF")]
    public void Parse_WhenSupportedHanScalarIsUsedThenAcceptsSingleScalar(string text)
    {
        using var reader = new StringReader($"{text}\tabc\t一一");

        var entry = Assert.Single(ShapeDictionarySourceParser.Parse(reader, "core.shape.tsv"));

        Assert.Equal(text, entry.Text);
    }

    [Theory]
    [InlineData("你好\tabc\t亻尔")]
    [InlineData("A\tabc\tlatin")]
    [InlineData("你\ta-b\t亻尔")]
    [InlineData("你\tabc\t")]
    public void Parse_WhenSourceLineIsInvalidThenReportsFileAndLine(string source)
    {
        using var reader = new StringReader(source);

        var exception = Assert.Throws<DictionarySourceException>(
            () => ShapeDictionarySourceParser.Parse(reader, "invalid.shape.tsv"));

        Assert.Equal(("invalid.shape.tsv", 1), (exception.FilePath, exception.LineNumber));
    }
}
