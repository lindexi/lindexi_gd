using ColorCode.Common;
using LightTextEditorPlus.Highlighters.CodeHighlighters;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class MarkdownCodeBlockHighlightingTests
{
    public static TheoryData<string, string> OtherLanguageCodeBlockData => new()
    {
        { "xml", "<bb>123</bb>\n" },
        { "xaml", "<Grid><TextBlock Text=\"Hello\" /></Grid>" },
        { "axaml", "<TextBlock Text=\"Hello\" />" },
        { "svg", "<svg><text>Hello</text></svg>" },
        { "javascript", "const value = \"done\";" },
        { "js", "const value = \"done\";" },
        { "node", "const value = \"done\";" },
        { "python", "class Person:\n    pass" },
        { "py", "class Person:\n    pass" },
        { "java", "public class Hello { }" },
        { "cpp", "int main() { return 0; }" },
        { "c", "int main() { return 0; }" },
        { "c++", "int main() { return 0; }" },
        { "hpp", "int add(int left, int right);" },
        { "sql", "SELECT * FROM Users" },
        { "xml", "<root><item>value</item></root>" },
        { "json", "{\"name\": \"value\", \"count\": 1}" },
        { "html", "<div class=\"note\">Hello</div>" },
        { "htm", "<span>Hello</span>" },
        { "css", ".container { color: red; }" },
        { "typescript", "interface Person { name: string; }" },
        { "ts", "interface Person { name: string; }" },
        { "tsx", "const view = <div>Hello</div>;" },
        { "fsharp", "let value = 10" },
        { "fs", "let value = 10" },
        { "vb", "Dim value As Integer = 10" },
        { "vbnet", "Dim value As Integer = 10" },
        { "php", "<?php function hello($name) { echo \"Hello\"; } ?>" },
        { "powershell", "Get-Process | Select-Object -First 1" },
        { "pwsh", "Get-Process | Select-Object -First 1" },
        { "fortran", "program hello\nprint *, \"Hello\"\nend program hello" },
        { "haskell", "main = putStrLn \"Hello\"" },
        { "markdown", "# Heading\n\n- Item" },
    };

    [Theory]
    [InlineData("csharp")]
    [InlineData("cs")]
    [InlineData("C#")]
    [InlineData("dotnet")]
    [InlineData(" CSharp ")]
    public void ApplyHighlight_CodeBlockWithCSharpAlias_HighlightsInnerCode(string language)
    {
        const string code = "var value = 42;";
        var markdown = CreateCodeBlock(language, code);

        var markdownEditor = CreateHighlightedEditor(markdown);
        var standaloneEditor = CreateStandaloneCodeEditor(language, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);
        AssertCodeBlockMatchesStandaloneHighlight(markdown, code, standaloneEditor, markdownEditor);
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithMultiLineXml_HighlightsXmlScopes()
    {
        var code = """
        <workspace title="Demo Board">
          <group id="card-001" enabled="true">
            <item>98.5</item>
            <item>中文</item>
          </group>
        </workspace>
        """.Replace("\r\n", "\n");

        var markdown = CreateCodeBlock("xml", code);
        var markdownEditor = CreateHighlightedEditor(markdown);

        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);

        AssertMarkdownXmlHighlight(markdownEditor, markdown, code,
            classMemberTokenList: [("workspace", 0), ("group", 0), ("item", 0), ("item", 1), ("item", 2), ("item", 3), ("group", 1), ("workspace", 1), ("title", 0), ("id", 0), ("enabled", 0)],
            stringTokenList: ["\"Demo Board\"", "\"card-001\"", "\"true\""],
            plainTextTokenList: [("98.5", 0), ("中文", 0)]);
    }

    [Fact]
    public void ApplyHighlight_MultipleXmlCodeBlocksWithTextBetween_UsesMatchingHighlightForEachBlock()
    {
        const string firstCode = "<bb>123</bb>";
        const string secondCode = "<root><item id=\"2\">next</item></root>";
        var markdown = $"intro\n\n{CreateCodeBlock("xml", firstCode)}\n\nbridge\n\n{CreateCodeBlock("xml", secondCode)}\n\noutro";

        var markdownEditor = CreateHighlightedEditor(markdown);
        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);
        DocumentHighlighterTestHelper.AssertScopeColor(markdownEditor, markdown, "intro", ScopeType.PlainText);
        DocumentHighlighterTestHelper.AssertScopeColor(markdownEditor, markdown, "bridge", ScopeType.PlainText);
        DocumentHighlighterTestHelper.AssertScopeColor(markdownEditor, markdown, "outro", ScopeType.PlainText);

        AssertMarkdownXmlHighlight(markdownEditor, markdown, firstCode,
            classMemberTokenList: [("bb", 0), ("bb", 1)],
            stringTokenList: [],
            plainTextTokenList: [("123", 0)]);

        AssertMarkdownXmlHighlight(markdownEditor, markdown, secondCode,
            classMemberTokenList: [("root", 0), ("item", 0), ("id", 0), ("item", 1), ("root", 1)],
            stringTokenList: ["\"2\""],
            plainTextTokenList: [("next", 0)]);
    }

    [Theory]
    [MemberData(nameof(OtherLanguageCodeBlockData))]
    public void ApplyHighlight_CodeBlockWithOtherLanguage_HighlightsInnerCode(string language, string code)
    {
        var markdown = CreateCodeBlock(language, code);

        var markdownEditor = CreateHighlightedEditor(markdown);
        var standaloneEditor = CreateStandaloneCodeEditor(language, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);
        AssertCodeBlockMatchesStandaloneHighlight(markdown, code, standaloneEditor, markdownEditor);
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithoutLanguage_KeepsInnerCodeAsPlainText()
    {
        const string markdown = "```\nconst value = 10;\n```";

        var textEditor = CreateHighlightedEditor(markdown);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, markdown);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, markdown, "const value = 10;", ScopeType.PlainText);
    }

    [Fact]
    public void ApplyHighlight_XmlCodeBlockWithTrailingBlankLine_HighlightsLikeStandaloneXml()
    {
        const string language = "xml";
        const string code = "<bb>123</bb>\n";
        var markdown = CreateCodeBlock(language, code);

        var markdownEditor = CreateHighlightedEditor(markdown);
        var standaloneEditor = CreateStandaloneCodeEditor(language, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);
        AssertCodeBlockMatchesStandaloneHighlight(markdown, code, standaloneEditor, markdownEditor);
    }

    [Fact]
    public void ApplyHighlight_MultipleCodeBlocksWithTextBetween_UsesMatchingHighlightForEachBlock()
    {
        const string firstLanguage = "xml";
        const string firstCode = "<bb>123</bb>\n";
        const string secondLanguage = "csharp";
        const string secondCode = "var total = 42;";
        var markdown = $"before\n\n{CreateCodeBlock(firstLanguage, firstCode)}\n\nmiddle\n\n{CreateCodeBlock(secondLanguage, secondCode)}\n\nafter";

        var markdownEditor = CreateHighlightedEditor(markdown);
        var firstStandaloneEditor = CreateStandaloneCodeEditor(firstLanguage, firstCode);
        var secondStandaloneEditor = CreateStandaloneCodeEditor(secondLanguage, secondCode);

        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);
        DocumentHighlighterTestHelper.AssertScopeColor(markdownEditor, markdown, "before", ScopeType.PlainText);
        DocumentHighlighterTestHelper.AssertScopeColor(markdownEditor, markdown, "middle", ScopeType.PlainText);
        DocumentHighlighterTestHelper.AssertScopeColor(markdownEditor, markdown, "after", ScopeType.PlainText);
        AssertCodeBlockMatchesStandaloneHighlight(markdown, firstCode, firstStandaloneEditor, markdownEditor);
        AssertCodeBlockMatchesStandaloneHighlight(markdown, secondCode, secondStandaloneEditor, markdownEditor);
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithMultiLineJson_HighlightsJsonScopes()
    {
        var code = """
        {
          "catalog": {
            "name": "Widget Set",
            "revision": 7,
            "entries": [
              {
                "id": "w-01",
                "enabled": true
              },
              {
                "id": "w-02",
                "enabled": false,
                "note": null
              }
            ]
          }
        }
        """.Replace("\r\n", "\n");

        var markdown = CreateCodeBlock("json", code);
        var markdownEditor = CreateHighlightedEditor(markdown);

        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);
        AssertJsonHighlight(markdownEditor, markdown,
            keyTokenList: ["\"catalog\"", "\"name\"", "\"revision\"", "\"entries\"", "\"id\"", "\"enabled\"", "\"note\""],
            stringValueTokenList: ["\"Widget Set\"", "\"w-01\"", "\"w-02\""],
            numberTokenList: ["7"],
            constantTokenList: ["true", "false", "null"]);
    }

    private static string CreateCodeBlock(string language, string code)
    {
        return $"```{language}\n{code}\n```";
    }

    private static TextEditor CreateHighlightedEditor(string markdown)
    {
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        textEditor.AppendText(markdown);
        highlighter.ApplyHighlight(markdown);
        return textEditor;
    }

    private static TextEditor CreateStandaloneCodeEditor(string language, string code)
    {
        var textEditor = new TextEditor();
        textEditor.AppendText(code);

        if (IsCsharpCodeLanguage(language))
        {
            var highlighter = new CSharpDocumentHighlighter(textEditor);
            highlighter.ApplyHighlight(code);
            return textEditor;
        }

        var otherLanguageId = GetOtherLanguageId(language);
        var otherHighlighter = new OtherCodeDocumentHighlighter(textEditor, otherLanguageId);
        otherHighlighter.ApplyHighlight(code);
        return textEditor;
    }

    private static void AssertCodeBlockMatchesStandaloneHighlight(string markdown, string code, TextEditor standaloneEditor, TextEditor markdownEditor)
    {
        var codeStart = markdown.IndexOf(code, StringComparison.Ordinal);
        Assert.True(codeStart >= 0);

        DocumentHighlighterTestHelper.AssertSameForegroundColors(standaloneEditor, 0, markdownEditor, codeStart, code.Length);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(markdownEditor);
    }

    private static void AssertMarkdownCodeTokenMatchesStandalone(string markdown, TextEditor standaloneEditor, TextEditor markdownEditor, string token, int occurrence = 0)
    {
        var standaloneStart = DocumentHighlighterTestHelper.GetOccurrenceStart(DocumentHighlighterTestHelper.GetEditorText(standaloneEditor), token, occurrence);
        var markdownStart = DocumentHighlighterTestHelper.GetOccurrenceStart(markdown, token, occurrence);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(standaloneEditor, standaloneStart, markdownEditor, markdownStart, token.Length);
    }

    private static void AssertMarkdownCodeTokenMatchesStandalone(string markdown, string code, TextEditor standaloneEditor, TextEditor markdownEditor, string token, int occurrence = 0)
    {
        var standaloneText = DocumentHighlighterTestHelper.GetEditorText(standaloneEditor);
        var standaloneStart = DocumentHighlighterTestHelper.GetOccurrenceStart(standaloneText, token, occurrence);

        var codeStart = markdown.IndexOf(code, StringComparison.Ordinal);
        Assert.True(codeStart >= 0);

        var codeTokenStart = DocumentHighlighterTestHelper.GetOccurrenceStart(code, token, occurrence);
        DocumentHighlighterTestHelper.AssertSameForegroundColors(standaloneEditor, standaloneStart, markdownEditor, codeStart + codeTokenStart, token.Length);
    }

    private static bool IsCsharpCodeLanguage(string language)
    {
        language = language.Trim();

        return language.Equals("csharp", StringComparison.OrdinalIgnoreCase)
               || language.Equals("cs", StringComparison.OrdinalIgnoreCase)
               || language.Equals("C#", StringComparison.OrdinalIgnoreCase)
               || language.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOtherLanguageId(string language)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "asax" => LanguageId.Asax,
            "ashx" => LanguageId.Ashx,
            "aspx" => LanguageId.Aspx,
            "aspx-cs" or "aspxcs" or "cshtml" => LanguageId.AspxCs,
            "aspx-vb" or "aspxvb" or "vbhtml" => LanguageId.AspxVb,
            "c" or "cpp" or "c++" or "cc" or "cxx" or "hpp" or "h" => LanguageId.Cpp,
            "css" => LanguageId.Css,
            "fortran" or "f90" or "f95" => LanguageId.Fortran,
            "html" or "htm" => LanguageId.Html,
            "haskell" or "hs" => LanguageId.Haskell,
            "java" => LanguageId.Java,
            "javascript" or "js" or "node" => LanguageId.JavaScript,
            "json" => "json",
            "koka" => LanguageId.Koka,
            "markdown" or "md" => LanguageId.Markdown,
            "matlab" => LanguageId.MatLab,
            "php" => LanguageId.Php,
            "powershell" or "pwsh" or "ps1" => LanguageId.PowerShell,
            "python" or "py" => LanguageId.Python,
            "sql" => LanguageId.Sql,
            "typescript" or "ts" or "tsx" => LanguageId.TypeScript,
            "vb" or "vbnet" or "vb.net" => LanguageId.VbDotNet,
            "xml" or "xaml" or "axaml" or "svg" => LanguageId.Xml,
            "f#" or "fsharp" or "fs" => LanguageId.FSharp,
            _ => throw new InvalidOperationException($"Unsupported test language '{language}'.")
        };
    }

    private static void AssertJsonHighlight(TextEditor textEditor, string text, IEnumerable<string> keyTokenList,
        IEnumerable<string> stringValueTokenList, IEnumerable<string> numberTokenList, IEnumerable<string> constantTokenList)
    {
        foreach (var keyToken in keyTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, keyToken, ScopeType.ClassMember);
        }

        foreach (var stringValueToken in stringValueTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, stringValueToken, ScopeType.String);
        }

        foreach (var numberToken in numberTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, numberToken, ScopeType.Number);
        }

        foreach (var constantToken in constantTokenList)
        {
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, constantToken, ScopeType.DeclarationTypeSyntax);
        }
    }

    private static void AssertMarkdownXmlHighlight(TextEditor textEditor, string markdown, string code,
        IEnumerable<(string Token, int Occurrence)> classMemberTokenList, IEnumerable<string> stringTokenList,
        IEnumerable<(string Token, int Occurrence)> plainTextTokenList, IEnumerable<(string Token, int Occurrence)>? commentTokenList = null)
    {
        var codeStart = markdown.IndexOf(code, StringComparison.Ordinal);
        Assert.True(codeStart >= 0);

        foreach (var (token, occurrence) in classMemberTokenList)
        {
            var tokenStart = DocumentHighlighterTestHelper.GetOccurrenceStart(code, token, occurrence);
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, codeStart + tokenStart, token.Length, ScopeType.ClassMember);
        }

        foreach (var token in stringTokenList)
        {
            var tokenStart = DocumentHighlighterTestHelper.GetOccurrenceStart(code, token, 0);
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, codeStart + tokenStart, token.Length, ScopeType.String);
        }

        foreach (var (token, occurrence) in plainTextTokenList)
        {
            var tokenStart = DocumentHighlighterTestHelper.GetOccurrenceStart(code, token, occurrence);
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, codeStart + tokenStart, token.Length, ScopeType.PlainText);
        }

        if (commentTokenList is null)
        {
            return;
        }

        foreach (var (token, occurrence) in commentTokenList)
        {
            var tokenStart = DocumentHighlighterTestHelper.GetOccurrenceStart(code, token, occurrence);
            DocumentHighlighterTestHelper.AssertScopeColor(textEditor, codeStart + tokenStart, token.Length, ScopeType.Comment);
        }
    }
}
