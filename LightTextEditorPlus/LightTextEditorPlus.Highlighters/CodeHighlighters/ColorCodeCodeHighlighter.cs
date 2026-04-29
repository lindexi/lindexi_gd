using System;
using System.Collections.Generic;
using System.Threading;

using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;

using Microsoft.CodeAnalysis.Text;

namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

/// <summary>
/// 基于 ColorCode 为文本应用语法高亮。
/// </summary>
public sealed class ColorCodeCodeHighlighter : ICodeHighlighter
{
    private readonly record struct ColoredSegment(TextSpan Span, ScopeType Scope, int Order);

    private static readonly ILanguageCompiler LanguageCompiler = new LanguageCompiler([], new ReaderWriterLockSlim());
    private static readonly ILanguageRepository LanguageRepository = CreateLanguageRepository();
    private static readonly LanguageParser LanguageParser = new(LanguageCompiler, LanguageRepository);

    /// <summary>
    /// 获取用于解析代码的语言标识。
    /// </summary>
    public required string LanguageId { get; init; }

    /// <summary>
    /// 对代码上下文应用 ColorCode 高亮。
    /// </summary>
    /// <param name="context">代码内容与着色输出上下文。</param>
    public void ApplyHighlight(in HighlightCodeContext context)
    {
        var language = LanguageRepository.FindById(LanguageId);
        if (language is null)
        {
            throw new InvalidOperationException($"Unable to find ColorCode language '{LanguageId}'.");
        }

        string code = context.PlainCode;
        var coloredSegmentList = new List<ColoredSegment>();

        LanguageParser.Parse(code, language, (parsedSourceCode, scopes) =>
        {
            var order = 0;
            CollectScope(scopes, coloredSegmentList, ref order);
        });

        FillTextSegments(code.Length, coloredSegmentList, context.ColorCode);
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

    private static void CollectScope(IEnumerable<Scope> scopeList, List<ColoredSegment> coloredSegmentList, ref int order)
    {
        foreach (var scope in scopeList)
        {
            if (scope.Length > 0)
            {
                coloredSegmentList.Add(new ColoredSegment(new TextSpan(scope.Index, scope.Length), MapScopeType(scope.Name), order));
                order++;
            }

            if (scope.Children.Count > 0)
            {
                CollectScope(scope.Children, coloredSegmentList, ref order);
            }
        }
    }

    private static void FillTextSegments(int textLength, List<ColoredSegment> coloredSegmentList, IColorCode colorCode)
    {
        if (textLength <= 0)
        {
            return;
        }

        var flattenedSegments = FlattenSegments(textLength, coloredSegmentList);
        foreach (var (span, scope, _) in flattenedSegments)
        {
            colorCode.FillCodeColor(span, scope);
        }
    }

    private static List<ColoredSegment> FlattenSegments(int textLength, List<ColoredSegment> coloredSegmentList)
    {
        var boundaryList = new List<int>(coloredSegmentList.Count * 2 + 2)
        {
            0,
            textLength
        };

        foreach (var (span, _, _) in coloredSegmentList)
        {
            if (span.Length <= 0)
            {
                continue;
            }

            boundaryList.Add(span.Start);
            boundaryList.Add(span.End);
        }

        boundaryList.Sort();

        var distinctBoundaries = new List<int>(boundaryList.Count);
        int? previousBoundary = null;
        foreach (var boundary in boundaryList)
        {
            if (previousBoundary == boundary)
            {
                continue;
            }

            distinctBoundaries.Add(boundary);
            previousBoundary = boundary;
        }

        var flattenedSegments = new List<ColoredSegment>();

        for (var i = 0; i < distinctBoundaries.Count - 1; i++)
        {
            int start = distinctBoundaries[i];
            int end = distinctBoundaries[i + 1];
            if (start >= end)
            {
                continue;
            }

            var currentSpan = TextSpan.FromBounds(start, end);
            var segment = SelectBestSegment(currentSpan, coloredSegmentList);

            if (flattenedSegments.Count > 0)
            {
                var last = flattenedSegments[^1];
                if (last.Scope == segment.Scope && last.Span.End == segment.Span.Start)
                {
                    flattenedSegments[^1] = last with
                    {
                        Span = TextSpan.FromBounds(last.Span.Start, segment.Span.End)
                    };
                    continue;
                }
            }

            flattenedSegments.Add(segment);
        }

        return flattenedSegments;
    }

    private static ColoredSegment SelectBestSegment(TextSpan currentSpan, List<ColoredSegment> coloredSegmentList)
    {
        ColoredSegment? bestSegment = null;

        foreach (var segment in coloredSegmentList)
        {
            if (segment.Span.Start > currentSpan.Start || segment.Span.End < currentSpan.End)
            {
                continue;
            }

            if (bestSegment is null || IsBetter(segment, bestSegment.Value))
            {
                bestSegment = segment;
            }
        }

        if (bestSegment is null)
        {
            return new ColoredSegment(currentSpan, ScopeType.PlainText, -1);
        }

        return new ColoredSegment(currentSpan, bestSegment.Value.Scope, bestSegment.Value.Order);
    }

    private static bool IsBetter(ColoredSegment current, ColoredSegment existing)
    {
        int lengthComparison = current.Span.Length.CompareTo(existing.Span.Length);
        if (lengthComparison != 0)
        {
            return lengthComparison < 0;
        }

        return current.Order > existing.Order;
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
