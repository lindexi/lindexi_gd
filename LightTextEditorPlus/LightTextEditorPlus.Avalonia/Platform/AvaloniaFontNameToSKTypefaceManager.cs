using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.FontManagers;
using LightTextEditorPlus.Utils;

using SkiaSharp;

namespace LightTextEditorPlus.Platform;

public class AvaloniaFontNameToSKTypefaceManager : IFontNameToSKTypefaceManager
{
    public SKTypeface Resolve(SkiaTextRunProperty runProperty)
    {
        //// 以下这一套被 Avalonia 封住，无法直接使用
        //if (TextEditorFontResourceManager.ResourceDictionary.TryGetValue(runProperty.FontName.UserFontName, out var fontFamily))
        //{
        //    IAvaloniaDependencyResolver resolver;

        //    Typeface typeface1 = new Typeface(fontFamily, runProperty.FontStyle.ToFontStyle(), runProperty.FontWeight.ToFontWeight(), runProperty.Stretch.ToFontStretch());
        //    if (FontManager.Current.TryGetGlyphTypeface(typeface1, out var glyphTypeface))
        //    {
        //        //glyphTypeface.
                
        //    }
        //}

        if (TextEditorFontResourceManager.FontFileDictionary.TryGetValue(runProperty.FontName.UserFontName, out var fontFile))
        {
            return SKTypeface.FromFile(fontFile.FullName);
        }

        var typeface = SKTypeface.FromFamilyName(runProperty.FontName.UserFontName, runProperty.FontWeight, runProperty.Stretch, runProperty.FontStyle);
        return typeface;
    }

    public bool TryFallbackRunProperty(SkiaTextRunProperty runProperty, ICharObject charObject,
        [NotNullWhen(true)] out SkiaTextRunProperty? newRunProperty)
    {
        // todo 实现字体回退
        newRunProperty = null;
        return false;
    }
}
