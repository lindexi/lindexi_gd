using System.Linq;
using System.Windows.Media;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 用来管理本地资源，包括画刷或者是字体等
/// </summary>
class RunPropertyPlatformManager
{
    public GlyphTypeface GetGlyphTypeface(RunProperty runProperty)
    {
        // todo 字体回滚，字体缓存
        FontFamily fontFamily;
        if (runProperty.FontName.IsNotDefineFontName)
        {
            fontFamily = new FontFamily("微软雅黑");
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
        return glyphTypeface;
    }
}