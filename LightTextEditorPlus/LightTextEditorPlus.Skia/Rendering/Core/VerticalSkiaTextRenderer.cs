using System;
using System.Runtime.InteropServices;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;

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

    private ArrangingType ArrangingType => TextEditor.TextEditorCore.ArrangingType;

    protected override void RenderTextLine(in ParagraphLineRenderInfo lineInfo)
    {
        // 先不考虑缓存
        LineDrawingArgument argument = lineInfo.Argument;

        if (TextEditor.RenderConfiguration.UseRenderCharByCharMode)
        {
            // 逐字符渲染。渲染效率慢，但可以遵循布局结果
            for (var i = 0; i < argument.CharList.Count; i++)
            {
                TextReadOnlyListSpan<CharData> charList = argument.CharList.Slice(i, 1);
                RenderCharList(charList, lineInfo);
            }
        }
        else
        {
            foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
            {
                RenderCharList(charList, lineInfo);
            }
        }

        TextPoint lineStartPoint = argument.StartPoint;
        if (!ArrangingType.IsLeftToRightVertical)
        {
            lineStartPoint = lineStartPoint with
            {
                X = lineStartPoint.X - argument.LineContentSize.Height
            };
        }
        DrawDebugBounds(new TextRect(lineStartPoint, argument.LineContentSize.SwapWidthAndHeight()), Config.DebugDrawLineContentBoundsInfo);
    }

    private void DrawDebugBounds(TextRect bounds, TextEditorDebugBoundsDrawInfo? drawInfo)
    {
        if (drawInfo is null)
        {
            return;
        }
        DrawDebugBoundsInfo(bounds.ToSKRect(), drawInfo);
    }

    private void RenderCharList(TextReadOnlyListSpan<CharData> charList, ParagraphLineRenderInfo lineInfo)
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
            DrawDebugBounds(charBounds, Config.DebugDrawCharBoundsInfo);

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
}
