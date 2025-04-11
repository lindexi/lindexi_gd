using System;

using LightTextEditorPlus.Diagnostics;

using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

abstract class BaseSkiaTextRender : IDisposable
{
    protected BaseSkiaTextRender(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public void UpdateDebugColor()
    {
        SkiaTextEditorDebugConfiguration debugConfiguration = TextEditor.DebugConfiguration;

        if (debugConfiguration.IsInDebugMode)
        {
            DebugDrawCharBoundsColor = debugConfiguration.DebugDrawCharBoundsInfo;
            DebugDrawCharSpanBoundsColor = debugConfiguration.DebugDrawCharSpanBoundsInfo;
            DebugDrawLineContentBoundsColor = debugConfiguration.DebugDrawLineContentBoundsInfo;
            DebugDrawLineOutlineBoundsInfo = debugConfiguration.DebugDrawLineOutlineBoundsInfo;
            DebugDrawParagraphContentBoundsInfo = debugConfiguration.DebugDrawParagraphContentBoundsInfo;
            DebugDrawParagraphOutlineBoundsInfo = debugConfiguration.DebugDrawParagraphOutlineBoundsInfo;
            DebugDrawDocumentContentBoundsInfo = debugConfiguration.DebugDrawDocumentContentBoundsInfo;
            DebugDrawDocumentOutlineBoundsInfo = debugConfiguration.DebugDrawDocumentOutlineBoundsInfo;
        }
    }

    protected TextEditorDebugBoundsDrawInfo? DebugDrawCharBoundsColor { get; private set; }
    protected TextEditorDebugBoundsDrawInfo? DebugDrawCharSpanBoundsColor { get; private set; }
    protected TextEditorDebugBoundsDrawInfo? DebugDrawLineContentBoundsColor { get; private set; }
    protected TextEditorDebugBoundsDrawInfo? DebugDrawLineOutlineBoundsInfo { get; private set; }
    protected TextEditorDebugBoundsDrawInfo? DebugDrawParagraphContentBoundsInfo { get; private set; }
    protected TextEditorDebugBoundsDrawInfo? DebugDrawParagraphOutlineBoundsInfo { get; private set; }
    protected TextEditorDebugBoundsDrawInfo? DebugDrawDocumentContentBoundsInfo { get; private set; }
    protected TextEditorDebugBoundsDrawInfo? DebugDrawDocumentOutlineBoundsInfo { get; private set; }

    protected SkiaTextEditor TextEditor { get; }

    public abstract SkiaTextRenderResult Render(in SkiaTextRenderArgument argument);

    private SKPaint? _debugSKPaint;

    /// <summary>
    /// 绘制四线三格的调试信息
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="charSpanBounds"></param>
    /// <param name="charHandwritingPaperInfo"></param>
    protected void DrawDebugHandwritingPaper(SKCanvas canvas, SKRect charSpanBounds, in CharHandwritingPaperInfo charHandwritingPaperInfo)
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