using System;
using System.Runtime.InteropServices;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Document;

using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

/// <summary>
/// 竖排文本渲染器
/// </summary>
class VerticalSkiaTextRenderer : BaseSkiaTextRenderer
{
    public VerticalSkiaTextRenderer(RenderManager renderManager, in SkiaTextRenderArgument renderArgument) : base(renderManager, in renderArgument)
    {
    }

    protected override void RenderCharList(in TextReadOnlyListSpan<CharData> charList, in ParagraphLineRenderInfo lineInfo)
    {
        LineDrawingArgument argument = lineInfo.Argument;

        CharData firstCharData = charList[0];
        SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();
        // 不需要在这里处理字体回滚，在输入的过程中已经处理过了
        RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(firstCharData.CharObject.CodePoint);
        SKFont skFont = renderingRunPropertyInfo.Font;
        SKPaint textRenderSKPaint = renderingRunPropertyInfo.Paint;

        using var positionList = new TextArrayPoolBoundedList<SKPoint>(charList.Count);
        for (int i = 0; i < charList.Count; i++)
        {
            CharData charData = charList[i];
            TextSize frameSize = charData.Size
                // 由于采用的是横排的坐标，在竖排计算下，需要倒换一下
                .SwapWidthAndHeight();
            TextSize faceSize = charData.FaceSize.SwapWidthAndHeight();
            // 这里的 space 计算和 y 值的计算，请参阅 《Skia 垂直直排竖排文本字符尺寸间距.enbx》
            var space = frameSize.Height - faceSize.Height;

            (double x, double y) = charData.GetStartPoint();
            var contentMargin = argument.StartPoint.X - x;

            if (!ArrangingType.IsLeftToRightVertical)
            {
                // 如果不是从左到右的竖排，则需要将 x 减去行宽度，确保从左到右渲染，不会让竖排越过文档右边
                // 文本字符是从左
                x -= (argument.LineContentSize.Height - contentMargin);
            }

            var charBounds = new TextRect(x, y, frameSize.Width, frameSize.Height);
            DrawDebugBoundsInfo(charBounds, Config.DebugDrawCharBoundsInfo);

            RenderBounds = RenderBounds.Union(charBounds);

            y += charData.Baseline - space / 2;
            if (charData.CharDataInfo.Status == CharDataInfoStatus.LigatureContinue)
            {
                // 对于连写字的续写字符，位置和前一个字符保持一致，不参与具体的渲染。不能加入到 positionList 里面。最终也不在 charList 里面体现
                // 连写字的续写字符，将和连写字的起始字符合并共用一个 GlyphIndex 值。此时也需要让 positionList 数量上和 glyphIndexSpan 数量保持一致。于是就不能在此对 positionList 添加对象
            }
            else
            {
                positionList.Add(new SKPoint((float) x, (float) y));
            }
        }

        using var glyphIndexSpanContext = charList.ToRenderGlyphIndexSpanContext();
        Span<ushort> glyphIndexSpan = glyphIndexSpanContext.Span;
        Span<byte> glyphIndexByteSpan = MemoryMarshal.AsBytes(glyphIndexSpan);
        using SKTextBlob skTextBlob = SKTextBlob.CreatePositioned(glyphIndexByteSpan, SKTextEncoding.GlyphId, skFont, positionList.ToSpan());
        Canvas.DrawText(skTextBlob, 0, 0, textRenderSKPaint);
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

            var x = argument.StartPoint.X;
            double y = charData.GetStartPoint().Y;

            var lastCharData = charList[^1];
            TextPoint lastCharDataStartPoint = lastCharData.GetStartPoint();
            double lastCharDataHeight = lastCharData.Size.SwapWidthAndHeight().Height;

            double width = argument.LineCharTextSize.SwapWidthAndHeight().Width;
            double height = (lastCharDataStartPoint.Y + lastCharDataHeight) - y;

            if (!ArrangingType.IsLeftToRightVertical)
            {
                // 如果是从右到左的竖排，则需要将 x 减去行宽度，确保从左到右渲染，不会让竖排越过文档右边。否则将会在行的右侧绘制背景
                x -= width;
            }

            SKRect backgroundRect = SKRect.Create((float) x, (float) y, (float) width, (float) height);
            paint.Style = SKPaintStyle.Fill;
            paint.Color = background;
            Canvas.DrawRect(backgroundRect, paint);
        }
    }
}
