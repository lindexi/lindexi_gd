using Avalonia.Media;
using ColorCode.Common;
using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Highlighters;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class OtherCodeDocumentHighlighterTests
{
    public static TheoryData<string, string> StableLanguageSamples => new()
    {
        { LanguageId.Cpp, "#include <iostream>\nint main() { return 0; }" },
        { LanguageId.Css, ".container { color: red; }" },
        { LanguageId.Java, "public class Hello { }" },
        { "json", "{ \"name\": \"test\", \"value\": 123 }" },
        { LanguageId.Php, "<?php function hello($name) { echo \"Hello\"; } ?>" },
        { LanguageId.Sql, "SELECT TOP 1 * FROM Users" },
        { LanguageId.TypeScript, "interface Person { name: string; }" },
        { LanguageId.Xml, "<root><item id=\"1\">Value</item></root>" }
    };

    [Fact]
    public void Constructor_NullTextEditor_ThrowsArgumentNullException()
    {
        TextEditor textEditor = null!;

        Assert.Throws<ArgumentNullException>(() => new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_NullOrWhitespaceLanguageId_ThrowsArgumentException(string? languageId)
    {
        var textEditor = new TextEditor();

        var exception = Assert.Throws<ArgumentException>(() => new OtherCodeDocumentHighlighter(textEditor, languageId!));
        Assert.Contains("languageId", exception.Message);
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var textEditor = new TextEditor();

        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);

        Assert.NotNull(highlighter);
    }

    [Theory]
    [MemberData(nameof(StableLanguageSamples))]
    public void ApplyHighlight_StableLanguage_AppliesNonPlainTextColors(string languageId, string code)
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, languageId);
        textEditor.AppendText(code);

        highlighter.ApplyHighlight(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_JavaScriptWithComments_HighlightsCommentsAndKeywords()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        const string code = "// Single line comment\nfunction test() { return 42; }";
        textEditor.AppendText(code);

        highlighter.ApplyHighlight(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_PythonWithIndentation_HighlightsClassAndInterpolatedString()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.Python);
        const string code = "class Person:\n    def greet(self):\n        return f\"Hello\"";
        textEditor.AppendText(code);

        highlighter.ApplyHighlight(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_EmptyString_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        textEditor.AppendText(string.Empty);

        highlighter.ApplyHighlight(string.Empty);

        Assert.Empty(DocumentHighlighterTestHelper.GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_MultipleCallsSameText_PreservesHighlighting()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        const string code = "var x = 10;";
        textEditor.AppendText(code);

        highlighter.ApplyHighlight(code);
        highlighter.ApplyHighlight(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_DifferentText_UpdatesHighlighting()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        const string code1 = "var x = 10;";
        const string code2 = "function test() {}";
        textEditor.AppendText(code1);

        highlighter.ApplyHighlight(code1);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);

#pragma warning disable CS0618
        textEditor.TextEditorCore.Clear();
#pragma warning restore CS0618
        textEditor.AppendText(code2);
        highlighter.ApplyHighlight(code2);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code2);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void RenderBackground_ValidContext_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        var mockDrawingContext = new Mock<DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object)
        {
            Viewport = null
        };

        highlighter.RenderBackground(in context);
    }

    [Fact]
    public void RenderForeground_ValidContext_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        var mockDrawingContext = new Mock<DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object)
        {
            Viewport = null
        };

        highlighter.RenderForeground(in context);
    }
}
