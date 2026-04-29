using ColorCode.Common;
using LightTextEditorPlus.Highlighters.CodeHighlighters;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class MarkdownCodeBlockHighlightingTests
{
    public static TheoryData<string, string> OtherLanguageCodeBlockData => new()
    {
        { "javascript", "const value = \"done\";" },
        { "js", "const value = \"done\";" },
        { "python", "class Person:\n    pass" },
        { "py", "class Person:\n    pass" },
        { "java", "public class Hello { }" },
        { "cpp", "int main() { return 0; }" },
        { "c", "int main() { return 0; }" },
        { "sql", "SELECT * FROM Users" },
        { "xml", "<root><item>value</item></root>" },
        { "json", "{\"name\": \"value\", \"count\": 1}" },
        { "html", "<div class=\"note\">Hello</div>" },
        { "css", ".container { color: red; }" },
        { "typescript", "interface Person { name: string; }" },
        { "ts", "interface Person { name: string; }" },
        { "fsharp", "let value = 10" },
        { "vb", "Dim value As Integer = 10" },
        { "php", "<?php function hello($name) { echo \"Hello\"; } ?>" },
    };

    [Theory]
    [InlineData("csharp")]
    [InlineData("cs")]
    [InlineData("C#")]
    [InlineData("dotnet")]
    public void ApplyHighlight_CodeBlockWithCSharpAlias_HighlightsInnerCode(string language)
    {
        const string code = "var value = 42;";
        var markdown = CreateCodeBlock(language, code);

        var markdownEditor = CreateHighlightedEditor(markdown);
        var standaloneEditor = CreateStandaloneCodeEditor(language, code);

        DocumentHighlighterTestHelper.AssertTextPreserved(markdownEditor, markdown);
        AssertCodeBlockMatchesStandaloneHighlight(markdown, code, standaloneEditor, markdownEditor);
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

    private static bool IsCsharpCodeLanguage(string language)
    {
        return language.Equals("csharp", StringComparison.OrdinalIgnoreCase)
               || language.Equals("cs", StringComparison.OrdinalIgnoreCase)
               || language.Equals("C#", StringComparison.OrdinalIgnoreCase)
               || language.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOtherLanguageId(string language)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "c" or "cpp" or "c++" or "cc" or "cxx" or "hpp" or "h" => LanguageId.Cpp,
            "css" => LanguageId.Css,
            "html" or "htm" => LanguageId.Html,
            "java" => LanguageId.Java,
            "javascript" or "js" or "node" => LanguageId.JavaScript,
            "json" => "json",
            "php" => LanguageId.Php,
            "python" or "py" => LanguageId.Python,
            "sql" => LanguageId.Sql,
            "typescript" or "ts" or "tsx" => LanguageId.TypeScript,
            "vb" or "vbnet" or "vb.net" => LanguageId.VbDotNet,
            "xml" or "xaml" or "axaml" or "svg" => LanguageId.Xml,
            "f#" or "fsharp" or "fs" => LanguageId.FSharp,
            _ => throw new InvalidOperationException($"Unsupported test language '{language}'.")
        };
    }
}
