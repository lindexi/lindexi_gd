using SkiaSharp;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformFontManager
{
    public SkiaPlatformFontManager(SkiaTextEditor textEditor)
    {
        textEditor.InternalRenderCompleted += TextEditor_InternalRenderCompleted;
    }

    private void TextEditor_InternalRenderCompleted(object? sender, EventArgs e)
    {
        // 每次渲染完成，都可以清理缓存。只有渲染布局过程才会需要用到 Skia 资源
        foreach (KeyValuePair<SkiaTextRunProperty, RenderingFontInfo> renderingFontInfo in _cache)
        {
            RenderingFontInfo fontInfo = renderingFontInfo.Value;
            fontInfo.Typeface.Dispose();
        }
        _cache.TrimExcess();
    }

    public RenderingFontInfo GetRenderingFontInfo(SkiaTextRunProperty runProperty, char unicodeChar, ref SKTypeface? skTypeface)
    {
        if (skTypeface?.ContainsGlyphs([unicodeChar]) is true)
        {
            return new RenderingFontInfo(skTypeface);
        }

        // todo 处理缓存和未找到字体的情况
        var typeface = SKTypeface.FromFamilyName(runProperty.FontName.UserFontName, runProperty.FontWeight, runProperty.Stretch, runProperty.FontStyle);

        return new RenderingFontInfo(typeface);
    }

    private readonly Dictionary<SkiaTextRunProperty, RenderingFontInfo> _cache =
        new Dictionary<SkiaTextRunProperty, RenderingFontInfo>();
}