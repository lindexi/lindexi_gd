using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Utils;
using SkiaSharp;

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

    protected abstract void RenderTextLine(in ParagraphLineRenderInfo lineRenderInfo);

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
}