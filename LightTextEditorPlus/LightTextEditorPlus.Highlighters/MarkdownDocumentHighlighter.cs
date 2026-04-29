using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Primitive;
using LightTextEditorPlus.Highlighters.CodeHighlighters;

using Markdig;
using Markdig.Syntax;

using SkiaSharp;

using LightTextEditorPlus.Utils;
using Path = System.IO.Path;

namespace LightTextEditorPlus.Highlighters;

public sealed partial class MarkdownDocumentHighlighter : IDocumentHighlighter
{
    private static readonly Regex UrlRegex = new(@"https?://[^\s<>\u3000]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly TextEditor _textEditor;
    private readonly SkiaTextRunProperty _normalTextRunProperty;
    private readonly IReadOnlyList<SkiaTextRunProperty> _titleLevelRunPropertyList;
    private readonly SkiaTextRunProperty _codeLangInfoRunProperty;
    private readonly SkiaTextRunProperty _urlRunProperty;
    private readonly CsharpCodeHighlighter _csharpCodeHighlighter = new();
    private readonly SolidColorBrush _codeBackgroundColorBrush = new SolidColorBrush(0xFF3B3C37);
    private readonly List<SourceSpan> _codeBlockList = [];
    private readonly List<MarkdownUrlInfo> _urlInfoList = [];
    private IReadOnlyList<HighlightSegmentSnapshot> _lastHighlightSnapshotList = [];
    public IReadOnlyList<MarkdownUrlInfo> UrlInfoList => _urlInfoList;

    public MarkdownDocumentHighlighter(TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(textEditor);

        _textEditor = textEditor;

        double normalFontSize = textEditor.StyleRunProperty.FontSize;

        _normalTextRunProperty = textEditor.StyleRunProperty;

        var titleLevel1RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 10,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel2RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 7,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel3RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 5,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel4RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 3,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel5RunProperty = _textEditor.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 1,
            FontWeight = SKFontStyleWeight.Bold,
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
            Foreground = new LightTextEditorPlus.Primitive.SolidColorSkiaTextBrush(new SKColor(0xFFAC90DE))
        });

        _urlRunProperty = _textEditor.CreateRunProperty(property => property with
        {
            Foreground = new LightTextEditorPlus.Primitive.SolidColorSkiaTextBrush(new SKColor(0xFF67D9E0)),
            DecorationCollection = UnderlineTextEditorDecoration.Instance,
        });
    }

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
                var closingFencedCharCount = fencedCodeBlock.ClosingFencedCharCount;
                var langInfoLength = fencedCodeBlock.Info?.Length ?? 0;
                var operationList = new List<HighlightOperation>
                {
                    new HighlightOperation(_normalTextRunProperty, sourceSpan)
                };

                ReadOnlySpan<char> codeLang = [];
                if (langInfoLength > 0 && firstLine.Length == closingFencedCharCount + langInfoLength)
                {
                    var span = new SourceSpan(closingFencedCharCount, closingFencedCharCount + langInfoLength - 1);
                    operationList.Add(new HighlightOperation(_codeLangInfoRunProperty,
                        new SourceSpan(span.Start + sourceSpan.Start, span.End + sourceSpan.Start)));

                    codeLang = codeText.AsSpan(span.Start, span.Length);
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

                var relativeOffset = sourceSpan.Start;
                var innerCodeSpan = new SourceSpan(firstLine.End + 1 + relativeOffset, lastLine.Start + relativeOffset - 1);
                var innerCodeText = ToText(innerCodeSpan);
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
            catch (ArgumentOutOfRangeException e)
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

    public void RenderBackground(in AvaloniaTextEditorDrawingContext context)
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

            drawingContext.DrawRectangle(_codeBackgroundColorBrush, null, bounds.ToSKRect().ToAvaloniaRect());
            mergedCodeBlockBounds = null;
        }
    }

    public void RenderForeground(in AvaloniaTextEditorDrawingContext context)
    {
    }

    private SkiaTextRunProperty GetTitleLevelRunProperty(int level)
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

    public readonly record struct MarkdownUrlInfo(SourceSpan SourceSpan, string Url);

    private readonly record struct HighlightOperation(SkiaTextRunProperty RunProperty, SourceSpan SourceSpan);

    private readonly record struct CodeBlockHighlightSnapshot(SourceSpan InnerCodeSpan, string CodeLang, string InnerCodeText, ICodeHighlighter CodeHighlighter);

    private readonly record struct HighlightSegmentSnapshot(
        SourceSpan SourceSpan,
        IReadOnlyList<HighlightOperation> OperationList,
        CodeBlockHighlightSnapshot? CodeBlockHighlightSnapshot);
}
