using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;

using SkiaSharp;

using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;

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

    private SKBitmap? _debugBitmap;

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var textWidth = 1000;
        var textHeight = 1000;
        if (_debugBitmap != null && (_debugBitmap.Width != textWidth || _debugBitmap.Height != textHeight))
        {
            _debugBitmap.Dispose();
            _debugBitmap = null;
        }

        _debugBitmap ??= new SKBitmap(textWidth, textHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKCanvas debugSkCanvas = new SKCanvas(_debugBitmap);
        using SKPaint debugSkPaint = new SKPaint();
        debugSkPaint.Color = SKColors.Blue;
        debugSkPaint.Style = SKPaintStyle.Stroke;
        debugSkPaint.StrokeWidth = 1;
        debugSkPaint.IsAntialias = true;

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

                Rect rect = new Rect(argument.StartPoint, argument.Size);
                debugSkCanvas.DrawRect(rect.ToSKRect(), debugSkPaint);
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

            skPaint.GetGlyphWidths(skiaTextRenderInfo.Text, out var skBounds);

            var y = skiaTextRenderInfo.Y;

            if (skBounds != null && skBounds.Length > 0)
            {
                y += skBounds[0].Height;
            }

            canvas.DrawText(skiaTextRenderInfo.Text, new SKPoint(skiaTextRenderInfo.X, y), skPaint);
        }

        SKPaint caretPaint = skPaint;
        caretPaint.Color = SKColors.Black;
        caretPaint.Style = SKPaintStyle.Fill;

        if (_debugBitmap != null)
        {
            canvas.DrawBitmap(_debugBitmap, 0, 0);
        }

        canvas.DrawRect(CurrentCaretBounds.ToSKRect(), caretPaint);
    }

    record SkiaTextRenderInfo(string Text, float X, float Y, IReadOnlyRunProperty RunProperty);
}