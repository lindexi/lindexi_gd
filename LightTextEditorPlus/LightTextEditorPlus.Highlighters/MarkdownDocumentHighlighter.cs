using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Highlighters.CodeHighlighters;

using Markdig;
using Markdig.Syntax;

#if USE_AVALONIA
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
using TextEditorDrawingContext = LightTextEditorPlus.AvaloniaTextEditorDrawingContext;
using BackgroundBrush = Avalonia.Media.SolidColorBrush;
using FontWeightValue = SkiaSharp.SKFontStyleWeight;
using LightTextEditorPlus.Primitive;
using LightTextEditorPlus.Utils;
using SkiaSharp;
#elif USE_WPF
using RunProperty = LightTextEditorPlus.Document.RunProperty;
using TextEditorDrawingContext = LightTextEditorPlus.WpfTextEditorDrawingContext;
using BackgroundBrush = System.Windows.Media.SolidColorBrush;
using FontWeightValue = System.Windows.FontWeight;
#endif

namespace LightTextEditorPlus.Highlighters;

/// <summary>
/// 为 Markdown 文本提供标题、链接和代码块高亮。
/// </summary>
public sealed partial class MarkdownDocumentHighlighter : IDocumentHighlighter
{
    private static readonly Regex UrlRegex = new(@"https?://[^\s<>\u3000]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly TextEditor _textEditor;
    private readonly RunProperty _normalTextRunProperty;
    private readonly IReadOnlyList<RunProperty> _titleLevelRunPropertyList;
    private readonly RunProperty _codeLangInfoRunProperty;
    private readonly RunProperty _urlRunProperty;
    private readonly CsharpCodeHighlighter _csharpCodeHighlighter = new();
    private readonly BackgroundBrush _codeBackgroundColorBrush = CreateCodeBackgroundBrush();
    private readonly List<SourceSpan> _codeBlockList = [];
    private readonly List<MarkdownUrlInfo> _urlInfoList = [];
    private IReadOnlyList<HighlightSegmentSnapshot> _lastHighlightSnapshotList = [];
    /// <summary>
    /// 获取最近一次高亮后识别出的 URL 信息。
    /// </summary>
    public IReadOnlyList<MarkdownUrlInfo> UrlInfoList => _urlInfoList;

    /// <summary>
    /// 创建 Markdown 文档高亮器。
    /// </summary>
    /// <param name="textEditor">要应用高亮的文本编辑器。</param>
    public MarkdownDocumentHighlighter(TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(textEditor);

        _textEditor = textEditor;

        double normalFontSize = textEditor.StyleRunProperty.FontSize;

        _normalTextRunProperty = textEditor.StyleRunProperty;

        var titleLevel1RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 10,
            FontWeight = GetBoldFontWeight(),
        });

        var titleLevel2RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 7,
            FontWeight = GetBoldFontWeight(),
        });

        var titleLevel3RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 5,
            FontWeight = GetBoldFontWeight(),
        });

        var titleLevel4RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 3,
            FontWeight = GetBoldFontWeight(),
        });

        var titleLevel5RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 1,
            FontWeight = GetBoldFontWeight(),
        });

        _titleLevelRunPropertyList =
        [
            titleLevel1RunProperty,
            titleLevel2RunProperty,
            titleLevel3RunProperty,
            titleLevel4RunProperty,
            titleLevel5RunProperty
        ];

        _codeLangInfoRunProperty = _textEditor.CreateRunProperty(property => property with
        {
            Foreground = CreateCodeLanguageForeground()
        });

        _urlRunProperty = _textEditor.CreateRunProperty(property => property with
        {
            Foreground = CreateUrlForeground(),
            DecorationCollection = UnderlineTextEditorDecoration.Instance,
        });
    }

    /// <summary>
    /// 对当前 Markdown 文本应用高亮。
    /// </summary>
    /// <param name="markdownText">要高亮的 Markdown 文本。</param>
    public void ApplyHighlight(string markdownText)
    {
        var setter = new TextRunPropertySetter(_textEditor);

        var markdownDocument = Markdown.Parse(markdownText, MarkdownPipeline);
        var currentHighlightSnapshotList = new List<HighlightSegmentSnapshot>();
        _codeBlockList.Clear();
        _urlInfoList.Clear();
        int lastBlockEnd = -1;

        foreach (var block in markdownDocument)
        {
            var blockSpan = block.Span;
            if (!TryCreateGapSourceSpan(lastBlockEnd + 1, blockSpan.Start - 1, out var gapSpan))
            {
                gapSpan = default;
            }
            else
            {
                currentHighlightSnapshotList.Add(CreateNormalSegmentSnapshot(gapSpan));
            }

            if (block is ParagraphBlock paragraphBlock)
            {
                currentHighlightSnapshotList.Add(CreateParagraphSegmentSnapshot(paragraphBlock.Span));
                lastBlockEnd = Math.Max(lastBlockEnd, paragraphBlock.Span.End);
                continue;
            }

            if (block is HeadingBlock headingBlock)
            {
                currentHighlightSnapshotList.Add(CreateSingleOperationSegmentSnapshot(blockSpan,
                    new HighlightOperation(GetTitleLevelRunProperty(headingBlock.Level), headingBlock.Span)));
                lastBlockEnd = Math.Max(lastBlockEnd, headingBlock.Span.End);
                continue;
            }

            if (block is FencedCodeBlock fencedCodeBlock)
            {
                var sourceSpan = fencedCodeBlock.Span;
                _codeBlockList.Add(sourceSpan);

                string codeText = ToText(sourceSpan);
                var lineReader = new LineReader(codeText);
                SourceSpan firstLine = lineReader.ReadLine();
                var operationList = new List<HighlightOperation>
                {
                    new HighlightOperation(_normalTextRunProperty, sourceSpan)
                };

                ReadOnlySpan<char> codeLang = [];
                if (TryGetCodeLanguageSpan(codeText, firstLine, out var codeLangSpan))
                {
                    operationList.Add(new HighlightOperation(_codeLangInfoRunProperty,
                        new SourceSpan(codeLangSpan.Start + sourceSpan.Start, codeLangSpan.End + sourceSpan.Start)));

                    codeLang = codeText.AsSpan(codeLangSpan.Start, codeLangSpan.Length);
                }

                // 取出方法体
                SourceSpan lastLine = firstLine;
                while (true)
                {
                    var currentLine = lineReader.ReadLine();
                    if (currentLine.End < 0)
                    {
                        break;
                    }
                    lastLine = currentLine;
                }

                var innerCodeStart = firstLine.End + 1;
                while (innerCodeStart < codeText.Length && codeText[innerCodeStart] is '\r' or '\n')
                {
                    innerCodeStart++;
                }

                var innerCodeEnd = lastLine.Start - 1;
                while (innerCodeEnd >= innerCodeStart && codeText[innerCodeEnd] is '\r' or '\n')
                {
                    innerCodeEnd--;
                }

                var relativeOffset = sourceSpan.Start;
                var innerCodeSpan = new SourceSpan(innerCodeStart + relativeOffset, innerCodeEnd + relativeOffset);
                var innerCodeText = innerCodeStart <= innerCodeEnd ? ToText(innerCodeSpan) : string.Empty;
                var codeLangText = codeLang.ToString();

                currentHighlightSnapshotList.Add(new HighlightSegmentSnapshot(sourceSpan, operationList,
                    TryCreateCodeBlockHighlightSnapshot(innerCodeSpan, codeLangText, innerCodeText)));

                lastBlockEnd = Math.Max(lastBlockEnd, sourceSpan.End);
                continue;
            }

            currentHighlightSnapshotList.Add(CreateNormalSegmentSnapshot(blockSpan));
            lastBlockEnd = Math.Max(lastBlockEnd, blockSpan.End);
        }

        if (TryCreateGapSourceSpan(lastBlockEnd + 1, markdownText.Length - 1, out var tailSpan))
        {
            currentHighlightSnapshotList.Add(CreateNormalSegmentSnapshot(tailSpan));
        }

        for (int i = 0; i < currentHighlightSnapshotList.Count; i++)
        {
            var currentSnapshot = currentHighlightSnapshotList[i];
            if (i < _lastHighlightSnapshotList.Count
                && HighlightSegmentSnapshotEquals(_lastHighlightSnapshotList[i], currentSnapshot))
            {
                continue;
            }

            ApplyHighlightSegment(currentSnapshot);
        }

        _lastHighlightSnapshotList = currentHighlightSnapshotList;

        string ToText(SourceSpan span)
        {
            if (span.Start + span.Length > markdownText.Length)
            {
                // 越界了
                return string.Empty;
            }

            try
            {
                return markdownText.Substring(span.Start, span.Length);
            }
            catch (ArgumentOutOfRangeException)
            {
                GC.KeepAlive(span);
                throw;
            }
        }

        HighlightSegmentSnapshot CreateNormalSegmentSnapshot(SourceSpan sourceSpan)
        {
            return CreateSingleOperationSegmentSnapshot(sourceSpan, new HighlightOperation(_normalTextRunProperty, sourceSpan));
        }

        HighlightSegmentSnapshot CreateParagraphSegmentSnapshot(SourceSpan sourceSpan)
        {
            var operationList = new List<HighlightOperation>
            {
                new HighlightOperation(_normalTextRunProperty, sourceSpan)
            };

            string text = ToText(sourceSpan);
            if (string.IsNullOrEmpty(text))
            {
                return new HighlightSegmentSnapshot(sourceSpan, operationList, null);
            }

            foreach (Match match in UrlRegex.Matches(text))
            {
                if (!match.Success || match.Length <= 0)
                {
                    continue;
                }

                var urlText = TrimUrl(text.AsSpan(match.Index, match.Length));
                if (urlText.IsEmpty)
                {
                    continue;
                }

                int start = sourceSpan.Start + match.Index;
                var urlSourceSpan = new SourceSpan(start, start + urlText.Length - 1);

                operationList.Add(new HighlightOperation(_urlRunProperty, urlSourceSpan));
                _urlInfoList.Add(new MarkdownUrlInfo(urlSourceSpan, urlText.ToString()));
            }

            return new HighlightSegmentSnapshot(sourceSpan, operationList, null);
        }

        HighlightSegmentSnapshot CreateSingleOperationSegmentSnapshot(SourceSpan sourceSpan, HighlightOperation operation)
        {
            return new HighlightSegmentSnapshot(sourceSpan, [operation], null);
        }

        void ApplyHighlightSegment(HighlightSegmentSnapshot snapshot)
        {
            foreach (var operation in snapshot.OperationList)
            {
                setter.TrySetRunProperty(ScopeType.PlainText, operation.RunProperty, operation.SourceSpan);
            }

            if (snapshot.CodeBlockHighlightSnapshot is not { } codeBlockHighlightSnapshot)
            {
                return;
            }

            var colorCode = new TextEditorColorCode(_textEditor, new DocumentOffset(codeBlockHighlightSnapshot.InnerCodeSpan.Start));
            var highlightCodeContext = new HighlightCodeContext(codeBlockHighlightSnapshot.InnerCodeText, colorCode);
            codeBlockHighlightSnapshot.CodeHighlighter.ApplyHighlight(highlightCodeContext);
        }

        static ReadOnlySpan<char> TrimUrl(ReadOnlySpan<char> urlText)
        {
            int length = urlText.Length;
            while (length > 0 && IsTrailingUrlPunctuation(urlText[length - 1]))
            {
                length--;
            }

            return urlText[..length];
        }

        static bool IsTrailingUrlPunctuation(char ch)
        {
            return ch is '.' or ',' or ';' or ':' or '!' or '?' or ')' or ']' or '}' or '>' or '"' or '\''
                or '。' or '，' or '；' or '：' or '！' or '？' or '）' or '】' or '}' or '、';
        }

        static bool TryGetCodeLanguageSpan(string codeText, SourceSpan firstLine, out SourceSpan codeLanguageSpan)
        {
            if (firstLine.Length <= 0)
            {
                codeLanguageSpan = default;
                return false;
            }

            var firstLineText = codeText.AsSpan(firstLine.Start, firstLine.Length);
            var position = 0;
            while (position < firstLineText.Length && (firstLineText[position] == ' ' || firstLineText[position] == '\t'))
            {
                position++;
            }

            if (position >= firstLineText.Length)
            {
                codeLanguageSpan = default;
                return false;
            }

            var fenceChar = firstLineText[position];
            while (position < firstLineText.Length && firstLineText[position] == fenceChar)
            {
                position++;
            }

            while (position < firstLineText.Length && char.IsWhiteSpace(firstLineText[position]))
            {
                position++;
            }

            var codeLanguageStart = position;
            var codeLanguageEnd = firstLineText.Length;
            while (codeLanguageEnd > codeLanguageStart && char.IsWhiteSpace(firstLineText[codeLanguageEnd - 1]))
            {
                codeLanguageEnd--;
            }

            if (codeLanguageStart >= codeLanguageEnd)
            {
                codeLanguageSpan = default;
                return false;
            }

            codeLanguageSpan = new SourceSpan(firstLine.Start + codeLanguageStart, firstLine.Start + codeLanguageEnd - 1);
            return true;
        }

        static bool TryCreateGapSourceSpan(int start, int end, out SourceSpan sourceSpan)
        {
            if (start <= end)
            {
                sourceSpan = new SourceSpan(start, end);
                return true;
            }

            sourceSpan = default;
            return false;
        }

        CodeBlockHighlightSnapshot? TryCreateCodeBlockHighlightSnapshot(SourceSpan innerCodeSpan, string codeLangText, string innerCodeText)
        {
            if (TryCreateCodeHighlighter(codeLangText) is not { } codeHighlighter)
            {
                return null;
            }

            return new CodeBlockHighlightSnapshot(innerCodeSpan, codeLangText, innerCodeText, codeHighlighter);
        }

        ICodeHighlighter? TryCreateCodeHighlighter(string codeLangText)
        {
            if (string.IsNullOrWhiteSpace(codeLangText))
            {
                return null;
            }

            if (IsCsharpCodeLanguage(codeLangText))
            {
                return _csharpCodeHighlighter;
            }

            if (TryGetOtherLanguageId(codeLangText) is not { } languageId)
            {
                return null;
            }

            return new ColorCodeCodeHighlighter
            {
                LanguageId = languageId
            };
        }

        static string? TryGetOtherLanguageId(string codeLangText)
        {
            return codeLangText.Trim().ToLowerInvariant() switch
            {
                "asax" => ColorCode.Common.LanguageId.Asax,
                "ashx" => ColorCode.Common.LanguageId.Ashx,
                "aspx" => ColorCode.Common.LanguageId.Aspx,
                "aspx-cs" or "aspxcs" or "cshtml" => ColorCode.Common.LanguageId.AspxCs,
                "aspx-vb" or "aspxvb" or "vbhtml" => ColorCode.Common.LanguageId.AspxVb,
                "c" or "cpp" or "c++" or "cc" or "cxx" or "hpp" or "h" => ColorCode.Common.LanguageId.Cpp,
                "css" => ColorCode.Common.LanguageId.Css,
                "f#" or "fsharp" or "fs" => ColorCode.Common.LanguageId.FSharp,
                "fortran" or "f90" or "f95" => ColorCode.Common.LanguageId.Fortran,
                "haskell" or "hs" => ColorCode.Common.LanguageId.Haskell,
                "html" or "htm" => ColorCode.Common.LanguageId.Html,
                "java" => ColorCode.Common.LanguageId.Java,
                "javascript" or "js" or "node" => ColorCode.Common.LanguageId.JavaScript,
                "json" => "json",
                "koka" => ColorCode.Common.LanguageId.Koka,
                "markdown" or "md" => ColorCode.Common.LanguageId.Markdown,
                "matlab" => ColorCode.Common.LanguageId.MatLab,
                "php" => ColorCode.Common.LanguageId.Php,
                "powershell" or "pwsh" or "ps1" => ColorCode.Common.LanguageId.PowerShell,
                "python" or "py" => ColorCode.Common.LanguageId.Python,
                "sql" => ColorCode.Common.LanguageId.Sql,
                "typescript" or "ts" or "tsx" => ColorCode.Common.LanguageId.TypeScript,
                "vb" or "vbnet" or "vb.net" => ColorCode.Common.LanguageId.VbDotNet,
                "xml" or "xaml" or "axaml" or "svg" => ColorCode.Common.LanguageId.Xml,
                _ => null
            };
        }

        static bool IsCsharpCodeLanguage(string codeLangText)
        {
            return codeLangText.Equals("csharp", StringComparison.OrdinalIgnoreCase)
                   || codeLangText.Equals("cs", StringComparison.OrdinalIgnoreCase)
                   || codeLangText.Equals("C#", StringComparison.OrdinalIgnoreCase)
                   || codeLangText.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        }

        static bool HighlightSegmentSnapshotEquals(HighlightSegmentSnapshot left, HighlightSegmentSnapshot right)
        {
            if (left.SourceSpan != right.SourceSpan)
            {
                return false;
            }

            if (left.OperationList.Count != right.OperationList.Count)
            {
                return false;
            }

            for (int i = 0; i < left.OperationList.Count; i++)
            {
                if (left.OperationList[i] != right.OperationList[i])
                {
                    return false;
                }
            }

            return left.CodeBlockHighlightSnapshot == right.CodeBlockHighlightSnapshot;
        }
    }

    /// <summary>
    /// 渲染 Markdown 代码块背景。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderBackground(in TextEditorDrawingContext context)
    {
        var viewport = context.Viewport;
        var drawingContext = context.DrawingContext;

        var renderInfoProvider = context.GetRenderInfo();
        TextRect? mergedCodeBlockBounds = null;
        ParagraphIndex? lastCodeParagraphIndex = null;

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            var paragraphBounds = paragraphRenderInfo.ParagraphLayoutData.TextContentBounds;
            var textParagraph = paragraphRenderInfo.Paragraph;
            bool isInCodeBlock = IsInCodeBlock(textParagraph);

            if (!isInCodeBlock)
            {
                DrawMergedCodeBlock();
                lastCodeParagraphIndex = null;
                continue;
            }

            if (lastCodeParagraphIndex is { } previousCodeParagraphIndex
                && paragraphRenderInfo.Index != previousCodeParagraphIndex + 1)
            {
                DrawMergedCodeBlock();
            }

            lastCodeParagraphIndex = paragraphRenderInfo.Index;

            if (viewport != null && !viewport.Value.IntersectsWith(paragraphBounds))
            {
                continue;
            }

            var outlineBounds = paragraphRenderInfo.ParagraphLayoutData.OutlineBounds;
            mergedCodeBlockBounds = mergedCodeBlockBounds is { } currentMergedBounds
                ? currentMergedBounds.Union(outlineBounds)
                : outlineBounds;
        }

        DrawMergedCodeBlock();

        void DrawMergedCodeBlock()
        {
            if (mergedCodeBlockBounds is not { } bounds)
            {
                return;
            }

            DrawCodeBlockBackground(drawingContext, bounds);
            mergedCodeBlockBounds = null;
        }
    }

    /// <summary>
    /// 渲染 Markdown 前景内容。
    /// </summary>
    /// <param name="context">绘制上下文。</param>
    public void RenderForeground(in TextEditorDrawingContext context)
    {
    }

    private RunProperty GetTitleLevelRunProperty(int level)
    {
        var levelIndex = level - 1;
        if (levelIndex < 0)
        {
            return _titleLevelRunPropertyList[0];
        }

        if (levelIndex >= _titleLevelRunPropertyList.Count)
        {
            return _titleLevelRunPropertyList[^1];
        }

        return _titleLevelRunPropertyList[levelIndex];
    }

    private bool IsInCodeBlock(ITextParagraph textParagraph)
    {
        int startOffset = textParagraph.GetParagraphStartOffset();
        var length = textParagraph.CharCount;
        var endOffset = startOffset + length;

        foreach (var sourceSpan in _codeBlockList)
        {
            var start = sourceSpan.Start;
            var end = sourceSpan.End + 1;

            if (startOffset < end && endOffset > start)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 表示 Markdown 中识别出的 URL 信息。
    /// </summary>
    /// <param name="SourceSpan">URL 在文档中的范围。</param>
    /// <param name="Url">URL 文本。</param>
    public readonly record struct MarkdownUrlInfo(SourceSpan SourceSpan, string Url);

    private readonly record struct HighlightOperation(RunProperty RunProperty, SourceSpan SourceSpan);

    private readonly record struct CodeBlockHighlightSnapshot(SourceSpan InnerCodeSpan, string CodeLang, string InnerCodeText, ICodeHighlighter CodeHighlighter);

    private readonly record struct HighlightSegmentSnapshot(
        SourceSpan SourceSpan,
        IReadOnlyList<HighlightOperation> OperationList,
        CodeBlockHighlightSnapshot? CodeBlockHighlightSnapshot);

    private static FontWeightValue GetBoldFontWeight()
#if USE_AVALONIA
        => SKFontStyleWeight.Bold;
#else
        => System.Windows.FontWeights.Bold;
#endif

#if USE_AVALONIA
    private static LightTextEditorPlus.Primitive.SolidColorSkiaTextBrush CreateCodeLanguageForeground()
        => new(new SKColor(0xFFAC90DE));

    private static LightTextEditorPlus.Primitive.SolidColorSkiaTextBrush CreateUrlForeground()
        => new(new SKColor(0xFF67D9E0));

    private static BackgroundBrush CreateCodeBackgroundBrush()
        => new(Avalonia.Media.Color.FromArgb(0xFF, 0x3B, 0x3C, 0x37));

    private static void DrawCodeBlockBackground(Avalonia.Media.DrawingContext drawingContext, TextRect bounds)
        => drawingContext.DrawRectangle(CreateCodeBackgroundBrush(), null,
            new Avalonia.Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));
#elif USE_WPF
    private static LightTextEditorPlus.Document.ImmutableBrush CreateCodeLanguageForeground()
        => CreateImmutableBrush(0xFF, 0xAC, 0x90, 0xDE);

    private static LightTextEditorPlus.Document.ImmutableBrush CreateUrlForeground()
        => CreateImmutableBrush(0xFF, 0x67, 0xD9, 0xE0);

    private static BackgroundBrush CreateCodeBackgroundBrush()
    {
        var brush = new BackgroundBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x3B, 0x3C, 0x37));
        brush.Freeze();
        return brush;
    }

    private static LightTextEditorPlus.Document.ImmutableBrush CreateImmutableBrush(byte alpha, byte red, byte green, byte blue)
    {
        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, red, green, blue));
        brush.Freeze();
        return new LightTextEditorPlus.Document.ImmutableBrush(brush);
    }

    private void DrawCodeBlockBackground(System.Windows.Media.DrawingContext drawingContext, TextRect bounds)
        => drawingContext.DrawRectangle(_codeBackgroundColorBrush, null,
            new System.Windows.Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));
#endif
}
