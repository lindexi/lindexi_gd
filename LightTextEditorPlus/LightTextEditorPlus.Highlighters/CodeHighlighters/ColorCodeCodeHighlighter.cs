using System;
using System.Collections.Generic;
using System.Threading;

using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;

using Microsoft.CodeAnalysis.Text;

namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

public sealed class ColorCodeCodeHighlighter : ICodeHighlighter
{
    private static readonly ILanguageCompiler LanguageCompiler = new LanguageCompiler([], new ReaderWriterLockSlim());
    private static readonly ILanguageRepository LanguageRepository = CreateLanguageRepository();
    private static readonly LanguageParser LanguageParser = new(LanguageCompiler, LanguageRepository);

    public required string LanguageId { get; init; }

    public void ApplyHighlight(in HighlightCodeContext context)
    {
        var language = LanguageRepository.FindById(LanguageId);
        if (language is null)
        {
            throw new InvalidOperationException($"Unable to find ColorCode language '{LanguageId}'.");
        }

        string code = context.PlainCode;
        var coloredSegmentList = new List<(TextSpan Span, ScopeType Scope)>();

        LanguageParser.Parse(code, language, (parsedSourceCode, scopes) =>
        {
            CollectScope(scopes, coloredSegmentList);
        });

        FillPlainTextSegments(code.Length, coloredSegmentList, context.ColorCode);
    }

    private static ILanguageRepository CreateLanguageRepository()
    {
        var languageRepository = new LanguageRepository([]);
        foreach (var language in Languages.All)
        {
            languageRepository.Load(language);
        }

        return languageRepository;
    }

    private static void CollectScope(IEnumerable<Scope> scopeList, List<(TextSpan Span, ScopeType Scope)> coloredSegmentList)
    {
        foreach (var scope in scopeList)
        {
            if (scope.Length > 0)
            {
                coloredSegmentList.Add((new TextSpan(scope.Index, scope.Length), MapScopeType(scope.Name)));
            }

            if (scope.Children.Count > 0)
            {
                CollectScope(scope.Children, coloredSegmentList);
            }
        }
    }

    private static void FillPlainTextSegments(int textLength, List<(TextSpan Span, ScopeType Scope)> coloredSegmentList, IColorCode colorCode)
    {
        if (textLength <= 0)
        {
            return;
        }

        coloredSegmentList.Sort(static (left, right) =>
        {
            int startComparison = left.Span.Start.CompareTo(right.Span.Start);
            return startComparison != 0 ? startComparison : left.Span.Length.CompareTo(right.Span.Length);
        });

        int currentPosition = 0;
        foreach (var (span, scope) in coloredSegmentList)
        {
            if (span.End <= currentPosition)
            {
                continue;
            }

            if (span.Start > currentPosition)
            {
                colorCode.FillCodeColor(TextSpan.FromBounds(currentPosition, span.Start), ScopeType.PlainText);
            }

            int start = Math.Max(span.Start, currentPosition);
            var currentSpan = TextSpan.FromBounds(start, span.End);
            colorCode.FillCodeColor(currentSpan, scope);
            currentPosition = currentSpan.End;
        }

        if (currentPosition < textLength)
        {
            colorCode.FillCodeColor(TextSpan.FromBounds(currentPosition, textLength), ScopeType.PlainText);
        }
    }

    private static ScopeType MapScopeType(string? scopeName)
    {
        return scopeName switch
        {
            ScopeName.Comment or ScopeName.HtmlComment or ScopeName.XmlComment or ScopeName.XmlDocComment => ScopeType.Comment,
            ScopeName.ClassName or ScopeName.Type or ScopeName.NameSpace => ScopeType.ClassName,
            ScopeName.Keyword or ScopeName.ControlKeyword or ScopeName.PreprocessorKeyword or ScopeName.PseudoKeyword => ScopeType.Keyword,
            ScopeName.String or ScopeName.StringCSharpVerbatim or ScopeName.StringEscape or ScopeName.HtmlAttributeValue or ScopeName.XmlAttributeValue or ScopeName.JsonString => ScopeType.String,
            ScopeName.Number or ScopeName.JsonNumber => ScopeType.Number,
            ScopeName.Brackets or ScopeName.Delimiter or ScopeName.HtmlTagDelimiter or ScopeName.XmlDelimiter or ScopeName.XmlAttributeQuotes => ScopeType.Brackets,
            ScopeName.PowerShellVariable or ScopeName.TypeVariable => ScopeType.Variable,
            ScopeName.BuiltinFunction or ScopeName.PowerShellCommand or ScopeName.SqlSystemFunction or ScopeName.Constructor => ScopeType.Invocation,
            ScopeName.HtmlElementName or ScopeName.HtmlAttributeName or ScopeName.XmlAttribute or ScopeName.XmlName or ScopeName.CssPropertyName or ScopeName.CssSelector or ScopeName.JsonKey or ScopeName.Attribute => ScopeType.ClassMember,
            ScopeName.BuiltinValue or ScopeName.Predefined or ScopeName.Intrinsic or ScopeName.PowerShellType or ScopeName.CssPropertyValue or ScopeName.JsonConst or ScopeName.SpecialCharacter or ScopeName.HtmlEntity or ScopeName.XmlCDataSection => ScopeType.DeclarationTypeSyntax,
            _ => ScopeType.PlainText,
        };
    }
}
