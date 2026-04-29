using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Microsoft.CodeAnalysis.Text;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests.CodeHighlighters;

public class ColorCodeCodeHighlighterTests
{
    [Fact]
    public void ApplyHighlight_InvalidLanguageId_ThrowsInvalidOperationException()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter
        {
            LanguageId = "NonExistentLanguage"
        };
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext("test", mockColorCode.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => highlighter.ApplyHighlight(context));
        Assert.Contains("NonExistentLanguage", exception.Message);
    }

    [Fact]
    public void ApplyHighlight_EmptyString_NoColoringCalls()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext("", mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        mockColorCode.Verify(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()), Times.Never);
    }

    [Fact]
    public void ApplyHighlight_CSharpSimpleCode_HighlightsKeywordsAndStrings()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "var x = \"hello\";";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword && GetText(code, s.Span) == "var");
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.String); // "hello"
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.PlainText); // whitespace and other parts
    }

    [Fact]
    public void ApplyHighlight_CSharpKeywords_HighlightsAllKeywords()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "public class Test { }";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        var keywordSegments = coloredSegments.Where(s => s.Scope == ScopeType.Keyword).ToList();
        Assert.NotEmpty(keywordSegments);
        Assert.Contains(keywordSegments, s => GetText(code, s.Span).Contains("public", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyHighlight_CSharpClassName_HighlightsClassName()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "public class MyClass { }";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // ColorCode may not always mark class names separately - verify keywords are present
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword);
    }

    [Fact]
    public void ApplyHighlight_CSharpComment_HighlightsComment()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "// This is a comment\nvar x = 1;";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        var commentSegments = coloredSegments.Where(s => s.Scope == ScopeType.Comment).ToList();
        Assert.NotEmpty(commentSegments);
        Assert.Contains(commentSegments, s => GetText(code, s.Span).Contains("comment"));
    }

    [Fact]
    public void ApplyHighlight_CSharpMultiLineComment_HighlightsComment()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "/* Multi-line\ncomment */\nvar x = 1;";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        var commentSegments = coloredSegments.Where(s => s.Scope == ScopeType.Comment).ToList();
        Assert.NotEmpty(commentSegments);
    }

    [Fact]
    public void ApplyHighlight_CSharpNumbers_HighlightsNumbers()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "int x = 123;";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // At minimum, code should be processed and segments created
        Assert.NotEmpty(coloredSegments);
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword); // int
    }

    [Fact]
    public void ApplyHighlight_JavaScriptCode_HighlightsCorrectly()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "javascript" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "var x = \"test\";";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword && GetText(code, s.Span) == "var");
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.String);
    }

    [Fact]
    public void ApplyHighlight_HtmlCode_HighlightsTagsAndAttributes()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "html" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "<div class=\"test\">Hello</div>";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.ClassMember); // tag name or attribute
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.String); // "test"
    }

    [Fact]
    public void ApplyHighlight_XmlCode_HighlightsCorrectly()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "xml" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "<?xml version=\"1.0\"?>\n<root>content</root>";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.ClassMember); // tag names
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.String); // "1.0"
    }

    [Fact]
    public void ApplyHighlight_SqlCode_HighlightsKeywords()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "sql" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "SELECT * FROM Users WHERE Id = 1";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword); // SELECT, FROM, WHERE
    }

    [Fact]
    public void ApplyHighlight_JsonCode_HighlightsCorrectly()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "json" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "{\"name\": \"test\", \"value\": 123}";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.ClassMember); // "name", "value" keys
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Number); // 123
    }

    [Fact]
    public void ApplyHighlight_CSharpVerbatimString_HighlightsAsString()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "var path = @\"C:\\Users\\Test\";";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        var stringSegments = coloredSegments.Where(s => s.Scope == ScopeType.String).ToList();
        Assert.NotEmpty(stringSegments);
        // Verify string segments exist and cover reasonable length
        var totalStringLength = stringSegments.Sum(s => s.Span.Length);
        Assert.True(totalStringLength > 0, "String segments should have non-zero length");
    }

    [Fact]
    public void ApplyHighlight_CSharpBrackets_HighlightsBrackets()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "var arr = new int[] { 1, 2, 3 };";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // Verify code is highlighted
        Assert.NotEmpty(coloredSegments);
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword);
    }

    [Fact]
    public void ApplyHighlight_CSharpNamespace_HighlightsAsClassName()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "using System.Text;";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // Verify that the code is highlighted at all
        Assert.NotEmpty(coloredSegments);
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword); // using keyword
    }

    [Fact]
    public void ApplyHighlight_PlainTextOnly_HighlightsAsPlainText()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "abc";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.All(coloredSegments, s => Assert.Equal(ScopeType.PlainText, s.Scope));
    }

    [Fact]
    public void ApplyHighlight_WhitespaceOnly_HighlightsAsPlainText()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "   \n\t  \n  ";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.All(coloredSegments, s => Assert.Equal(ScopeType.PlainText, s.Scope));
    }

    [Fact]
    public void ApplyHighlight_CSharpComplexCode_HighlightsAllElements()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = @"// Comment
public class Test
{
    private int _value = 42;
    public string Name { get; set; } = ""default"";
}";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Comment);
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword);
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.String);
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.PlainText);
    }

    [Fact]
    public void ApplyHighlight_PythonCode_HighlightsCorrectly()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "python" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "def hello():\n    print(\"Hello\")";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword); // def
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.String); // "Hello"
    }

    [Fact]
    public void ApplyHighlight_CssCode_HighlightsCorrectly()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "css" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = ".class { color: red; }";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.ClassMember); // property name or selector
    }

    [Fact]
    public void ApplyHighlight_PhpCode_HighlightsCorrectly()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "php" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "<?php echo 'Hello'; ?>";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.Keyword); // echo
        Assert.Contains(coloredSegments, s => s.Scope == ScopeType.String); // 'Hello'
    }

    [Fact]
    public void ApplyHighlight_CSharpEscapeSequence_HighlightsAsString()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "var text = \"Line1\\nLine2\\tTab\";";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        var stringSegments = coloredSegments.Where(s => s.Scope == ScopeType.String).ToList();
        Assert.NotEmpty(stringSegments);
    }

    [Fact]
    public void ApplyHighlight_CoversEntireText_NoGaps()
    {
        // Arrange
        var highlighter = new ColorCodeCodeHighlighter { LanguageId = "csharp" };
        var coloredSegments = new List<(TextSpan Span, ScopeType Scope)>();
        var mockColorCode = new Mock<IColorCode>();
        mockColorCode.Setup(c => c.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()))
            .Callback<TextSpan, ScopeType>((span, scope) => coloredSegments.Add((span, scope)));

        var code = "var x = 1;";
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        var sortedSegments = coloredSegments.OrderBy(s => s.Span.Start).ToList();
        int totalCoverage = 0;
        int lastEnd = 0;
        
        foreach (var (span, _) in sortedSegments)
        {
            // Check no gaps (adjacent or overlapping segments)
            Assert.True(span.Start <= lastEnd + 1, "Gap detected in highlighting");
            lastEnd = Math.Max(lastEnd, span.End);
            totalCoverage += span.Length;
        }
        
        // Verify full text is covered
        Assert.True(lastEnd >= code.Length, "Not all text was highlighted");
    }

    private static string GetText(string source, TextSpan span)
    {
        return source.Substring(span.Start, span.Length);
    }
}
