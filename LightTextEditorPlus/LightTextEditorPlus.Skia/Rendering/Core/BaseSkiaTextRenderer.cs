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

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Document.Decorations;

namespace LightTextEditorPlus.Rendering.Core;

/// <summary>
/// 文本渲染器
/// </summary>
/// 原本是采用 file struct Renderer 表示的，但是为了统一横排和竖排，就需要有继承关系。结构体没法继承，所以改成类了。每次渲染都需要重新创建浪费一个对象，好在这个是一个小对象，用完就丢，影响很小
abstract class BaseSkiaTextRenderer : IDisposable
{
    protected BaseSkiaTextRenderer(RenderManager renderManager, in SkiaTextRenderArgument renderArgument)
    {
        TextEditor = renderManager.TextEditor;
        SKCanvas canvas = renderArgument.Canvas;
        Canvas = canvas;

        RenderInfoProvider renderInfoProvider = renderArgument.RenderInfoProvider;
        TextRect renderBounds = renderArgument.RenderBounds;
        RenderInfoProvider = renderInfoProvider;
        RenderBounds = renderBounds;
        Viewport = renderArgument.Viewport;

        Debug.Assert(!renderInfoProvider.IsDirty);
    }

    protected SkiaTextEditor TextEditor { get; }

    protected SkiaTextEditorDebugConfiguration Config => TextEditor.DebugConfiguration;
    protected ITextLogger Logger => TextEditor.TextEditorCore.Logger;

    protected SKCanvas Canvas { get; }

    protected RenderInfoProvider RenderInfoProvider { get; }

    protected TextRect RenderBounds { get; set; }

    /// <summary>
    /// 可见范围，为空则代表需要全文档渲染
    /// </summary>
    protected TextRect? Viewport { get; }

    protected ArrangingType ArrangingType => TextEditor.TextEditorCore.ArrangingType;

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

                DrawDebugBoundsInfo(paragraphLayoutData.TextContentBounds.ToSKRect(), Config.DebugDrawParagraphContentBoundsInfo);
                DrawDebugBoundsInfo(paragraphLayoutData.OutlineBounds.ToSKRect(), Config.DebugDrawParagraphOutlineBoundsInfo);
            }

            // 段落内逐行渲染
            foreach (ParagraphLineRenderInfo lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                if (Viewport is { } viewport)
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
                DrawDebugHandwritingPaper(new TextRect(argument.StartPoint, argument.LineContentSize with
                {
                    // 空行是 0 宽度，需要将其设置为整个文本的宽度才好计算
                    Width = RenderInfoProvider.TextEditor.DocumentManager.DocumentWidth,
                }).ToSKRect(), charHandwritingPaperInfo);
            }
        }

        if (containsTextDecoration)
        {
            // 渲染文本装饰
            RenderTextDecoration(in lineRenderInfo);
        }

        DrawDebugBoundsInfo(new TextRect(argument.StartPoint, argument.LineContentSize).ToSKRect(), Config.DebugDrawLineContentBoundsInfo);

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
    protected abstract void RenderCharList
        (in TextReadOnlyListSpan<CharData> charList, in ParagraphLineRenderInfo lineInfo);

    /// <summary>
    /// 渲染背景
    /// </summary>
    /// <param name="lineRenderInfo"></param>
    protected abstract void RenderBackground(in ParagraphLineRenderInfo lineRenderInfo);

    /// <summary>
    /// 绘制文本装饰层
    /// </summary>
    /// <param name="lineRenderInfo"></param>
    private void RenderTextDecoration(in ParagraphLineRenderInfo lineRenderInfo)
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

    public void DrawDebugBoundsInfo(TextRect bounds, TextEditorDebugBoundsDrawInfo? drawInfo)
    {
        if (!Config.IsInDebugMode || drawInfo is null)
        {
            return;
        }

        DrawDebugBoundsInfo(bounds.ToSKRect(), drawInfo);
    }

    public void DrawDebugBoundsInfo(SKRect bounds, TextEditorDebugBoundsDrawInfo? drawInfo)
    {
        if (!Config.IsInDebugMode || drawInfo is null)
        {
            return;
        }

        if (drawInfo.StrokeColor is { } strokeColor && drawInfo.StrokeThickness > 0)
        {
            SKPaint debugPaint = GetDebugPaint(strokeColor);
            debugPaint.StrokeWidth = drawInfo.StrokeThickness;
            Canvas.DrawRect(bounds, debugPaint);
        }

        if (drawInfo.FillColor is { } fillColor)
        {
            SKPaint debugPaint = GetDebugPaint(fillColor);
            debugPaint.Style = SKPaintStyle.Fill;
            Canvas.DrawRect(bounds, debugPaint);
        }
    }

    /// <summary>
    /// 绘制四线三格的调试信息
    /// </summary>
    /// <param name="charSpanBounds"></param>
    /// <param name="charHandwritingPaperInfo"></param>
    public void DrawDebugHandwritingPaper(SKRect charSpanBounds, in CharHandwritingPaperInfo charHandwritingPaperInfo)
    {
        SKCanvas canvas = Canvas;

        HandwritingPaperDebugDrawInfo drawInfo = TextEditor.DebugConfiguration.HandwritingPaperDebugDrawInfo;
        DrawLine(charHandwritingPaperInfo.TopLineGradation, drawInfo.TopLineGradationDebugDrawInfo);
        DrawLine(charHandwritingPaperInfo.MiddleLineGradation, drawInfo.MiddleLineGradationDebugDrawInfo);
        DrawLine(charHandwritingPaperInfo.BaselineGradation, drawInfo.BaselineGradationDebugDrawInfo);
        DrawLine(charHandwritingPaperInfo.BottomLineGradation, drawInfo.BottomLineGradationDebugDrawInfo);

        void DrawLine(double gradation, HandwritingPaperGradationDebugDrawInfo info)
        {
            SKPaint debugPaint = GetDebugPaint(info.DebugColor);
            debugPaint.StrokeWidth = info.StrokeThickness;

            float y = (float) gradation;
            float x = charSpanBounds.Left;
            float width = charSpanBounds.Width;
            canvas.DrawLine(x, y, x + width, y, debugPaint);
        }
    }

    private SKPaint? _debugSKPaint;

    protected SKPaint GetDebugPaint(SKColor color)
    {
        _debugSKPaint ??= new SKPaint();

        _debugSKPaint.Style = SKPaintStyle.Stroke;
        _debugSKPaint.StrokeWidth = 1;
        _debugSKPaint.Color = color;
        return _debugSKPaint;
    }

    protected SKPaint CachePaint
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

    public void Dispose()
    {
        _debugSKPaint?.Dispose();
        _cachePaint?.Dispose();
    }

    protected static SKTextBlob ToSKTextBlob(in TextReadOnlyListSpan<CharData> charList, SKFont skFont)
    {
        using TextPoolArrayContext<ushort> glyphIndexContext = charList.ToRenderGlyphIndexSpanContext();
        Span<ushort> glyphIndexSpan = glyphIndexContext.Span;
        Span<byte> glyphIndexByteSpan = MemoryMarshal.AsBytes(glyphIndexSpan);

        skFont.Hinting = SKFontHinting.None;
        SKTextBlob skTextBlob = SKTextBlob.Create(glyphIndexByteSpan, SKTextEncoding.GlyphId, skFont);
        return skTextBlob;
    }

    protected static bool CheckSameBackground(CharData a, CharData b)
    {
        SkiaTextRunProperty aRunProperty = a.RunProperty.AsSkiaRunProperty();
        SkiaTextRunProperty bRunProperty = b.RunProperty.AsSkiaRunProperty();
        return aRunProperty.Background == bRunProperty.Background;
    }
}