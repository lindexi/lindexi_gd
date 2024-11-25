using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

public record SkiaTextRunProperty : LayoutOnlyRunProperty
{
    internal SkiaTextRunProperty(SkiaPlatformFontManager skiaPlatformFontManager)
    {
        FontManager = skiaPlatformFontManager;
    }

    public override FontName FontName
    {
        get => _fontName;
        init
        {
            if (value.Equals(_fontName))
            {
                return;
            }

            _fontName = value;
            _skTypeface = null;
        }
    }

    private readonly FontName _fontName;
    private SkiaPlatformFontManager FontManager { get; }

    public SKTypeface GetRenderSKTypeface(char unicodeChar = '1')
    {
        if (_skTypeface is null)
        {
            RenderingFontInfo renderingFontInfo = FontManager.GetRenderingFontInfo(this, unicodeChar);
            _skTypeface = renderingFontInfo.Typeface;
        }

        return _skTypeface;
    }

    private SKTypeface? _skTypeface;
}

internal class SkiaPlatformFontManager
{
    public RenderingFontInfo GetRenderingFontInfo(SkiaTextRunProperty runProperty, char unicodeChar)
    {
        // todo 处理缓存和未找到字体的情况
        var typeface = SKTypeface.FromFamilyName(runProperty.FontName.UserFontName);
        return new RenderingFontInfo(typeface);
    }
}

/// <summary>
/// 渲染时的字体信息
/// </summary>
internal readonly record struct RenderingFontInfo(SKTypeface Typeface);

static class RunPropertyExtension
{
    public static SkiaTextRunProperty AsRunProperty(this IReadOnlyRunProperty runProperty) => (SkiaTextRunProperty) runProperty;
}