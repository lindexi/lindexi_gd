using System;
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
    public HorizontalSkiaTextRenderer(RenderManager renderManager, in SkiaTextRenderArgument renderArgument) : base(renderManager, in renderArgument)
    {
    }

    /// <inheritdoc />
    protected override void RenderCharList(in TextReadOnlyListSpan<CharData> charList, in ParagraphLineRenderInfo lineInfo)
    {
        CharData firstCharData = charList[0];

        SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();

        // 不需要在这里处理字体回滚，在输入的过程中已经处理过了
        ////  考虑字体回滚问题
        RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(firstCharData.CharObject.CodePoint);

        SKFont skFont = renderingRunPropertyInfo.Font;

        SKPaint textRenderSKPaint = renderingRunPropertyInfo.Paint;
        textRenderSKPaint.IsAntialias = false;

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
            DrawDebugBoundsInfo(charData.GetBounds().ToSKRect(), Config.DebugDrawCharBoundsInfo);

            width += (float) charData.Size.Width;
        }

        SKRect charSpanBounds = SKRect.Create(x, y, width, height);
        DrawDebugBoundsInfo(charSpanBounds, Config.DebugDrawCharSpanBoundsInfo);
        RenderBounds = RenderBounds.Union(charSpanBounds.ToTextRect());

        // 绘制四线三格的调试信息
        if (Config.ShowHandwritingPaperDebugInfo)
        {
            CharHandwritingPaperInfo charHandwritingPaperInfo =
                RenderInfoProvider.GetHandwritingPaperInfo(in lineInfo, firstCharData);
            DrawDebugHandwritingPaper(charSpanBounds, charHandwritingPaperInfo);
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

        textRenderSKPaint.IsAntialias = false;

        Canvas.DrawText(skTextBlob, x, y, textRenderSKPaint);
    }

    /// <inheritdoc />
    protected override void RenderBackground(in ParagraphLineRenderInfo lineRenderInfo)
    {
        SKPaint paint = CachePaint;

        LineDrawingArgument argument = lineRenderInfo.Argument;

        foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous(CheckSameBackground))
        {
            var charData = charList[0];
            SkiaTextRunProperty skiaTextRunProperty = charData.RunProperty.AsSkiaRunProperty();

            SKColor background = skiaTextRunProperty.Background;
            if (background.Alpha == 0x00)
            {
                // 完全透明的，就不绘制了
                // 尽管这样可能导致 Avalonia 命中穿透，但为了性能考虑，还是不绘制了
                continue;
            }

            var x = charData.GetStartPoint().X;
            double y = argument.StartPoint.Y;

            var lastCharData = charList[^1];
            // 宽度是最后一个字符的结束位置减去第一个字符的起始位置
            double width = lastCharData.GetBounds().Right - x;
            double height = argument.LineCharTextSize.Height;

            SKRect backgroundRect = SKRect.Create((float) x, (float) y, (float) width, (float) height);
            paint.Style = SKPaintStyle.Fill;
            paint.Color = background;
            Canvas.DrawRect(backgroundRect, paint);
        }
    }
}
