using System;
using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering.Core;

/// <summary>
/// 竖排文本渲染器
/// </summary>
class VerticalSkiaTextRender : BaseSkiaTextRender
{
    public VerticalSkiaTextRender(RenderManager renderManager) : base(renderManager.TextEditor)
    {
        RenderManager = renderManager;
    }

    public RenderManager RenderManager { get; }

    public override SkiaTextRenderResult Render(in SkiaTextRenderArgument renderArgument)
    {
        SKCanvas canvas = renderArgument.Canvas;
        RenderInfoProvider renderInfoProvider = renderArgument.RenderInfoProvider;
        TextRect renderBounds = renderArgument.RenderBounds;

        Debug.Assert(!renderInfoProvider.IsDirty);

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            foreach (ParagraphLineRenderInfo lineInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                // 先不考虑缓存
                LineDrawingArgument argument = lineInfo.Argument;
                foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
                {
                    CharData firstCharData = charList[0];
                    SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();
                    // 不需要在这里处理字体回滚，在输入的过程中已经处理过了
                    RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(firstCharData.CharObject.CodePoint);
                    SKFont skFont = renderingRunPropertyInfo.Font;
                    SKPaint textRenderSKPaint = renderingRunPropertyInfo.Paint;

                    using CharDataListToCharSpanResult charSpanResult = charList.ToCharSpan();
                    ReadOnlySpan<char> charSpan = charSpanResult.CharSpan;

                    SKPoint[] positionList = new SKPoint[charList.Count];
                    for (int i = 0; i < charList.Count; i++)
                    {
                        CharData charData = charList[i];
                        TextSize frameSize = charData.Size!.Value
                            // 由于采用的是横排的坐标，在竖排计算下，需要倒换一下
                            .SwapWidthAndHeight();
                        TextSize faceSize = charData.FaceSize.SwapWidthAndHeight();
                        // 这里的 space 计算和 y 值的计算，请参阅 《Skia 垂直直排竖排文本字符尺寸间距.enbx》
                        var space = frameSize.Height - faceSize.Height;

                        (double x, double y) = charData.GetStartPoint();

                        var charBounds = new TextRect(x, y, frameSize.Width, frameSize.Height);
                        DrawDebugBounds(charBounds, DebugDrawCharBoundsColor);
                        
                        renderBounds = renderBounds.Union(charBounds);

                        y += charData.Baseline - space / 2;
                        positionList[i] = new SKPoint((float) x, (float) y);
                    }

                    using SKTextBlob skTextBlob = SKTextBlob.CreatePositioned(charSpan, skFont, positionList.AsSpan());
                    canvas.DrawText(skTextBlob, 0, 0, textRenderSKPaint);
                }
            }

            void DrawDebugBounds(TextRect bounds, SKColor? color)
            {
                if (color is null)
                {
                    return;
                }

                SKPaint debugPaint = GetDebugPaint(color.Value);
                canvas.DrawRect(bounds.ToSKRect(), debugPaint);
            }
        }

        return new SkiaTextRenderResult()
        {
            RenderBounds = renderBounds
        };
    }
}