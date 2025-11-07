using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Configurations;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Primitive;
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
        using var renderer = new Renderer(in renderArgument, TextEditor, this);
        return renderer.Render();
    }
}

/// <summary>
/// 渲染器
/// </summary>
/// 这个结构体仅仅只是为了减少一些内部方法而已，没有实际的逻辑作用
file struct Renderer : IDisposable
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
        Viewport = renderArgument.Viewport;

        Debug.Assert(!renderInfoProvider.IsDirty);

        HorizontalSkiaTextRenderer = horizontalSkiaTextRenderer;
    }

    public SkiaTextEditor TextEditor { get; }

    public SKCanvas Canvas { get; }

    public RenderInfoProvider RenderInfoProvider { get; }

    public TextRect RenderBounds { get; set; }

    /// <summary>
    /// 可见范围，为空则代表需要全文档渲染
    /// </summary>
    public TextRect? Viewport { get; }

    /// <summary>
    /// 是否在可见范围内
    /// </summary>
    /// <param name="textRect"></param>
    /// <returns></returns>
    private bool IsInViewport(in TextRect textRect)
    {
        if (Viewport is null) return true;
        return Viewport.Value.IntersectsWith(textRect);
    }

    public HorizontalSkiaTextRenderer HorizontalSkiaTextRenderer { get; }

    private SkiaTextEditorDebugConfiguration Config => TextEditor.DebugConfiguration;

    private ITextLogger Logger => TextEditor.TextEditorCore.Logger;

    public SkiaTextRenderResult Render()
    {
        foreach (ParagraphRenderInfo paragraphRenderInfo in RenderInfoProvider.GetParagraphRenderInfoList())
        {
            if (!IsInViewport(paragraphRenderInfo.ParagraphLayoutData.OutlineBounds))
            {
                // 不在可见范围内，跳过
                continue;
            }

            if (Config.IsInDebugMode)
            {
                IParagraphLayoutData paragraphLayoutData = paragraphRenderInfo.ParagraphLayoutData;

                DrawDebugBounds(paragraphLayoutData.TextContentBounds.ToSKRect(), Config.DebugDrawParagraphContentBoundsInfo);
                DrawDebugBounds(paragraphLayoutData.OutlineBounds.ToSKRect(), Config.DebugDrawParagraphOutlineBoundsInfo);
            }

            foreach (ParagraphLineRenderInfo lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                if (Viewport is {} viewport)
                {
                    if (!viewport.IntersectsWith(lineRenderInfo.OutlineBounds))
                    {
                        continue;
                    }
                }

                RenderTextLine(in lineRenderInfo);
            }
        }

        return new SkiaTextRenderResult()
        {
            RenderBounds = RenderBounds
        };
    }

    /// <summary>
    /// 渲染一行文本
    /// </summary>
    /// <param name="lineRenderInfo"></param>
    private void RenderTextLine(in ParagraphLineRenderInfo lineRenderInfo)
    {
        RenderBackground(in lineRenderInfo);

        if (lineRenderInfo.IsIncludeMarker)
        {
            // 如果包含了项目符号，那么就需要先绘制项目符号
            TextReadOnlyListSpan<CharData> markerCharDataList = lineRenderInfo.GetMarkerCharDataList();
            RenderCharList(in markerCharDataList, in lineRenderInfo);
        }

        // 标记是否包含文本装饰
        bool containsTextDecoration = false;

        // 先不考虑缓存
        LineDrawingArgument argument = lineRenderInfo.Argument;

        if (TextEditor.RenderConfiguration.UseRenderCharByCharMode)
        {
            // 逐字符渲染。渲染效率慢，但可以遵循布局结果
            for (var i = 0; i < argument.CharList.Count; i++)
            {
                TextReadOnlyListSpan<CharData> charList = argument.CharList.Slice(i, 1);
                if (charList[0].CharDataInfo.Status == CharDataInfoStatus.LigatureContinue)
                {
                    // 逐个渲染时，需要考虑连写字的情况，跳过连写字的中间部分
                    continue;
                }
                RenderCharList(in charList, in lineRenderInfo);

                containsTextDecoration |= ContainsTextDecoration(in charList);
            }
        }
        else
        {
            foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
            {
                RenderCharList(charList, lineRenderInfo);

                containsTextDecoration |= ContainsTextDecoration(in charList);
            }
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

        if (containsTextDecoration)
        {
            // 渲染文本装饰
            RenderTextDecoration(lineRenderInfo);
        }

        DrawDebugBounds(new TextRect(argument.StartPoint, argument.LineContentSize).ToSKRect(), Config.DebugDrawLineContentBoundsInfo);

        static bool ContainsTextDecoration(in TextReadOnlyListSpan<CharData> charList)
        {
            SkiaTextRunProperty skiaTextRunProperty = charList[0].RunProperty.AsSkiaRunProperty();
            return !skiaTextRunProperty.DecorationCollection.IsEmpty;
        }
    }

    /// <summary>
    /// 连续、相同字符属性的字符列表渲染方法
    /// </summary>
    /// <param name="charList"></param>
    /// <param name="lineInfo"></param>
    /// <exception cref="TextEditorInnerException"></exception>
    private void RenderCharList(in TextReadOnlyListSpan<CharData> charList, in ParagraphLineRenderInfo lineInfo)
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

        var marginLeft = 0f;
        if (TextEditor.RenderConfiguration.UseRenderCharByCharMode)
        {
            switch (TextEditor.RenderConfiguration.RenderFaceInFrameAlignment)
            {
                case SkiaTextEditorCharRenderFaceInFrameAlignment.Left:
                    marginLeft = 0;
                    break;
                case SkiaTextEditorCharRenderFaceInFrameAlignment.Center:
                    marginLeft = (float)
                        (firstCharData.CharDataInfo.FrameSize.Width - firstCharData.CharDataInfo.FaceSize.Width) / 2;
                    break;
                case SkiaTextEditorCharRenderFaceInFrameAlignment.Right:
                    marginLeft = (float)
                        (firstCharData.CharDataInfo.FrameSize.Width - firstCharData.CharDataInfo.FaceSize.Width);
                    break;
            }
        }

        x += marginLeft;

        foreach (CharData charData in charList)
        {
            DrawDebugBounds(charData.GetBounds().ToSKRect(), Config.DebugDrawCharBoundsInfo);

            width += (float) charData.Size.Width;
        }

        SKRect charSpanBounds = SKRect.Create(x, y, width, height);
        DrawDebugBounds(charSpanBounds, Config.DebugDrawCharSpanBoundsInfo);
        RenderBounds = RenderBounds.Union(charSpanBounds.ToTextRect());

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

        using SKTextBlob skTextBlob = ToSKTextBlob(in charList, skFont);
        //SKRect skTextBlobBounds = skTextBlob.Bounds;
        //_ = skTextBlobBounds;

        // 所有都完成之后，再来决定前景色。后续可以考虑相同的前景色统一起来，这样才能做比较大范围的渐变色。否则中间有一个字符稍微改了字号，就会打断渐变色
        SkiaTextBrush foreground = skiaTextRunProperty.Foreground;
        SkiaTextBrushRenderContext brushRenderContext = new SkiaTextBrushRenderContext()
        {
            Canvas = Canvas,
            Opacity = skiaTextRunProperty.Opacity,
            Paint = textRenderSKPaint,
            RenderBounds = charSpanBounds,
        };
        foreground.Apply(brushRenderContext);

        Canvas.DrawText(skTextBlob, x, y, textRenderSKPaint);
    }

    /// <summary>
    /// 渲染背景
    /// </summary>
    /// <param name="lineRenderInfo"></param>
    private void RenderBackground(in ParagraphLineRenderInfo lineRenderInfo)
    {
        
    }

    /// <summary>
    /// 绘制文本装饰层
    /// </summary>
    /// <param name="lineRenderInfo"></param>
    private void RenderTextDecoration(ParagraphLineRenderInfo lineRenderInfo)
    {
        LineDrawingArgument argument = lineRenderInfo.Argument;
        foreach (DecorationSplitResult decorationSplitResult in TextEditorDecorationHelper
                     .SplitContinuousTextDecorationCharData(argument.CharList))
        {
            TextEditorDecoration textEditorDecoration = decorationSplitResult.Decoration;
            SkiaTextRunProperty runProperty = decorationSplitResult.RunProperty;
            TextReadOnlyListSpan<CharData> charDataList = decorationSplitResult.CharList;

            var currentCharDataList = charDataList;
            var currentCharIndexInLine = decorationSplitResult.CurrentCharIndexInLine;

            while (true)
            {
                TextRect recommendedBounds = TextEditorDecoration
                    .GetDecorationLocationRecommendedBounds(textEditorDecoration.TextDecorationLocation, in currentCharDataList, in lineRenderInfo, TextEditor);

                var decorationArgument = new BuildDecorationArgument()
                {
                    CharDataList = currentCharDataList,
                    CurrentCharIndexInLine = currentCharIndexInLine,
                    RunProperty = runProperty,
                    LineRenderInfo = lineRenderInfo,
                    TextEditor = TextEditor,
                    RecommendedBounds = recommendedBounds,
                    Canvas = Canvas,
                    CachePaint = CachePaint,
                };
                BuildDecorationResult result = textEditorDecoration.BuildDecoration(in decorationArgument);

                if (result.TakeCharCount == currentCharDataList.Count)
                {
                    break;
                }
                else if (result.TakeCharCount < 1)
                {
                    var message = $"文本装饰渲染时，所需使用的字符数量至少要有一个。装饰器类型： {textEditorDecoration.GetType()}；TakeCharCount={result.TakeCharCount}";
                    if (Config.IsInDebugMode)
                    {
                        throw new TextEditorDebugException(message);
                    }
                    else
                    {
                        Logger.LogWarning(message);
                    }
                }
                else if (result.TakeCharCount > currentCharDataList.Count)
                {
                    var message = $"文本装饰渲染时，所需使用的字符数量不能超过传入的字符数量。装饰器类型： {textEditorDecoration.GetType()}；TakeCharCount={result.TakeCharCount}；CurrentCharDataListCount={currentCharDataList.Count}";
                    if (Config.IsInDebugMode)
                    {
                        throw new TextEditorDebugException(message);
                    }
                    else
                    {
                        Logger.LogWarning(message);
                    }
                }
                else
                {
                    currentCharDataList = currentCharDataList.Slice(result.TakeCharCount);
                    currentCharIndexInLine += result.TakeCharCount;
                }
            }
        }
    }

    private void DrawDebugBounds(SKRect bounds, TextEditorDebugBoundsDrawInfo? drawInfo)
    {
        HorizontalSkiaTextRenderer.DrawDebugBoundsInfo(Canvas, bounds, drawInfo);
    }

    private SKPaint CachePaint
    {
        get
        {
            if (_cachePaint is null)
            {
                _cachePaint = new SKPaint()
                {
                    IsAntialias = true
                };
            }

            return _cachePaint;
        }
    }

    private SKPaint? _cachePaint;

    private static SKTextBlob ToSKTextBlob(in TextReadOnlyListSpan<CharData> charList, SKFont skFont)
    {
        using TextPoolArrayContext<ushort> glyphIndexContext = charList.ToRenderGlyphIndexSpanContext();
        Span<ushort> glyphIndexSpan = glyphIndexContext.Span;
        Span<byte> glyphIndexByteSpan = MemoryMarshal.AsBytes(glyphIndexSpan);

        SKTextBlob skTextBlob = SKTextBlob.Create(glyphIndexByteSpan, SKTextEncoding.GlyphId, skFont);
        return skTextBlob;
    }

    public void Dispose()
    {
        _cachePaint?.Dispose();
    }
}

// 由于可能会在业务层访问，因此不开启
///// <summary>
///// 支持借用的 SKPaint 信息，只能支持有限量的更改
///// </summary>
//struct SKPaintRentInfo
//{
//    public SKPaintRentInfo(SKPaint paint)
//    {
//        Paint = paint;
//        paint.
//    }

//    public SKPaint Paint { get; }
//}