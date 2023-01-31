using System.Diagnostics.CodeAnalysis;
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
    public RunPropertyPlatformManager(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditor _textEditor;

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

        // 获取字体，获取方式：
        // 先拿首个，然后再尝试获取字体样式接近的
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
            // 字体回滚：
            // 1. 先使用业务层传入的字体回滚策略。例如将 “方正楷体” 修改为 “楷体”
            // 2. 如果业务层的字体回滚策略不满足，那就采用 WPF 的方式回滚

            
            // 找不到字体，需要进行回滚
            // todo 回滚字体时，需要给定对应的字符，否则回滚将失败
            if (TryGetFallbackGlyphTypefaceByWpf(typeface,out var fallbackGlyph))
            {
                glyphTypeface = fallbackGlyph;
            }
        }

        return glyphTypeface;
    }

    private bool TryGetFallbackGlyphTypefaceByWpf(Typeface typeface, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
    {
        if (FallBackFontFamily.TryGetFallBackFontFamily('1', out var familyName))
        {
            var fallbackTypeface = new Typeface(new FontFamily(familyName), typeface.Style, typeface.Weight,
                typeface.Stretch);

            return fallbackTypeface.TryGetGlyphTypeface(out glyphTypeface);
        }

        glyphTypeface = null;
        return false;
    }

    private FallBackFontFamily FallBackFontFamily { get; } = new FallBackFontFamily(CultureInfo.CurrentUICulture);
}