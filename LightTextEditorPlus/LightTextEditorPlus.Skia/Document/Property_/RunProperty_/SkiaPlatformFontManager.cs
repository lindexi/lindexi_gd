using SkiaSharp;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformFontManager
{
    public RenderingFontInfo GetRenderingFontInfo(SkiaTextRunProperty runProperty, char unicodeChar)
    {
        // todo 处理缓存和未找到字体的情况
        // todo 处理加粗
        var typeface = SKTypeface.FromFamilyName(runProperty.FontName.UserFontName);
        
        return new RenderingFontInfo(typeface);
    }
}