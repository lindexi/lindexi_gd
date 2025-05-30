﻿using System;

using LightTextEditorPlus.Diagnostics;

using SkiaSharp;

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