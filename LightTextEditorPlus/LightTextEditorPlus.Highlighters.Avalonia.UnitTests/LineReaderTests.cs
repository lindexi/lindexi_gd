using LightTextEditorPlus.Highlighters;
using Markdig.Syntax;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class LineReaderTests
{
    [Fact]
    public void ReadLine_EmptyString_ReturnsInvalidSpan()
    {
        // Arrange
        var lineReader = new LineReader(string.Empty);

        // Act
        var result = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, result.Start);
        Assert.Equal(-1, result.End);
    }

    [Fact]
    public void ReadLine_SingleLineWithoutLineEnding_ReturnsFullLine()
    {
        // Arrange
        var text = "Hello World";
        var lineReader = new LineReader(text);

        // Act
        var result = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, result.Start);
        Assert.Equal(10, result.End);
        Assert.Equal(text, text.Substring(result.Start, result.End - result.Start + 1));
    }

    [Fact]
    public void ReadLine_SingleLineWithLfEnding_ReturnsLineWithoutEnding()
    {
        // Arrange
        var text = "Hello World\n";
        var lineReader = new LineReader(text);

        // Act
        var result = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, result.Start);
        Assert.Equal(10, result.End);
        Assert.Equal("Hello World", text.Substring(result.Start, result.End - result.Start + 1));
    }

    [Fact]
    public void ReadLine_SingleLineWithCrEnding_ReturnsLineWithoutEnding()
    {
        // Arrange
        var text = "Hello World\r";
        var lineReader = new LineReader(text);

        // Act
        var result = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, result.Start);
        Assert.Equal(10, result.End);
        Assert.Equal("Hello World", text.Substring(result.Start, result.End - result.Start + 1));
    }

    [Fact]
    public void ReadLine_SingleLineWithCrLfEnding_ReturnsLineWithoutEnding()
    {
        // Arrange
        var text = "Hello World\r\n";
        var lineReader = new LineReader(text);

        // Act
        var result = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, result.Start);
        Assert.Equal(10, result.End);
        Assert.Equal("Hello World", text.Substring(result.Start, result.End - result.Start + 1));
    }

    [Fact]
    public void ReadLine_MultipleLinesWithLf_ReadsAllLines()
    {
        // Arrange
        var text = "Line 1\nLine 2\nLine 3";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();
        var line4 = lineReader.ReadLine();

        // Assert
        Assert.Equal("Line 1", text.Substring(line1.Start, line1.End - line1.Start + 1));
        Assert.Equal("Line 2", text.Substring(line2.Start, line2.End - line2.Start + 1));
        Assert.Equal("Line 3", text.Substring(line3.Start, line3.End - line3.Start + 1));
        Assert.Equal(-1, line4.End); // No more lines
    }

    [Fact]
    public void ReadLine_MultipleLinesWithCr_ReadsAllLines()
    {
        // Arrange
        var text = "Line 1\rLine 2\rLine 3";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();
        var line4 = lineReader.ReadLine();

        // Assert
        Assert.Equal("Line 1", text.Substring(line1.Start, line1.End - line1.Start + 1));
        Assert.Equal("Line 2", text.Substring(line2.Start, line2.End - line2.Start + 1));
        Assert.Equal("Line 3", text.Substring(line3.Start, line3.End - line3.Start + 1));
        Assert.Equal(-1, line4.End); // No more lines
    }

    [Fact]
    public void ReadLine_MultipleLinesWithCrLf_ReadsAllLines()
    {
        // Arrange
        var text = "Line 1\r\nLine 2\r\nLine 3";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();
        var line4 = lineReader.ReadLine();

        // Assert
        Assert.Equal("Line 1", text.Substring(line1.Start, line1.End - line1.Start + 1));
        Assert.Equal("Line 2", text.Substring(line2.Start, line2.End - line2.Start + 1));
        Assert.Equal("Line 3", text.Substring(line3.Start, line3.End - line3.Start + 1));
        Assert.Equal(-1, line4.End); // No more lines
    }

    [Fact]
    public void ReadLine_MixedLineEndings_ReadsAllLines()
    {
        // Arrange
        var text = "Line 1\r\nLine 2\nLine 3\rLine 4";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();
        var line4 = lineReader.ReadLine();
        var line5 = lineReader.ReadLine();

        // Assert
        Assert.Equal("Line 1", text.Substring(line1.Start, line1.End - line1.Start + 1));
        Assert.Equal("Line 2", text.Substring(line2.Start, line2.End - line2.Start + 1));
        Assert.Equal("Line 3", text.Substring(line3.Start, line3.End - line3.Start + 1));
        Assert.Equal("Line 4", text.Substring(line4.Start, line4.End - line4.Start + 1));
        Assert.Equal(-1, line5.End); // No more lines
    }

    [Fact]
    public void ReadLine_EmptyLinesWithLf_ReturnsEmptySpans()
    {
        // Arrange
        var text = "\n\n\n";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();
        var line4 = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, line1.Start);
        Assert.Equal(-1, line1.End);
        Assert.Equal(1, line2.Start);
        Assert.Equal(0, line2.End);
        Assert.Equal(2, line3.Start);
        Assert.Equal(1, line3.End);
        Assert.Equal(-1, line4.End); // No more lines
    }

    [Fact]
    public void ReadLine_EmptyLinesWithCrLf_ReturnsEmptySpans()
    {
        // Arrange
        var text = "\r\n\r\n";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, line1.Start);
        Assert.Equal(-1, line1.End);
        Assert.Equal(2, line2.Start);
        Assert.Equal(1, line2.End);
        Assert.Equal(-1, line3.End); // No more lines
    }

    [Fact]
    public void ReadLine_TextEndingWithMultipleLineEndings_HandlesCorrectly()
    {
        // Arrange
        var text = "Line 1\n\n";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();

        // Assert
        Assert.Equal("Line 1", text.Substring(line1.Start, line1.End - line1.Start + 1));
        Assert.Equal(7, line2.Start);
        Assert.Equal(6, line2.End);
        Assert.Equal(-1, line3.End); // No more lines
    }

    [Fact]
    public void ReadLine_OnlyCarriageReturn_ReturnsEmptyLine()
    {
        // Arrange
        var text = "\r";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, line1.Start);
        Assert.Equal(-1, line1.End);
        Assert.Equal(-1, line2.End); // No more lines
    }

    [Fact]
    public void ReadLine_OnlyLineFeed_ReturnsEmptyLine()
    {
        // Arrange
        var text = "\n";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, line1.Start);
        Assert.Equal(-1, line1.End);
        Assert.Equal(-1, line2.End); // No more lines
    }

    [Fact]
    public void ReadLine_OnlyCrLf_ReturnsEmptyLine()
    {
        // Arrange
        var text = "\r\n";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, line1.Start);
        Assert.Equal(-1, line1.End);
        Assert.Equal(-1, line2.End); // No more lines
    }

    [Fact]
    public void ReadLine_LongLine_HandlesCorrectly()
    {
        // Arrange
        var text = new string('a', 10000) + "\n" + new string('b', 10000);
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, line1.Start);
        Assert.Equal(9999, line1.End);
        Assert.Equal(10001, line2.Start);
        Assert.Equal(20000, line2.End);
        Assert.Equal(-1, line3.End);
    }

    [Fact]
    public void ReadLine_ConsecutiveCalls_ReturnsInvalidSpanAfterEnd()
    {
        // Arrange
        var text = "Single line";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();
        var line3 = lineReader.ReadLine();

        // Assert
        Assert.Equal("Single line", text.Substring(line1.Start, line1.End - line1.Start + 1));
        Assert.Equal(-1, line2.End);
        Assert.Equal(-1, line3.End);
    }

    [Fact]
    public void ReadLine_TextWithTrailingContent_ReadsCorrectly()
    {
        // Arrange
        var text = "First\nSecond";
        var lineReader = new LineReader(text);

        // Act
        var line1 = lineReader.ReadLine();
        var line2 = lineReader.ReadLine();

        // Assert
        Assert.Equal(0, line1.Start);
        Assert.Equal(4, line1.End);
        Assert.Equal(6, line2.Start);
        Assert.Equal(11, line2.End);
    }
}
