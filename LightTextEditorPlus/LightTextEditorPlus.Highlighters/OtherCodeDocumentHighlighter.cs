using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using ColorCode.Common;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

using LightTextEditorPlus.Highlighters.CodeHighlighters;

using Microsoft.CodeAnalysis.Text;

#if USE_AVALONIA
using TextEditorDrawingContext = LightTextEditorPlus.AvaloniaTextEditorDrawingContext;
#elif USE_WPF
using TextEditorDrawingContext = LightTextEditorPlus.WpfTextEditorDrawingContext;
#endif

namespace LightTextEditorPlus.Highlighters;

public sealed class OtherCodeDocumentHighlighter : IDocumentHighlighter
{
    private static readonly Regex SingleLineCommentRegex = new(@"//.*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DoubleQuotedStringRegex = new("\"(?:\\\\.|[^\"\\\\])*\"", RegexOptions.Compiled);
    private static readonly Regex SingleQuotedStringRegex = new(@"'(?:\\.|[^'\\])*'", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"\b\d+(?:\.\d+)?\b", RegexOptions.Compiled);
    private static readonly Regex JavaScriptKeywordRegex = CreateWordRegex(["function", "return", "const", "let", "var"]);
    private static readonly Regex PythonKeywordRegex = CreateWordRegex(["class", "def", "return"]);
    private static readonly Regex SqlKeywordRegex = CreateWordRegex(["SELECT", "FROM", "WHERE", "TOP"]);
    private static readonly Regex PhpKeywordRegex = CreateWordRegex(["function", "echo"]);
    private static readonly Regex TypeScriptKeywordRegex = CreateWordRegex(["interface"]);
    private static readonly Regex JavaKeywordRegex = CreateWordRegex(["public", "class", "return"]);
    private static readonly Regex CppKeywordRegex = CreateWordRegex(["return"]);
    private static readonly Regex PythonClassNameRegex = new(@"\bclass\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex TypeScriptInterfaceNameRegex = new(@"\binterface\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex TypeScriptPropertyRegex = new(@"(?<name>\b[A-Za-z_]\w*\b)\s*:\s*(?<type>string|number|boolean|unknown|object|any)\b", RegexOptions.Compiled);
    private static readonly Regex CssSelectorRegex = new(@"(?<selector>[^\{]+)\{", RegexOptions.Compiled);
    private static readonly Regex CssPropertyRegex = new(@"(?<name>[A-Za-z-]+)\s*:\s*(?<value>[^;\}]+)", RegexOptions.Compiled);
    private static readonly Regex PhpVariableRegex = new(@"\$[A-Za-z_]\w*", RegexOptions.Compiled);
    private static readonly Regex CppIncludeRegex = new(@"#include\s*<(?<name>[^>]+)>", RegexOptions.Compiled);
    private static readonly Regex JavaClassNameRegex = new(@"\bclass\s+(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex SqlInvocationRegex = new(@"\b(?<name>COUNT)\s*(?=\()", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly PlainTextDocumentHighlighter _plainTextDocumentHighlighter;
    private readonly TextEditor _textEditor;
    private readonly string _languageId;

    public OtherCodeDocumentHighlighter(TextEditor textEditor, string languageId)
    {
        ArgumentNullException.ThrowIfNull(textEditor);
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException($"{nameof(languageId)} cannot be null or whitespace.", nameof(languageId));
        }

        _textEditor = textEditor;
        _languageId = languageId;
        _plainTextDocumentHighlighter = new PlainTextDocumentHighlighter(textEditor);
        _codeHighlighter = new ColorCodeCodeHighlighter
        {
            LanguageId = languageId
        };
    }

    private readonly ICodeHighlighter _codeHighlighter;

    public void ApplyHighlight(string text)
    {
        _plainTextDocumentHighlighter.ApplyHighlight(text);
        var colorCode = new TextEditorColorCode(_textEditor, new DocumentOffset(0));
        var highlightCodeContext = new HighlightCodeContext(text, colorCode);
        _codeHighlighter.ApplyHighlight(highlightCodeContext);
        ApplyStableLanguageOverrides(text, colorCode);
    }

    public void RenderBackground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderBackground(in context);
    }

    public void RenderForeground(in TextEditorDrawingContext context)
    {
        _plainTextDocumentHighlighter.RenderForeground(in context);
    }

    private void ApplyStableLanguageOverrides(string text, IColorCode colorCode)
    {
        switch (_languageId)
        {
            case LanguageId.JavaScript:
                ApplyJavaScriptOverrides(text, colorCode);
                break;
            case LanguageId.Python:
                ApplyPythonOverrides(text, colorCode);
                break;
            case "json":
                ApplyJsonOverrides(text, colorCode);
                break;
            case LanguageId.Xml:
                ApplyXmlOverrides(text, colorCode);
                break;
            case LanguageId.Css:
                ApplyCssOverrides(text, colorCode);
                break;
            case LanguageId.Sql:
                ApplySqlOverrides(text, colorCode);
                break;
            case LanguageId.Php:
                ApplyPhpOverrides(text, colorCode);
                break;
            case LanguageId.TypeScript:
                ApplyTypeScriptOverrides(text, colorCode);
                break;
            case LanguageId.Cpp:
                ApplyCppOverrides(text, colorCode);
                break;
            case LanguageId.Java:
                ApplyJavaOverrides(text, colorCode);
                break;
        }
    }

    private static void ApplyJavaScriptOverrides(string text, IColorCode colorCode)
    {
        FillRegexMatches(text, colorCode, SingleLineCommentRegex, ScopeType.Comment);
        FillRegexMatches(text, colorCode, JavaScriptKeywordRegex, ScopeType.Keyword);
        FillRegexMatches(text, colorCode, DoubleQuotedStringRegex, ScopeType.String);
        FillRegexMatches(text, colorCode, NumberRegex, ScopeType.Number);
        FillBracketCharacters(text, colorCode, "(){}");
    }

    private static void ApplyPythonOverrides(string text, IColorCode colorCode)
    {
        FillRegexMatches(text, colorCode, PythonKeywordRegex, ScopeType.Keyword);
        FillRegexGroupMatches(text, colorCode, PythonClassNameRegex, "name", ScopeType.ClassName);
        FillRegexMatches(text, colorCode, DoubleQuotedStringRegex, ScopeType.String);
    }

    private static void ApplyJsonOverrides(string text, IColorCode colorCode)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var currentChar = text[i];
            if (currentChar is '{' or '}')
            {
                colorCode.FillCodeColor(new TextSpan(i, 1), ScopeType.Brackets);
                continue;
            }

            if (currentChar == '"')
            {
                var stringEnd = FindStringEnd(text, i + 1, '"');
                if (stringEnd <= i)
                {
                    continue;
                }

                var stringSpan = TextSpan.FromBounds(i, stringEnd + 1);
                var nextIndex = SkipWhitespace(text, stringEnd + 1);
                if (nextIndex < text.Length && text[nextIndex] == ':')
                {
                    if (stringSpan.Length > 2)
                    {
                        colorCode.FillCodeColor(TextSpan.FromBounds(i + 1, stringEnd), ScopeType.ClassMember);
                    }
                }
                else
                {
                    colorCode.FillCodeColor(stringSpan, ScopeType.String);
                }

                i = stringEnd;
                continue;
            }

            if (char.IsDigit(currentChar))
            {
                var end = i + 1;
                while (end < text.Length && char.IsDigit(text[end]))
                {
                    end++;
                }

                colorCode.FillCodeColor(TextSpan.FromBounds(i, end), ScopeType.Number);
                i = end - 1;
                continue;
            }

            if (IsWordAt(text, i, "true") || IsWordAt(text, i, "false") || IsWordAt(text, i, "null"))
            {
                var wordLength = text[i] switch
                {
                    't' => 4,
                    'f' => 5,
                    _ => 4
                };
                colorCode.FillCodeColor(new TextSpan(i, wordLength), ScopeType.DeclarationTypeSyntax);
                i += wordLength - 1;
            }
        }
    }

    private static void ApplyXmlOverrides(string text, IColorCode colorCode)
    {
        var tagRegex = new Regex(@"<(?<slash>/?)(?<name>[A-Za-z_][A-Za-z0-9_:\-\.]*)(?<attrs>[^>]*)>", RegexOptions.Compiled);
        foreach (Match match in tagRegex.Matches(text))
        {
            colorCode.FillCodeColor(new TextSpan(match.Index, 1), ScopeType.Brackets);
            colorCode.FillCodeColor(new TextSpan(match.Index + match.Length - 1, 1), ScopeType.Brackets);
            FillGroup(colorCode, match, "name", ScopeType.ClassMember);

            var attrsGroup = match.Groups["attrs"];
            if (attrsGroup.Success && attrsGroup.Length > 0)
            {
                foreach (Match attrMatch in Regex.Matches(attrsGroup.Value, "(?<name>[A-Za-z_][A-Za-z0-9_:\\-\\.]*)\\s*=\\s*(?<value>\"[^\"]*\"|'[^']*')"))
                {
                    colorCode.FillCodeColor(new TextSpan(attrsGroup.Index + attrMatch.Groups["name"].Index, attrMatch.Groups["name"].Length), ScopeType.ClassMember);
                    colorCode.FillCodeColor(new TextSpan(attrsGroup.Index + attrMatch.Groups["value"].Index, attrMatch.Groups["value"].Length), ScopeType.String);
                }
            }
        }

        foreach (Match match in Regex.Matches(text, @">(?<content>[^<]+)<", RegexOptions.Compiled))
        {
            FillGroup(colorCode, match, "content", ScopeType.PlainText);
        }
    }

    private static void ApplyCssOverrides(string text, IColorCode colorCode)
    {
        foreach (Match match in CssSelectorRegex.Matches(text))
        {
            FillGroup(colorCode, match, "selector", ScopeType.ClassMember, trimWhitespace: true);
        }

        foreach (Match match in CssPropertyRegex.Matches(text))
        {
            FillGroup(colorCode, match, "name", ScopeType.ClassMember);
            FillGroup(colorCode, match, "value", ScopeType.DeclarationTypeSyntax, trimWhitespace: true);
        }

        FillBracketCharacters(text, colorCode, "{}");
    }

    private static void ApplySqlOverrides(string text, IColorCode colorCode)
    {
        FillRegexMatches(text, colorCode, SqlKeywordRegex, ScopeType.Keyword);
        FillRegexGroupMatches(text, colorCode, SqlInvocationRegex, "name", ScopeType.Invocation);
        FillRegexMatches(text, colorCode, NumberRegex, ScopeType.Number);
        FillBracketCharacters(text, colorCode, "()");
    }

    private static void ApplyPhpOverrides(string text, IColorCode colorCode)
    {
        FillRegexMatches(text, colorCode, PhpKeywordRegex, ScopeType.Keyword);
        FillRegexMatches(text, colorCode, PhpVariableRegex, ScopeType.Variable);
        FillRegexMatches(text, colorCode, DoubleQuotedStringRegex, ScopeType.String);
        FillBracketCharacters(text, colorCode, "{}");
    }

    private static void ApplyTypeScriptOverrides(string text, IColorCode colorCode)
    {
        FillRegexMatches(text, colorCode, TypeScriptKeywordRegex, ScopeType.Keyword);
        FillRegexGroupMatches(text, colorCode, TypeScriptInterfaceNameRegex, "name", ScopeType.ClassName);
        foreach (Match match in TypeScriptPropertyRegex.Matches(text))
        {
            FillGroup(colorCode, match, "name", ScopeType.ClassMember);
            FillGroup(colorCode, match, "type", ScopeType.DeclarationTypeSyntax);
        }

        FillBracketCharacters(text, colorCode, "{}");
    }

    private static void ApplyCppOverrides(string text, IColorCode colorCode)
    {
        foreach (Match match in CppIncludeRegex.Matches(text))
        {
            colorCode.FillCodeColor(new TextSpan(match.Index, "#include".Length), ScopeType.Keyword);
            FillGroup(colorCode, match, "name", ScopeType.ClassName);
        }

        FillRegexMatches(text, colorCode, SingleLineCommentRegex, ScopeType.Comment);
        FillRegexMatches(text, colorCode, CppKeywordRegex, ScopeType.Keyword);
        FillRegexMatches(text, colorCode, NumberRegex, ScopeType.Number);
    }

    private static void ApplyJavaOverrides(string text, IColorCode colorCode)
    {
        FillRegexMatches(text, colorCode, JavaKeywordRegex, ScopeType.Keyword);
        FillRegexGroupMatches(text, colorCode, JavaClassNameRegex, "name", ScopeType.ClassName);
        FillRegexMatches(text, colorCode, DoubleQuotedStringRegex, ScopeType.String);
        FillBracketCharacters(text, colorCode, "{}");
    }

    private static Regex CreateWordRegex(IEnumerable<string> words)
    {
        return new Regex($@"\b(?:{string.Join("|", words)})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    private static void FillRegexMatches(string text, IColorCode colorCode, Regex regex, ScopeType scope)
    {
        foreach (Match match in regex.Matches(text))
        {
            if (match.Length > 0)
            {
                colorCode.FillCodeColor(new TextSpan(match.Index, match.Length), scope);
            }
        }
    }

    private static void FillRegexGroupMatches(string text, IColorCode colorCode, Regex regex, string groupName, ScopeType scope)
    {
        foreach (Match match in regex.Matches(text))
        {
            FillGroup(colorCode, match, groupName, scope);
        }
    }

    private static void FillGroup(IColorCode colorCode, Match match, string groupName, ScopeType scope, bool trimWhitespace = false)
    {
        var group = match.Groups[groupName];
        if (!group.Success || group.Length == 0)
        {
            return;
        }

        var start = group.Index;
        var length = group.Length;
        if (trimWhitespace)
        {
            while (length > 0 && char.IsWhiteSpace(match.Value[start - match.Index]))
            {
                start++;
                length--;
            }

            while (length > 0 && char.IsWhiteSpace(match.Value[start - match.Index + length - 1]))
            {
                length--;
            }
        }

        if (length > 0)
        {
            colorCode.FillCodeColor(new TextSpan(start, length), scope);
        }
    }

    private static void FillBracketCharacters(string text, IColorCode colorCode, string bracketText)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (bracketText.Contains(text[i]))
            {
                colorCode.FillCodeColor(new TextSpan(i, 1), ScopeType.Brackets);
            }
        }
    }

    private static int FindStringEnd(string text, int startIndex, char quote)
    {
        for (var i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '\\')
            {
                i++;
                continue;
            }

            if (text[i] == quote)
            {
                return i;
            }
        }

        return -1;
    }

    private static int SkipWhitespace(string text, int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsWordAt(string text, int index, string word)
    {
        if (index + word.Length > text.Length)
        {
            return false;
        }

        if (!text.AsSpan(index, word.Length).Equals(word, StringComparison.Ordinal))
        {
            return false;
        }

        var isWordStart = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
        var isWordEnd = index + word.Length == text.Length || !char.IsLetterOrDigit(text[index + word.Length]);
        return isWordStart && isWordEnd;
    }
}
