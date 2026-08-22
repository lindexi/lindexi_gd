using System.Text;

namespace XiaoXiIme.Dictionary.Tests;

public class PhoneticDictionarySourceParserTests
{
    [Fact]
    public void Parse_WhenSourceContainsCommentsAndBlankLinesThenReturnsEntries()
    {
        using var reader = CreateReader("# comment\n\n你\tni\t100\n好\thao\t90");

        var entries = PhoneticDictionarySourceParser.Parse(reader, "core.phonetic.tsv");

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void Parse_WhenTextHasMultipleReadingsThenKeepsEachReading()
    {
        using var reader = CreateReader("行\txing\t100\n行\thang\t80");

        var entries = PhoneticDictionarySourceParser.Parse(reader, "core.phonetic.tsv");

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void Parse_WhenReadingContainsCaseAndExtraWhitespaceThenNormalizesReading()
    {
        using var reader = CreateReader("你好\t  NI   HAO  \t100");

        var entries = PhoneticDictionarySourceParser.Parse(reader, "core.phonetic.tsv");

        Assert.Equal("ni hao", Assert.Single(entries).Reading);
    }

    [Fact]
    public void Parse_WhenEntryIsDuplicatedThenUsesHighestFrequency()
    {
        using var reader = CreateReader("你\tni\t20\n你\tNI\t100\n你\tni\t80");

        var entries = PhoneticDictionarySourceParser.Parse(reader, "core.phonetic.tsv");

        Assert.Equal(100, Assert.Single(entries).Frequency);
    }

    [Fact]
    public void Parse_WhenInputOrderChangesThenReturnsStableOrder()
    {
        using var firstReader = CreateReader("呢\tni\t80\n你\tni\t100\n好\thao\t90");
        using var secondReader = CreateReader("好\thao\t90\n你\tni\t100\n呢\tni\t80");

        var first = PhoneticDictionarySourceParser.Parse(firstReader, "first.phonetic.tsv");
        var second = PhoneticDictionarySourceParser.Parse(secondReader, "second.phonetic.tsv");

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("你\tni", 1)]
    [InlineData("\tni\t100", 1)]
    [InlineData("你\t\t100", 1)]
    [InlineData("你\tni\t-1", 1)]
    [InlineData("你\tni\tnot-a-number", 1)]
    [InlineData("# comment\n你\tnǐ\t100", 2)]
    public void Parse_WhenSourceLineIsInvalidThenReportsFileAndLine(string source, int expectedLineNumber)
    {
        using var reader = CreateReader(source);

        var exception = Assert.Throws<PhoneticDictionarySourceException>(
            () => PhoneticDictionarySourceParser.Parse(reader, "invalid.phonetic.tsv"));

        Assert.Equal(("invalid.phonetic.tsv", expectedLineNumber), (exception.FilePath, exception.LineNumber));
    }

    [Fact]
    public void Parse_WhenTextIsTooLongThenRejectsEntry()
    {
        var text = new string('字', PhoneticDictionarySourceParser.MaxTextLength + 1);
        using var reader = CreateReader($"{text}\tzi\t100");

        Assert.Throws<PhoneticDictionarySourceException>(
            () => PhoneticDictionarySourceParser.Parse(reader, "large.phonetic.tsv"));
    }

    [Fact]
    public void Parse_WhenReadingIsTooLongThenRejectsEntry()
    {
        var reading = new string('a', PhoneticDictionarySourceParser.MaxReadingLength + 1);
        using var reader = CreateReader($"字\t{reading}\t100");

        Assert.Throws<PhoneticDictionarySourceException>(
            () => PhoneticDictionarySourceParser.Parse(reader, "large.phonetic.tsv"));
    }

    [Fact]
    public void Parse_WhenLineIsTooLongThenRejectsEntry()
    {
        var line = new string('a', PhoneticDictionarySourceParser.MaxLineLength + 1);
        using var reader = CreateReader(line);

        Assert.Throws<PhoneticDictionarySourceException>(
            () => PhoneticDictionarySourceParser.Parse(reader, "large.phonetic.tsv"));
    }

    private static StringReader CreateReader(string source)
    {
        return new StringReader(source.Replace("\n", Environment.NewLine, StringComparison.Ordinal));
    }
}
