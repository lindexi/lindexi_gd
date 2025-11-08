using HarfBuzzSharp;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Utils;

using SkiaSharp;

using System;

namespace LightTextEditorPlus.Rendering.Core;

abstract class BaseSkiaTextRenderer : IDisposable
{
    protected BaseSkiaTextRenderer(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    protected SkiaTextEditorDebugConfiguration Config => TextEditor.DebugConfiguration;

    protected SkiaTextEditor TextEditor { get; }

    public abstract SkiaTextRenderResult Render(in SkiaTextRenderArgument argument);

    internal SkiaTextRenderResult RenderInner(in SkiaTextRenderArgument argument)
    {
        SKCanvas canvas = argument.Canvas;

        foreach (ParagraphRenderInfo paragraphRenderInfo in argument.RenderInfoProvider.GetParagraphRenderInfoList())
        {
            if (!IsInViewport(in argument, paragraphRenderInfo.ParagraphLayoutData.OutlineBounds))
            {
                // 不在可见范围内，跳过
                continue;
            }

            if (Config.IsInDebugMode)
            {
                IParagraphLayoutData paragraphLayoutData = paragraphRenderInfo.ParagraphLayoutData;

                DrawDebugBoundsInfo(canvas, paragraphLayoutData.TextContentBounds.ToSKRect(), Config.DebugDrawParagraphContentBoundsInfo);
                DrawDebugBoundsInfo(canvas, paragraphLayoutData.OutlineBounds.ToSKRect(), Config.DebugDrawParagraphOutlineBoundsInfo);
            }

            // 段落内逐行渲染
            foreach (ParagraphLineRenderInfo lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                if (argument.Viewport is { } viewport)
                {

                }

            }
        }
    }

    /// <summary>
    /// 是否在可见范围内
    /// </summary>
    /// <returns></returns>
    private bool IsInViewport(in SkiaTextRenderArgument argument, in TextRect textRect)
    {
        if (argument.Viewport is null) return true;
        return argument.Viewport.Value.IntersectsWith(textRect);
    }

    private SKPaint? _debugSKPaint;

    /// <summary>
    /// 绘制四线三格的调试信息
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="charSpanBounds"></param>
    /// <param name="charHandwritingPaperInfo"></param>
    public void DrawDebugHandwritingPaper(SKCanvas canvas, SKRect charSpanBounds, in CharHandwritingPaperInfo charHandwritingPaperInfo)
    {
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

    public void DrawDebugBoundsInfo(SKCanvas canvas, SKRect bounds, TextEditorDebugBoundsDrawInfo? drawInfo)
    {
        if (!Config.IsInDebugMode || drawInfo is null)
        {
            return;
        }

        if (drawInfo.StrokeColor is { } strokeColor && drawInfo.StrokeThickness > 0)
        {
            SKPaint debugPaint = GetDebugPaint(strokeColor);
            debugPaint.StrokeWidth = drawInfo.StrokeThickness;
            canvas.DrawRect(bounds, debugPaint);
        }

        if (drawInfo.FillColor is { } fillColor)
        {
            SKPaint debugPaint = GetDebugPaint(fillColor);
            debugPaint.Style = SKPaintStyle.Fill;
            canvas.DrawRect(bounds, debugPaint);
        }
    }

    protected SKPaint GetDebugPaint(SKColor color)
    {
        _debugSKPaint ??= new SKPaint();

        _debugSKPaint.Style = SKPaintStyle.Stroke;
        _debugSKPaint.StrokeWidth = 1;
        _debugSKPaint.Color = color;
        return _debugSKPaint;
    }

    public void Dispose()
    {
        _debugSKPaint?.Dispose();
    }
}