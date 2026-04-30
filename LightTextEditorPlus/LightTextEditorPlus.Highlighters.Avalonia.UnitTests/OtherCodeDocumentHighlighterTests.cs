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
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.JavaScript, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_MultiLineLongJson_HighlightsKeysStringsNumbersAndConstants()
    {
        var code = """
        {
          "workspace": {
            "title": "Demo Board",
            "version": 3,
            "items": [
              {
                "id": "card-001",
                "active": true,
                "score": 98.5,
                "tags": [
                  "ui",
                  "json"
                ]
              },
              {
                "id": "card-002",
                "active": false,
                "score": null,
                "tags": []
              }
            ]
          }
        }
        """.Replace("\r\n", "\n");

        var textEditor = CreateHighlightedEditor("json", code);

        AssertJsonHighlight(textEditor, code,
            keyTokenList: ["\"workspace\"", "\"title\"", "\"version\"", "\"items\"", "\"id\"", "\"active\"", "\"score\"", "\"tags\""],
            stringValueTokenList: ["\"Demo Board\"", "\"card-001\"", "\"ui\"", "\"json\"", "\"card-002\""],
            numberTokenList: ["3", "98.5"],
            constantTokenList: ["true", "false", "null"]);
    }

    [Fact]
    public void ApplyHighlight_LooseMultiLineLongJson_HighlightsWhenJsonParsingSucceeds()
    {
        var code = """
        {
          // dashboard definition
          "workspace": {
            "title": "Loose Config",
            "version": 5,
            "items": [
              {
                "id": "card-100",
                "active": true,
              },
              {
                "id": "card-200",
                "active": false,
              },
            ],
          },
        }
        """.Replace("\r\n", "\n");

        var textEditor = CreateHighlightedEditor("json", code);

        AssertJsonHighlight(textEditor, code,
            keyTokenList: ["\"workspace\"", "\"title\"", "\"version\"", "\"items\"", "\"id\"", "\"active\""],
            stringValueTokenList: ["\"Loose Config\"", "\"card-100\"", "\"card-200\""],
            numberTokenList: ["5"],
            constantTokenList: ["true", "false"]);

        DocumentHighlighterTestHelper.AssertPlainTextColor(textEditor, "// dashboard definition");
    }

    [Fact]
    public void ApplyHighlight_InvalidJson_FallsBackToColorCodeHighlighting()
    {
        var code = """
        {
          "title": "Broken Json",
          "value": 10,
          "items": [1, 2,, 3]
        }
        """.Replace("\r\n", "\n");

        var textEditor = CreateHighlightedEditor("json", code);
        var expectedEditor = CreateColorCodeHighlightedEditor("json", code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_PythonClassAndString_HighlightsDetailedScopes()
    {
        const string code = "class Person:\n    def greet(self):\n        return \"Hello\"";

        var textEditor = CreateHighlightedEditor(LanguageId.Python, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.Python, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_JsonKeysValuesAndBraces_HighlightsDetailedScopes()
    {
        const string code = "{\"name\": \"lindexi\", \"value\": 123, \"enabled\": true}";

        var textEditor = CreateHighlightedEditor("json", code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"name\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"value\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"enabled\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"lindexi\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "123", ScopeType.Number);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "true", ScopeType.DeclarationTypeSyntax);
    }

    [Fact]
    public void ApplyHighlight_XmlAttributesAndContent_HighlightsDetailedScopes()
    {
        const string code = "<root><item id=\"1\">Value</item></root>";

        var textEditor = CreateHighlightedEditor(LanguageId.Xml, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.Xml, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_CssSelectorPropertyAndValue_HighlightsDetailedScopes()
    {
        const string code = ".container { color: red; }";

        var textEditor = CreateHighlightedEditor(LanguageId.Css, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.Css, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_SqlKeywordsFunctionAndNumber_HighlightsDetailedScopes()
    {
        const string code = "SELECT COUNT(*) FROM Users WHERE Id = 1";

        var textEditor = CreateHighlightedEditor(LanguageId.Sql, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.Sql, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_PhpKeywordVariableAndString_HighlightsDetailedScopes()
    {
        const string code = "<?php function hello($name) { echo \"Hello\"; } ?>";

        var textEditor = CreateHighlightedEditor(LanguageId.Php, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.Php, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_TypeScriptInterfaceTypeAndString_HighlightsDetailedScopes()
    {
        const string code = "interface Person { name: string; age: number; }";

        var textEditor = CreateHighlightedEditor(LanguageId.TypeScript, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.TypeScript, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_CppPreprocessorCommentAndReturn_HighlightsDetailedScopes()
    {
        const string code = "#include <iostream>\n// entry\nint main() { return 0; }";

        var textEditor = CreateHighlightedEditor(LanguageId.Cpp, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.Cpp, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_JavaClassMethodAndString_HighlightsDetailedScopes()
    {
        const string code = "public class Hello { String greet() { return \"Hello\"; } }";

        var textEditor = CreateHighlightedEditor(LanguageId.Java, code);
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.Java, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
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
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.JavaScript, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
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
        var expectedEditor = CreateColorCodeHighlightedEditor(LanguageId.JavaScript, code2);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code2.Length);
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

    private static TextEditor CreateColorCodeHighlightedEditor(string languageId, string code)
    {
        var textEditor = new TextEditor();
        textEditor.AppendText(code);

        var plainTextHighlighter = new PlainTextDocumentHighlighter(textEditor);
        plainTextHighlighter.ApplyHighlight(code);

        var highlighter = new ColorCodeCodeHighlighter
        {
            LanguageId = languageId
        };
        var colorCode = new TextEditorColorCode(textEditor, new LightTextEditorPlus.Core.Document.Segments.DocumentOffset(0));
        var context = new HighlightCodeContext(code, colorCode);
        highlighter.ApplyHighlight(context);
        return textEditor;
    }

    private static void AssertJsonHighlight(TextEditor textEditor, string code, IEnumerable<string> keyTokenList,
        IEnumerable<string> stringValueTokenList, IEnumerable<string> numberTokenList, IEnumerable<string> constantTokenList)
    {
        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);

        foreach (var keyToken in keyTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, keyToken, ScopeType.ClassMember);
        }

        foreach (var stringValueToken in stringValueTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, stringValueToken, ScopeType.String);
        }

        foreach (var numberToken in numberTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, numberToken, ScopeType.Number);
        }

        foreach (var constantToken in constantTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, constantToken, ScopeType.DeclarationTypeSyntax);
        }
    }
}
