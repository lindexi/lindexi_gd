using System;
using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;

using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

/// <summary>
/// 水平横排的文本渲染器
/// </summary>
class HorizontalSkiaTextRenderer : BaseSkiaTextRenderer
{
    public HorizontalSkiaTextRenderer(RenderManager renderManager) : base(renderManager.TextEditor)
    {
        RenderManager = renderManager;
    }

    public RenderManager RenderManager { get; }

    public override SkiaTextRenderResult Render(in SkiaTextRenderArgument renderArgument)
    {
        var renderer = new Renderer(in renderArgument, TextEditor, this);
        return renderer.Render();
    }
}

/// <summary>
/// 渲染器
/// </summary>
/// 这个结构体仅仅只是为了减少一些内部方法而已，没有实际的逻辑作用
file struct Renderer
{
    public Renderer(in SkiaTextRenderArgument renderArgument, SkiaTextEditor textEditor, HorizontalSkiaTextRenderer horizontalSkiaTextRenderer)
    {
        TextEditor = textEditor;
        SKCanvas canvas = renderArgument.Canvas;
        Canvas = canvas;

        RenderInfoProvider renderInfoProvider = renderArgument.RenderInfoProvider;
        TextRect renderBounds = renderArgument.RenderBounds;
        RenderInfoProvider = renderInfoProvider;
        RenderBounds = renderBounds;

        Debug.Assert(!renderInfoProvider.IsDirty);

        HorizontalSkiaTextRenderer = horizontalSkiaTextRenderer;
    }

    public SkiaTextEditor TextEditor { get; }

    public SKCanvas Canvas { get; }

    public RenderInfoProvider RenderInfoProvider { get; }

    public TextRect RenderBounds { get; set; }
    public HorizontalSkiaTextRenderer HorizontalSkiaTextRenderer { get; }

    private SkiaTextEditorDebugConfiguration Config => TextEditor.DebugConfiguration;

    public SkiaTextRenderResult Render()
    {
        foreach (ParagraphRenderInfo paragraphRenderInfo in RenderInfoProvider.GetParagraphRenderInfoList())
        {
            foreach (ParagraphLineRenderInfo lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                DrawLine(lineRenderInfo);
            }
        }

        return new SkiaTextRenderResult()
        {
            RenderBounds = RenderBounds
        };
    }

    private void DrawLine(ParagraphLineRenderInfo lineRenderInfo)
    {
        if (lineRenderInfo.IsIncludeMarker)
        {
            TextReadOnlyListSpan<CharData> markerCharDataList = lineRenderInfo.GetMarkerCharDataList();
            RenderCharList(markerCharDataList, lineRenderInfo);
        }

        // 先不考虑缓存
        LineDrawingArgument argument = lineRenderInfo.Argument;

        foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
        {
            RenderCharList(charList, lineRenderInfo);
        }

        if (argument.CharList.Count == 0)
        {
            // 空行
            // 绘制四线三格的调试信息
            if (Config.ShowHandwritingPaperDebugInfo)
            {
                CharHandwritingPaperInfo charHandwritingPaperInfo =
                    RenderInfoProvider.GetHandwritingPaperInfo(in lineRenderInfo);
                HorizontalSkiaTextRenderer.DrawDebugHandwritingPaper(Canvas, new TextRect(argument.StartPoint, argument.LineContentSize with
                {
                    // 空行是 0 宽度，需要将其设置为整个文本的宽度才好计算
                    Width = RenderInfoProvider.TextEditor.DocumentManager.DocumentWidth,
                }).ToSKRect(), charHandwritingPaperInfo);
            }
        }

        // 渲染文本装饰
        RenderTextDecoration(lineRenderInfo);

        DrawDebugBounds(new TextRect(argument.StartPoint, argument.LineContentSize).ToSKRect(), Config.DebugDrawLineContentBoundsInfo);
    }

    private void RenderCharList(TextReadOnlyListSpan<CharData> charList, ParagraphLineRenderInfo lineInfo)
    {
        CharData firstCharData = charList[0];

        SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();

        // 不需要在这里处理字体回滚，在输入的过程中已经处理过了
        ////  考虑字体回滚问题
        RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(firstCharData.CharObject.CodePoint);

        SKFont skFont = renderingRunPropertyInfo.Font;

        SKPaint textRenderSKPaint = renderingRunPropertyInfo.Paint;

        var runBounds = firstCharData.GetBounds();
        var startPoint = runBounds.LeftTop;

        float x = (float) startPoint.X;
        float y = (float) startPoint.Y;
        float width = 0;
        float height = (float) runBounds.Height;

        using CharDataListToCharSpanResult charSpanResult = charList.ToCharSpan();
        ReadOnlySpan<char> charSpan = charSpanResult.CharSpan;

        foreach (CharData charData in charList)
        {
            DrawDebugBounds(charData.GetBounds().ToSKRect(), Config.DebugDrawCharBoundsInfo);

            width += (float) charData.Size.Width;
        }

        SKRect charSpanBounds = SKRect.Create(x, y, width, height);
        DrawDebugBounds(charSpanBounds, Config.DebugDrawCharSpanBoundsInfo);
        RenderBounds = RenderBounds.Union(charSpanBounds.ToTextRect());

        if (!skFont.ContainsGlyphs(charSpan))
        {
            // 预计不会出现这样的问题，在渲染之前已经处理过了
            throw new TextEditorInnerException($"文本框架内应该确保进入渲染层时，不会出现字体不能包含字符的情况");
        }

        // 绘制四线三格的调试信息
        if (Config.ShowHandwritingPaperDebugInfo)
        {
            CharHandwritingPaperInfo charHandwritingPaperInfo =
                RenderInfoProvider.GetHandwritingPaperInfo(in lineInfo, firstCharData);
            HorizontalSkiaTextRenderer.DrawDebugHandwritingPaper(Canvas, charSpanBounds, charHandwritingPaperInfo);
        }

        //float x = skiaTextRenderInfo.X;
        //float y = skiaTextRenderInfo.Y;

        var baselineY = /*skFont.Metrics.Leading +*/ (-skFont.Metrics.Ascent);

        // 由于 Skia 的 DrawText 传入的 Point 是文本的基线，因此需要调整 Y 值
        y += baselineY;
        using SKTextBlob skTextBlob = SKTextBlob.Create(charSpan, skFont);
        Canvas.DrawText(skTextBlob, x, y, textRenderSKPaint);
    }

    /// <summary>
    /// 绘制文本装饰层
    /// </summary>
    /// <param name="lineRenderInfo"></param>
    private void RenderTextDecoration(ParagraphLineRenderInfo lineRenderInfo)
    {

    }

    private void DrawDebugBounds(SKRect bounds, TextEditorDebugBoundsDrawInfo? drawInfo)
    {
        HorizontalSkiaTextRenderer.DrawDebugBoundsInfo(Canvas, bounds, drawInfo);
    }
}