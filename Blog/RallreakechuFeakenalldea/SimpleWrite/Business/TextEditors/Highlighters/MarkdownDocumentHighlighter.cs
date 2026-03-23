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

using Markdig;
using Markdig.Syntax;

using SkiaSharp;

using LightTextEditorPlus.Utils;
using SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;
using Path = System.IO.Path;

namespace SimpleWrite.Business.TextEditors.Highlighters;

internal sealed partial class MarkdownDocumentHighlighter : IDocumentHighlighter
{
    [GeneratedRegex(@"https?://[^\s<>\u3000]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GetUrlRegex();


    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly SimpleWriteTextEditor _textEditor;
    private readonly SkiaTextRunProperty _normalTextRunProperty;
    private readonly IReadOnlyList<SkiaTextRunProperty> _titleLevelRunPropertyList;
    private readonly SkiaTextRunProperty _codeLangInfoRunProperty;
    private readonly SkiaTextRunProperty _urlRunProperty;
    private readonly SolidColorBrush _codeBackgroundColorBrush = new SolidColorBrush(0xFF3B3C37);
    private readonly List<SourceSpan> _codeBlockList = [];
    private readonly List<MarkdownUrlInfo> _urlInfoList = [];
    public IReadOnlyList<MarkdownUrlInfo> UrlInfoList => _urlInfoList;

    public MarkdownDocumentHighlighter(SimpleWriteTextEditor textEditor)
    {
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
            Foreground = new LightTextEditorPlus.Primitive.SolidColorSkiaTextBrush(new SKColor(0xFF1A73E8)),
            DecorationCollection = UnderlineTextEditorDecoration.Instance,
        });
    }

    public void ApplyHighlight(string markdownText)
    {
        var setter = new TextRunPropertySetter(_textEditor);

        setter.TrySetRunProperty(_normalTextRunProperty, _textEditor.TextEditorCore.GetAllDocumentSelection());

        var markdownDocument = Markdown.Parse(markdownText, MarkdownPipeline);
        _codeBlockList.Clear();
        _urlInfoList.Clear();

        foreach (var block in markdownDocument)
        {
            if (block is ParagraphBlock paragraphBlock)
            {
                setter.TrySetRunProperty(_normalTextRunProperty, paragraphBlock.Span);
                ApplyUrlHighlight(paragraphBlock.Span);
                continue;
            }

            if (block is HeadingBlock headingBlock)
            {
                setter.TrySetRunProperty(GetTitleLevelRunProperty(headingBlock.Level), headingBlock.Span);
                continue;
            }

            if (block is FencedCodeBlock fencedCodeBlock)
            {
                var sourceSpan = fencedCodeBlock.Span;
                _codeBlockList.Add(sourceSpan);

                string codeText = ToText(sourceSpan);
                var codeSetter = setter with
                {
                    StartOffset = sourceSpan.Start
                };

                var lineReader = new LineReader(codeText);
                SourceSpan firstLine = lineReader.ReadLine();
                var closingFencedCharCount = fencedCodeBlock.ClosingFencedCharCount;
                var langInfoLength = fencedCodeBlock.Info?.Length ?? 0;

                ReadOnlySpan<char> codeLang = [];
                if (langInfoLength > 0 && firstLine.Length == closingFencedCharCount + langInfoLength)
                {
                    var span = new SourceSpan(closingFencedCharCount, closingFencedCharCount + langInfoLength - 1);
                    codeSetter.TrySetRunProperty(_codeLangInfoRunProperty, span);

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

                if (codeLang.Equals("csharp", StringComparison.OrdinalIgnoreCase)
                    || codeLang.Equals("C#", StringComparison.OrdinalIgnoreCase))
                {
                    var csharpCodeHighlighter = new CsharpCodeHighlighter();

                    var colorCode = new TextEditorColorCode(_textEditor, new DocumentOffset(innerCodeSpan.Start));
                    var highlightCodeContext = new HighlightCodeContext(innerCodeText, colorCode);
                    csharpCodeHighlighter.ApplyHighlight(highlightCodeContext);
                }
            }
        }

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

        void ApplyUrlHighlight(SourceSpan sourceSpan)
        {
            string text = ToText(sourceSpan);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (Match match in GetUrlRegex().Matches(text))
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

                setter.TrySetRunProperty(_urlRunProperty, urlSourceSpan);
                _urlInfoList.Add(new MarkdownUrlInfo(urlSourceSpan, urlText.ToString()));
            }
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
    }

    public void RenderBackground(in AvaloniaTextEditorDrawingContext context)
    {
        var viewport = context.Viewport;
        var drawingContext = context.DrawingContext;

        var renderInfoProvider = context.GetRenderInfo();
        TextRect? mergedCodeBlockBounds = null;

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            var paragraphBounds = paragraphRenderInfo.ParagraphLayoutData.TextContentBounds;

            if (viewport != null && !viewport.Value.IntersectsWith(paragraphBounds))
            {
                continue;
            }

            var textParagraph = paragraphRenderInfo.Paragraph;
            if (IsInCodeBlock(textParagraph))
            {
                var outlineBounds = paragraphRenderInfo.ParagraphLayoutData.OutlineBounds;
                mergedCodeBlockBounds = mergedCodeBlockBounds is { } currentMergedBounds
                    ? currentMergedBounds.Union(outlineBounds)
                    : outlineBounds;
            }
            else
            {
                DrawMergedCodeBlock();
            }
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
}
