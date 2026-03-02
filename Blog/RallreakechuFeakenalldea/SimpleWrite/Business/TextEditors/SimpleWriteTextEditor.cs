using Avalonia.Controls;
using Avalonia.Media;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Primitive;

using Markdig;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Syntax;

using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Business.Snippets;
using SimpleWrite.Utils;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Skia;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using static Markdig.Syntax.CodeBlock;

namespace SimpleWrite.Business.TextEditors;

/// <summary>
/// 文本编辑器
/// </summary>
internal sealed class SimpleWriteTextEditor : TextEditor
{
    public SimpleWriteTextEditor()
    {
        CaretConfiguration.SelectionBrush = new Color(0x9F, 0x26, 0x3F, 0xC7);

        TextEditorCore.TextChanged += TextEditorCore_TextChanged;

        SizeToContent = SizeToContent.Height;

        var normalFontSize = 20;

        SetStyleTextRunProperty(runProperty => runProperty with
        {
            FontSize = normalFontSize,
            Foreground = new SolidColorSkiaTextBrush(SKColors.Azure)
        });

        var normalTextRunProperty = this.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize
        });
        NormalTextRunProperty = normalTextRunProperty;

        var titleLevel1RunProperty = this.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 10,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel2RunProperty = this.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 7,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel3RunProperty = this.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 5,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel4RunProperty = this.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 3,
            FontWeight = SKFontStyleWeight.Bold,
        });

        var titleLevel5RunProperty = this.CreateRunProperty(property => property with
        {
            FontSize = normalFontSize + 1,
            FontWeight = SKFontStyleWeight.Bold,
        });

        TitleLevelRunPropertyList = [titleLevel1RunProperty, titleLevel2RunProperty, titleLevel3RunProperty, titleLevel4RunProperty, titleLevel5RunProperty];

        CodeLangInfoRunProperty = CreateColorRunProperty(new SKColor(0xFFAC90DE));

        SkiaTextRunProperty CreateColorRunProperty(SKColor color)
        {
            return CreateRunProperty(property => property with
            {
                Foreground = new SolidColorSkiaTextBrush(color)
            });
        }
    }

    private void TextEditorCore_TextChanged(object? sender, EventArgs e)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var markdownText = Text;
        var markdownDocument = Markdown.Parse(markdownText, pipeline);
        var setter = new TextRunPropertySetter(this);

        // 先全部刷成正常文本，但会影响性能
        var textEditor = this;
        textEditor.SetRunProperty(NormalTextRunProperty, textEditor.GetAllDocumentSelection());

        CodeBlockList.Clear();

        foreach (Block block in markdownDocument)
        {
            if (block is ParagraphBlock paragraphBlock)
            {
                if (paragraphBlock.Inline is { } inline)
                {

                }

                setter.TrySetRunProperty(NormalTextRunProperty, paragraphBlock.Span);
            }

            if (block is HeadingBlock headingBlock)
            {
                var levelIndex = headingBlock.Level - 1;
                SkiaTextRunProperty runProperty;
                if (TitleLevelRunPropertyList.Count - 1 > levelIndex)
                {
                    runProperty = TitleLevelRunPropertyList[levelIndex];
                }
                else
                {
                    runProperty = TitleLevelRunPropertyList[^1];
                }

                setter.TrySetRunProperty(runProperty, headingBlock.Span);
            }

            if (block is FencedCodeBlock fencedCodeBlock)
            {
                SourceSpan sourceSpan = fencedCodeBlock.Span;
                CodeBlockList.Add(sourceSpan);

                //setter.SetRunProperty(property =>
                //    property with
                //    {
                //        Background = CodeBackgroundColor,
                //    }, sourceSpan);

                var codeText = ToText(sourceSpan);

                var codeSetter = setter with
                {
                    StartOffset = sourceSpan.Start
                };

                //var stringReader = new StringReader(codeText);
                //stringReader.ReadLine()
                //var codeLineArray = codeText.Split('\n');

                var fencedChar = fencedCodeBlock.FencedChar;
                var closingFencedCharCount = fencedCodeBlock.ClosingFencedCharCount;
                var langInfo = fencedCodeBlock.Info;

                var lineReader = new Markdig.Helpers.LineReader(codeText);
                StringSlice firstLine = lineReader.ReadLine();
                if (firstLine.Length == closingFencedCharCount + (langInfo?.Length ?? 0))
                {
                    codeSetter.TrySetRunProperty(CodeLangInfoRunProperty, new SourceSpan(closingFencedCharCount, closingFencedCharCount + langInfo?.Length ?? 0));
                }

                // 准备给代码内容着色
                // 除了最后一行和开始行之外，其他的部分就是代码部分了
                var lastLine = firstLine;
                var codeLine = firstLine;
                while (true)
                {
                    var currentLine = lineReader.ReadLine();
                    if (string.IsNullOrEmpty(currentLine.Text))
                    {
                        break;
                    }

                    if (codeLine.Start == firstLine.Start)
                    {
                        codeLine = currentLine;
                    }

                    lastLine = currentLine;
                }

                var innerCodeText = codeText.AsSpan().Slice(codeLine.Start, lastLine.Start- codeLine.Start).ToString();
            }
        }

        string ToText(SourceSpan span)
        {
            var text = markdownText.AsSpan().Slice(span.Start, span.Length).ToString();
            return text;
        }
    }

    private List<SourceSpan> CodeBlockList { get; } = [];

    /// <summary>
    /// 快捷键执行器
    /// </summary>
    public required ShortcutExecutor ShortcutExecutor { get; init; }

    /// <summary>
    /// 代码片管理器
    /// </summary>
    public required SnippetManager SnippetManager { get; init; }

    /// <summary>
    /// 正文文本属性
    /// </summary>
    public SkiaTextRunProperty NormalTextRunProperty { get; }
    public IReadOnlyList<SkiaTextRunProperty> TitleLevelRunPropertyList { get; }
    public SkiaTextRunProperty CodeLangInfoRunProperty { get; }
    public SKColor CodeBackgroundColor { get; } = new SKColor(0xFF3B3C37);
    public SolidColorBrush CodeBackgroundColorBrush { get; } = new SolidColorBrush(0xFF3B3C37);

    protected override TextEditorHandler CreateTextEditorHandler()
    {
        return new SimpleWriteTextEditorHandler(this);
    }

    protected override void Render(in AvaloniaTextEditorDrawingContext context)
    {
        var viewport = context.Viewport;
        var drawingContext = context.DrawingContext;

        var renderInfoProvider = context.GetRenderInfo();
        TextRect? mergedCodeBlockBounds = null;

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            var paragraphBounds = paragraphRenderInfo.ParagraphLayoutData.TextContentBounds;

            if (viewport != null)
            {
                if (!viewport.Value.IntersectsWith(paragraphBounds))
                {
                    // 不在可见范围内，忽略
                    continue;
                }
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

            drawingContext.DrawRectangle(CodeBackgroundColorBrush, null, bounds.ToSKRect().ToAvaloniaRect());
            mergedCodeBlockBounds = null;
        }

        base.Render(in context);

        bool IsInCodeBlock(ITextParagraph textParagraph)
        {
            int startOffset = textParagraph.GetParagraphStartOffset();
            var length = textParagraph.CharCount;
            var endOffset = startOffset + length;

            foreach (var sourceSpan in CodeBlockList)
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
    }
}