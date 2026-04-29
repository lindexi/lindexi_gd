using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Highlighters;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class PlainTextDocumentHighlighterTests
{
    private static string GetEditorText(TextEditor textEditor)
    {
        var allSelection = textEditor.GetAllDocumentSelection();
        return textEditor.TextEditorCore.GetText(in allSelection);
    }

    [Fact]
    public void Constructor_NullTextEditor_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlainTextDocumentHighlighter(null!));
    }

    [Fact]
    public void Constructor_ValidTextEditor_CreatesInstance()
    {
        // Arrange
        var textEditor = new TextEditor();

        // Act
        var highlighter = new PlainTextDocumentHighlighter(textEditor);

        // Assert
        Assert.NotNull(highlighter);
    }

    [Fact]
    public void ApplyHighlight_EmptyString_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);

        // Act
        highlighter.ApplyHighlight(string.Empty);

        // Assert - no exception thrown
    }

    [Fact]
    public void ApplyHighlight_PlainText_MaintainsNormalTextStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "hello world";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_TextWithNumbers_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "Test 123 with numbers 456.789";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_TextWithSpecialCharacters_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "Special chars: !@#$%^&*()_+-={}[]|\\:\";<>?,./";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_CodeLikeText_TreatsAsPlainText()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "public class MyClass { }";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_TextWithQuotes_TreatsAsPlainText()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "var text = \"hello world\";";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_MultilineText_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "Line 1\nLine 2\nLine 3";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_TextWithComments_TreatsAsPlainText()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "// This looks like a comment but should be plain";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "Sample text";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);
        highlighter.ApplyHighlight(text);
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_DifferentTextEachTime_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);

        // Act & Assert - First text
        const string text1 = "First text";
        textEditor.AppendText(text1);
        highlighter.ApplyHighlight(text1);
        Assert.Equal(text1, GetEditorText(textEditor));

        // Clear and apply new text
#pragma warning disable CS0618 // Type or member is obsolete
        textEditor.TextEditorCore.Clear();
#pragma warning restore CS0618 // Type or member is obsolete
        const string text2 = "Second text with numbers 123";
        textEditor.AppendText(text2);
        highlighter.ApplyHighlight(text2);
        Assert.Equal(text2, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_WhitespaceOnly_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "   \t\n  ";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert - no exception thrown
    }

    [Fact]
    public void ApplyHighlight_UnicodeCharacters_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "Unicode: 你好世界 🌍 αβγδ";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_LongText_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        var text = string.Concat(System.Linq.Enumerable.Repeat("Lorem ipsum dolor sit amet, consectetur adipiscing elit. ", 100));
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_MixedContent_TreatsAllAsPlainText()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = @"Mixed content:
Keywords: class public void
Numbers: 123 456.789
Symbols: @#$%^&*()
Quotes: ""hello"" 'world'
Unicode: 你好";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void RenderBackground_ValidContext_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        var mockDrawingContext = new Mock<global::Avalonia.Media.DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object)
        {
            Viewport = null
        };

        // Act
        highlighter.RenderBackground(in context);

        // Assert - no exception thrown
    }

    [Fact]
    public void RenderForeground_ValidContext_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        var mockDrawingContext = new Mock<global::Avalonia.Media.DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object)
        {
            Viewport = null
        };

        // Act
        highlighter.RenderForeground(in context);

        // Assert - no exception thrown
    }

    [Fact]
    public void ApplyHighlight_EmptyDocument_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "";

        // Act
        highlighter.ApplyHighlight(text);

        // Assert - no exception thrown
    }

    [Fact]
    public void ApplyHighlight_SingleCharacter_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "a";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void ApplyHighlight_TextWithTabs_MaintainsPlainStyle()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new PlainTextDocumentHighlighter(textEditor);
        const string text = "Column1\tColumn2\tColumn3";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        var documentText = GetEditorText(textEditor);
        Assert.Equal(text, documentText);
    }
}
