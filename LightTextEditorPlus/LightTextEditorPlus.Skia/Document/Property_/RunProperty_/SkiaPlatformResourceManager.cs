using HarfBuzzSharp;

using SkiaSharp;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformResourceManager
{
    public SkiaPlatformResourceManager(SkiaTextEditor textEditor)
    {
        textEditor.InternalRenderCompleted += TextEditor_InternalRenderCompleted;
    }

    private void TextEditor_InternalRenderCompleted(object? sender, EventArgs e)
    {
        // 每次渲染完成，都可以清理缓存。只有渲染布局过程才会需要用到 Skia 资源
        foreach (KeyValuePair<SkiaTextRunProperty, RenderingRunPropertyInfo> renderingFontInfo in _cache)
        {
            RenderingRunPropertyInfo runPropertyInfo = renderingFontInfo.Value;
            runPropertyInfo.Dispose();
        }
        _cache.Clear();
        _cache.TrimExcess();
    }

    private SKTypeface GetTypeface(SkiaTextRunProperty runProperty, char unicodeChar)
    {
        var typeface = SKTypeface.FromFamilyName(runProperty.FontName.UserFontName, runProperty.FontWeight, runProperty.Stretch, runProperty.FontStyle);
        // todo 处理未找到字体的情况
        //if (skTypeface?.ContainsGlyphs([unicodeChar]) is true)
        //{
        //    return new RenderingFontInfo(skTypeface);
        //}
        return typeface;
    }

    public RenderingRunPropertyInfo GetRenderingRunPropertyInfo(SkiaTextRunProperty runProperty, char unicodeChar)
    {
        // todo 处理对齐情况
        if (_cache.TryGetValue(runProperty, out var cache))
        {
            // todo 处理未找到字体的情况
            //if (skTypeface?.ContainsGlyphs([unicodeChar]) is true)
            //{
            //    return new RenderingFontInfo(skTypeface);
            //}
            return cache;
        }

        SKTypeface skTypeface = GetTypeface(runProperty, unicodeChar);
        SKFont renderSkFont = new SKFont(skTypeface, (float) runProperty.FontSize);
        // From Avalonia
        // Ideally the requested edging should be passed to the glyph run.
        // Currently the edging is computed dynamically inside the drawing context, so we can't know it in advance.
        // But the bounds depends on the edging: for now, always use SubpixelAntialias so we have consistent values.
        // The resulting bounds may be shifted by 1px on some fonts:
        // "F" text with Inter size 14 has a 0px left bound with SubpixelAntialias but 1px with Antialias.

        var edging = SKFontEdging.SubpixelAntialias;

        renderSkFont.Hinting = SKFontHinting.Full;
        renderSkFont.Edging = edging;
        renderSkFont.Subpixel = edging != SKFontEdging.Alias;

        SKPaint skPaint = new SKPaint(renderSkFont);
        // skPaint 已经用上 SKFont 的字号属性，不需要再设置 TextSize 属性
        //skPaint.TextSize = runProperty.FontSize;
        skPaint.Color = runProperty.Foreground;

        var info = new RenderingRunPropertyInfo(skTypeface, renderSkFont, skPaint);
        _cache[runProperty] = info;
        return info;
    }

    private readonly Dictionary<SkiaTextRunProperty, RenderingRunPropertyInfo> _cache =
        new Dictionary<SkiaTextRunProperty, RenderingRunPropertyInfo>();
}