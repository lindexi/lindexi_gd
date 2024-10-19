using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;

using SkiaSharp;

using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Rendering;

class RenderManager : IRenderManager, ITextEditorSkiaRender
{
    public RenderManager(SkiaTextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly SkiaTextEditor _textEditor;

    private List<SkiaTextRenderInfo>? RenderInfoList { set; get; }

    /// <summary>
    /// 默认的光标宽度
    /// </summary>
    public const double DefaultCaretWidth = 2;

    private Rect CurrentCaretBounds { set; get; }

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var list = new List<SkiaTextRenderInfo>();

        CaretRenderInfo currentCaretRenderInfo = renderInfoProvider.GetCurrentCaretRenderInfo();
        Rect caretBounds = currentCaretRenderInfo.GetCaretBounds(DefaultCaretWidth);
        CurrentCaretBounds = caretBounds;

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

        SKPaint caretPaint = skPaint;
        caretPaint.Color = SKColors.Black;
        caretPaint.Style = SKPaintStyle.Fill;

        canvas.DrawRect(CurrentCaretBounds.ToSKRect(), caretPaint);
    }

    record SkiaTextRenderInfo(string Text, float X, float Y, IReadOnlyRunProperty RunProperty);
}

public static class SkiaExtensions
{
    public static SKRect ToSKRect(this Rect rect)
    {
        return new SKRect((float) rect.Left, (float) rect.Top, (float) rect.Right, (float) rect.Bottom);
    }
}