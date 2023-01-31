using System.Globalization;
using System.Linq;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Render;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 用来管理本地资源，包括画刷或者是字体等
/// </summary>
class RunPropertyPlatformManager
{
    public GlyphTypeface GetGlyphTypeface(RunProperty runProperty)
    {
        // todo 字体缓存
        FontFamily fontFamily;
        if (runProperty.FontName.IsNotDefineFontName)
        {
            fontFamily = TextEditor.StaticConfiguration.DefaultNotDefineFontFamily; 
        }
        else
        {
            fontFamily = new FontFamily(runProperty.FontName.UserFontName);
        }

        var collection = fontFamily.GetTypefaces();
        Typeface typeface = collection.First();

        foreach (var t in collection)
        {
            if (t.Stretch == runProperty.Stretch && t.Weight == runProperty.Weight)
            {
                typeface = t;
                break;
            }
        }

        bool success = typeface.TryGetGlyphTypeface(out var glyphTypeface);

        if (!success)
        {
            // 找不到字体，需要进行回滚
            // todo 回滚字体时，需要给定对应的字符，否则回滚将失败
            if (FallBackFontFamily.TryGetFallBackFontFamily('1', out var familyName))
            {
                var fallbackTypeface = new Typeface(new FontFamily(familyName), typeface.Style, typeface.Weight,
                    typeface.Stretch);

                if (fallbackTypeface.TryGetGlyphTypeface(out var fallbackGlyph))
                {
                    glyphTypeface = fallbackGlyph;
                }
            }
        }

        return glyphTypeface;
    }

    private FallBackFontFamily FallBackFontFamily { get; } = new FallBackFontFamily(CultureInfo.CurrentUICulture);
}