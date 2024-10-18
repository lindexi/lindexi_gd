using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;

using SkiaSharp;

using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Rendering;

class RenderManager : IRenderManager, ITextEditorSkiaRender
{
    record SkiaTextRenderInfo(string Text, float X, float Y, IReadOnlyRunProperty RunProperty);

    private List<SkiaTextRenderInfo>? RenderInfoList { set; get; }

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var list = new List<SkiaTextRenderInfo>();

        foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
        {
            foreach (ParagraphLineRenderInfo lineInfo in paragraphRenderInfo.GetLineRenderInfoList())
            {
                // 先不考虑缓存
                LineDrawingArgument argument = lineInfo.Argument;
                foreach (CharData charData in argument.CharList)
                {
                    Point startPoint = charData.GetStartPoint();
                    float x = (float) startPoint.X;
                    float y = (float) startPoint.Y;
                    var skiaTextRenderInfo = new SkiaTextRenderInfo(charData.CharObject.ToText(), x, y, charData.RunProperty);
                    list.Add(skiaTextRenderInfo);
                }
            }
        }

        RenderInfoList = list;
    }

    public void Render(SKCanvas canvas)
    {
        if (RenderInfoList is null)
        {
            return;
        }

        using SKPaint skPaint = new SKPaint();

        var skFontManager = SKFontManager.Default;
        var skTypeface = skFontManager.MatchFamily("微软雅黑");

        skPaint.Typeface = skTypeface;
        skPaint.IsAntialias = true;

        foreach (SkiaTextRenderInfo skiaTextRenderInfo in RenderInfoList)
        {
            skPaint.TextSize = (float) skiaTextRenderInfo.RunProperty.FontSize;
            canvas.DrawText(skiaTextRenderInfo.Text, new SKPoint(skiaTextRenderInfo.X, skiaTextRenderInfo.Y + skPaint.TextSize), skPaint);
        }
    }
}