using Avalonia.Media;
using ColorCode.Common;
using LightTextEditorPlus;
using LightTextEditorPlus.Highlighters;
using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class OtherCodeDocumentHighlighterTests
{
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
    public void ApplyHighlight_JavaScriptFunctionAndComment_HighlightsDetailedScopes()
    {
        const string code = "// sum values\nfunction add(value) { return value + 42; }";

        var textEditor = CreateHighlightedEditor(LanguageId.JavaScript, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "// sum values", ScopeType.Comment);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "function", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "return", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "42", ScopeType.Number);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "(", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, ")", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "}", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_PythonClassAndString_HighlightsDetailedScopes()
    {
        const string code = "class Person:\n    def greet(self):\n        return \"Hello\"";

        var textEditor = CreateHighlightedEditor(LanguageId.Python, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "class", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "Person", ScopeType.ClassName);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "def", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "return", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"Hello\"", ScopeType.String);
    }

    [Fact]
    public void ApplyHighlight_JsonKeysValuesAndBraces_HighlightsDetailedScopes()
    {
        const string code = "{\"name\": \"lindexi\", \"value\": 123, \"enabled\": true}";

        var textEditor = CreateHighlightedEditor("json", code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "name", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "value", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "enabled", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"lindexi\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "123", ScopeType.Number);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "true", ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "}", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_XmlAttributesAndContent_HighlightsDetailedScopes()
    {
        const string code = "<root><item id=\"1\">Value</item></root>";

        var textEditor = CreateHighlightedEditor(LanguageId.Xml, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "root", ScopeType.ClassMember, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "item", ScopeType.ClassMember, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "id", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"1\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "<", ScopeType.Brackets, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, ">", ScopeType.Brackets, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "Value", ScopeType.PlainText);
    }

    [Fact]
    public void ApplyHighlight_CssSelectorPropertyAndValue_HighlightsDetailedScopes()
    {
        const string code = ".container { color: red; }";

        var textEditor = CreateHighlightedEditor(LanguageId.Css, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, ".container", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "color", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "red", ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "}", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_SqlKeywordsFunctionAndNumber_HighlightsDetailedScopes()
    {
        const string code = "SELECT COUNT(*) FROM Users WHERE Id = 1";

        var textEditor = CreateHighlightedEditor(LanguageId.Sql, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "SELECT", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "FROM", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "WHERE", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "COUNT", ScopeType.Invocation);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "1", ScopeType.Number);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "(", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, ")", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_PhpKeywordVariableAndString_HighlightsDetailedScopes()
    {
        const string code = "<?php function hello($name) { echo \"Hello\"; } ?>";

        var textEditor = CreateHighlightedEditor(LanguageId.Php, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "function", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "echo", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertTokenUsesNonPlainTextColor(textEditor, code, "$name");
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"Hello\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "}", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_TypeScriptInterfaceTypeAndString_HighlightsDetailedScopes()
    {
        const string code = "interface Person { name: string; age: number; }";

        var textEditor = CreateHighlightedEditor(LanguageId.TypeScript, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "interface", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "Person", ScopeType.ClassName);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "name", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "age", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "string", ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "number", ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "}", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_CppPreprocessorCommentAndReturn_HighlightsDetailedScopes()
    {
        const string code = "#include <iostream>\n// entry\nint main() { return 0; }";

        var textEditor = CreateHighlightedEditor(LanguageId.Cpp, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "#include", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "iostream", ScopeType.ClassName, ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "// entry", ScopeType.Comment);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "return", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "0", ScopeType.Number);
    }

    [Fact]
    public void ApplyHighlight_JavaClassMethodAndString_HighlightsDetailedScopes()
    {
        const string code = "public class Hello { String greet() { return \"Hello\"; } }";

        var textEditor = CreateHighlightedEditor(LanguageId.Java, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "public", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "class", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "Hello", ScopeType.ClassName);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "return", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"Hello\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "{", ScopeType.Brackets, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "}", ScopeType.Brackets, 0);
    }

    [Fact]
    public void ApplyHighlight_MultipleCallsSameText_PreservesDetailedHighlighting()
    {
        const string code = "const value = 10;";
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        textEditor.AppendText(code);

        highlighter.ApplyHighlight(code);
        highlighter.ApplyHighlight(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "const", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "10", ScopeType.Number);
    }

    [Fact]
    public void ApplyHighlight_DifferentText_UpdatesToNewDetailedHighlighting()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.JavaScript);
        const string code1 = "var x = 10;";
        const string code2 = "function test() { return \"done\"; }";
        textEditor.AppendText(code1);

        highlighter.ApplyHighlight(code1);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code1, "var", ScopeType.Keyword);

#pragma warning disable CS0618
        textEditor.TextEditorCore.Clear();
#pragma warning restore CS0618
        textEditor.AppendText(code2);
        highlighter.ApplyHighlight(code2);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code2);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code2, "function", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code2, "return", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code2, "\"done\"", ScopeType.String);
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

    private static TextEditor CreateHighlightedEditor(string languageId, string code)
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, languageId);
        textEditor.AppendText(code);
        highlighter.ApplyHighlight(code);
        return textEditor;
    }
}
