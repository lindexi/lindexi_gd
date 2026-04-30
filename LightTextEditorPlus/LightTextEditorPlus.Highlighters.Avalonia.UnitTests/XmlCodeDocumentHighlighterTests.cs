using Avalonia.Media;
using ColorCode.Common;
using LightTextEditorPlus;
using LightTextEditorPlus.Highlighters;
using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class XmlCodeDocumentHighlighterTests
{
    [Fact]
    public void ApplyHighlight_SingleLineElementWithNumberContent_HighlightsXmlScopes()
    {
        const string code = "<bb>123</bb>";

        var textEditor = CreateHighlightedEditor(code);

        AssertXmlHighlight(textEditor, code,
            classMemberTokenList: [("bb", 0), ("bb", 1)],
            stringTokenList: [],
            plainTextTokenList: [("123", 0)]);
    }

    [Fact]
    public void ApplyHighlight_XmlAttributesAndChineseText_HighlightsDetailedScopes()
    {
        const string code = "<root><item id=\"1\" title=\"中文\">Value</item></root>";

        var textEditor = CreateHighlightedEditor(code);
        AssertXmlHighlight(textEditor, code,
            classMemberTokenList: [("root", 0), ("item", 0), ("id", 0), ("title", 0), ("item", 1), ("root", 1)],
            stringTokenList: ["\"1\"", "\"中文\""],
            plainTextTokenList: [("Value", 0)]);
    }

    [Fact]
    public void ApplyHighlight_MultiLineNestedXml_HighlightsTagsAttributesStringsAndTextContent()
    {
        var code = """
        <?xml version="1.0" encoding="utf-8"?>
        <workspace title="Demo Board">
          <group id="card-001" enabled="true">
            <item>98.5</item>
            <item>中文</item>
          </group>
        </workspace>
        """.Replace("\r\n", "\n");

        var textEditor = CreateHighlightedEditor(code);
        AssertXmlHighlight(textEditor, code,
            classMemberTokenList:
            [
                ("workspace", 0), ("group", 0), ("item", 0), ("item", 1), ("item", 2), ("item", 3), ("group", 1), ("workspace", 1),
                ("version", 0), ("encoding", 0), ("title", 0), ("id", 0), ("enabled", 0)
            ],
            stringTokenList: ["\"1.0\"", "\"utf-8\"", "\"Demo Board\"", "\"card-001\"", "\"true\""],
            plainTextTokenList: [("98.5", 0), ("中文", 0)]);
    }

    [Fact]
    public void ApplyHighlight_XmlWithCommentAndSelfClosingElement_HighlightsCommentAndXmlScopes()
    {
        var code = """
        <root>
          <!-- dashboard definition -->
          <item id="1" />
          <item id="2">ready</item>
        </root>
        """.Replace("\r\n", "\n");

        var textEditor = CreateHighlightedEditor(code);
        AssertXmlHighlight(textEditor, code,
            classMemberTokenList: [("root", 0), ("item", 0), ("id", 0), ("item", 1), ("id", 1), ("item", 2), ("root", 1)],
            stringTokenList: ["\"1\"", "\"2\""],
            plainTextTokenList: [("ready", 0)],
            commentTokenList: [("<!-- dashboard definition -->", 0)]);
    }

    [Fact]
    public void ApplyHighlight_MultipleCallsSameText_PreservesDetailedHighlighting()
    {
        const string code = "<root><item>中文</item></root>";
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.Xml);
        textEditor.AppendText(code);

        highlighter.ApplyHighlight(code);
        highlighter.ApplyHighlight(code);

        AssertXmlHighlight(textEditor, code,
            classMemberTokenList: [("root", 0), ("item", 0), ("item", 1), ("root", 1)],
            stringTokenList: [],
            plainTextTokenList: [("中文", 0)]);
    }

    [Fact]
    public void ApplyHighlight_DifferentText_UpdatesToNewDetailedHighlighting()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.Xml);
        const string code1 = "<bb>123</bb>";
        const string code2 = "<root><item id=\"2\">next</item></root>";
        textEditor.AppendText(code1);

        highlighter.ApplyHighlight(code1);

#pragma warning disable CS0618
        textEditor.TextEditorCore.Clear();
#pragma warning restore CS0618
        textEditor.AppendText(code2);
        highlighter.ApplyHighlight(code2);

        AssertXmlHighlight(textEditor, code2,
            classMemberTokenList: [("root", 0), ("item", 0), ("id", 0), ("item", 1), ("root", 1)],
            stringTokenList: ["\"2\""],
            plainTextTokenList: [("next", 0)]);
    }

    [Fact]
    public void RenderBackground_ValidContext_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.Xml);
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
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.Xml);
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
        var highlighter = new OtherCodeDocumentHighlighter(textEditor, LanguageId.Xml);
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
            LanguageId = LanguageId.Xml
        };
        var colorCode = new TextEditorColorCode(textEditor, new LightTextEditorPlus.Core.Document.Segments.DocumentOffset(0));
        var context = new HighlightCodeContext(code, colorCode);
        highlighter.ApplyHighlight(context);
        return textEditor;
    }

    private static void AssertXmlHighlight(TextEditor textEditor, string code, IEnumerable<(string Token, int Occurrence)> classMemberTokenList,
        IEnumerable<string> stringTokenList, IEnumerable<(string Token, int Occurrence)> plainTextTokenList,
        IEnumerable<(string Token, int Occurrence)>? commentTokenList = null)
    {
        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, code);

        foreach (var (token, occurrence) in classMemberTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, token, occurrence, ScopeType.ClassMember);
        }

        foreach (var token in stringTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, token, ScopeType.String);
        }

        foreach (var (token, occurrence) in plainTextTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, token, occurrence, ScopeType.PlainText);
        }

        if (commentTokenList is null)
        {
            return;
        }

        foreach (var (token, occurrence) in commentTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, code, token, occurrence, ScopeType.Comment);
        }
    }
}
