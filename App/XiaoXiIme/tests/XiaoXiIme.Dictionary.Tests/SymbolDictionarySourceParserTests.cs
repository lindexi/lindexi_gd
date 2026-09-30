namespace XiaoXiIme.Dictionary.Tests;

public class SymbolDictionarySourceParserTests
{
    [Fact]
    public void Parse_WhenInputRepeatsThenMergesCandidatesAndPreservesFirstOccurrenceOrder()
    {
        using var reader = new StringReader("fh\t，\t。\nFH\t。\t！");

        var entry = Assert.Single(SymbolDictionarySourceParser.Parse(reader, "core.symbols.tsv"));

        Assert.Equal(["，", "。", "！"], entry.Candidates);
    }

    [Fact]
    public void Parse_WhenInputsAreUnorderedThenReturnsStableInputOrder()
    {
        using var reader = new StringReader("zz\t…\naa\t——");

        var entries = SymbolDictionarySourceParser.Parse(reader, "core.symbols.tsv");

        Assert.Equal(["aa", "zz"], entries.Select(entry => entry.Input));
    }

    [Fact]
    public void Parse_WhenInputHasMoreThan128CandidatesThenPreservesAllCandidates()
    {
        var candidates = Enumerable.Range(0, 129).Select(index => $"symbol-{index}").ToArray();
        using var reader = new StringReader($"fh\t{string.Join('\t', candidates)}");

        var entry = Assert.Single(SymbolDictionarySourceParser.Parse(reader, "core.symbols.tsv"));

        Assert.Equal(candidates, entry.Candidates);
    }

    [Theory]
    [InlineData("fh")]
    [InlineData("\t，")]
    [InlineData("f h\t，")]
    [InlineData("fh\t")]
    public void Parse_WhenSourceLineIsInvalidThenReportsFileAndLine(string source)
    {
        using var reader = new StringReader(source);

        var exception = Assert.Throws<DictionarySourceException>(
            () => SymbolDictionarySourceParser.Parse(reader, "invalid.symbols.tsv"));

        Assert.Equal(("invalid.symbols.tsv", 1), (exception.FilePath, exception.LineNumber));
    }
}
