using Avalonia.Media;

using LightTextEditorPlus;
using LightTextEditorPlus.Editing;

using Markdig;

using SimpleWrite.Business.ShortcutManagers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Primitive;
using Markdig.Renderers;
using Markdig.Syntax;
using SimpleWrite.Business.Snippets;
using SimpleWrite.Utils;
using SkiaSharp;

namespace SimpleWrite.Business.TextEditors;

/// <summary>
/// 文本编辑器
/// </summary>
internal sealed class SimpleWriteTextEditor : TextEditor
{
    public SimpleWriteTextEditor()
    {
        CaretConfiguration.SelectionBrush = new Color(0x9F, 0x26, 0x3F, 0xC7);

        TextEditorCore.DocumentChanged += TextEditorCore_DocumentChanged;

        var normalFontSize = 25;

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
    }

    private void TextEditorCore_DocumentChanged(object? sender, EventArgs e)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var markdownDocument = Markdown.Parse(this.Text, pipeline);
        foreach (Block block in markdownDocument)
        {
            if (block is ParagraphBlock paragraphBlock)
            {
                if (paragraphBlock.Inline is { } inline)
                {

                }

                TrySetRunProperty(NormalTextRunProperty, paragraphBlock.Span);
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

                TrySetRunProperty(runProperty, headingBlock.Span);
            }

        }

        void TrySetRunProperty(SkiaTextRunProperty runProperty, SourceSpan span)
        {
            var selection = SourceSpanToSelection(span);

            // 由于 GetRunPropertyRange 没有去重，导致不能用 OneOrDefault 直接处理
            //var currentRunProperty = this.GetRunPropertyRange(selection)

            //    // 为什么取 OneOrDefault 呢？ 这是因为如果能够拿到多个字符属性，则必定需要重新设置
            //    .OneOrDefault();

            //if (currentRunProperty != runProperty)
            //{
            //    SetRunProperty(runProperty,selection);
            //}
            TextEditorCore.SetUndoRedoEnable(false, "框架内部设置文本样式，防止将内容动作记录");
            IEnumerable<SkiaTextRunProperty> runPropertyRange = GetRunPropertyRange(selection);
            var same = runPropertyRange.All(t => t == runProperty);
            if (!same)
            {
                SetRunProperty(runProperty, selection);
            }
            TextEditorCore.SetUndoRedoEnable(true, "完成框架内部设置文本样式，启用撤销恢复");
        }
    }

    private Selection SourceSpanToSelection(SourceSpan span) => new Selection(new CaretOffset(span.Start), span.Length);

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

    protected override TextEditorHandler CreateTextEditorHandler()
    {
        return new SimpleWriteTextEditorHandler(this);
    }

    //class F : IMarkdownRenderer
    //{
    //    public object Render(MarkdownObject markdownObject)
    //    {
    //    }

    //    public ObjectRendererCollection ObjectRenderers { get; set; }
    //    public event Action<IMarkdownRenderer, MarkdownObject>? ObjectWriteBefore;
    //    public event Action<IMarkdownRenderer, MarkdownObject>? ObjectWriteAfter;
    //}
}
