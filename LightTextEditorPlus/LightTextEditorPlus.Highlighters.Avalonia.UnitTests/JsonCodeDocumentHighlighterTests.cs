using Avalonia.Media;
using LightTextEditorPlus;
using LightTextEditorPlus.Highlighters;
using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class JsonCodeDocumentHighlighterTests
{
    [Fact]
    public void ApplyHighlight_JsonKeysValuesAndBraces_HighlightsDetailedScopes()
    {
        const string code = "{\"name\": \"lindexi\", \"value\": 123, \"enabled\": true}";

        var textEditor = CreateHighlightedEditor(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"name\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"value\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"enabled\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"lindexi\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "123", ScopeType.Number);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "true", ScopeType.DeclarationTypeSyntax);
    }

    [Fact]
    public void ApplyHighlight_JsonContainsChineseString_HighlightsChineseStringValue()
    {
        const string code = "{   \"Key1\": \"中文\",   \"Key2\": 5 }";

        var textEditor = CreateHighlightedEditor(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"Key1\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"中文\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"Key2\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "5", ScopeType.Number);
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

        var textEditor = CreateHighlightedEditor(code);

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

        var textEditor = CreateHighlightedEditor(code);

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

        var textEditor = CreateHighlightedEditor(code);
        var expectedEditor = CreateColorCodeHighlightedEditor(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(expectedEditor, 0, textEditor, 0, code.Length);
    }

    [Fact]
    public void ApplyHighlight_MultipleCallsSameText_PreservesDetailedHighlighting()
    {
        const string code = "{\"name\": \"中文\", \"value\": 10}";
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, "json");
        textEditor.AppendText(code);

        highlighter.ApplyHighlight(code);
        highlighter.ApplyHighlight(code);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"name\"", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, "\"中文\"", ScopeType.String);
    }

    [Fact]
    public void RenderBackground_ValidContext_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, "json");
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
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, "json");
        var mockDrawingContext = new Mock<DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object)
        {
            Viewport = null
        };

        highlighter.RenderForeground(in context);
    }

    private static TextEditor CreateHighlightedEditor(string code)
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, "json");
        textEditor.AppendText(code);
        highlighter.ApplyHighlight(code);
        return textEditor;
    }

    private static TextEditor CreateColorCodeHighlightedEditor(string code)
    {
        var textEditor = new TextEditor();
        textEditor.AppendText(code);

        var plainTextHighlighter = new PlainTextDocumentHighlighter(textEditor);
        plainTextHighlighter.ApplyHighlight(code);

        var highlighter = new ColorCodeCodeHighlighter
        {
            LanguageId = "json"
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
