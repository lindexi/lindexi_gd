using System;
using Avalonia.Media;
using Avalonia.Skia;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Utils;

static class AvaloniaSkiaExtensions
{
    public static Color? ToAvaloniaColor(this SKColor? color)
    {
        return color == null ? null : ToAvaloniaColor(color.Value);
    }

    public static Color ToAvaloniaColor(this SKColor color)
    {
        return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
    }

    public static SKFontStyleSlant ToSKFontStyleSlant(this FontStyle fontStyle)
    {
        return fontStyle switch
        {
            FontStyle.Normal => SKFontStyleSlant.Upright,
            FontStyle.Italic => SKFontStyleSlant.Italic,
            FontStyle.Oblique => SKFontStyleSlant.Oblique,
            _ => throw new ArgumentOutOfRangeException(nameof(fontStyle), fontStyle, null)
        };
    }

    public static FontStyle ToFontStyle(this SKFontStyleSlant fontStyle)
    {
        return fontStyle switch
        {
            SKFontStyleSlant.Upright => FontStyle.Normal,
            SKFontStyleSlant.Italic => FontStyle.Italic,
            SKFontStyleSlant.Oblique => FontStyle.Oblique,
            _ => throw new ArgumentOutOfRangeException(nameof(fontStyle), fontStyle, null)
        };
    }

    public static SKFontStyleWeight ToSKFontStyleWeight(this FontWeight fontWeight)
    {
        return fontWeight switch
        {
            FontWeight.Thin => SKFontStyleWeight.Thin,
            FontWeight.ExtraLight => SKFontStyleWeight.ExtraLight,
            FontWeight.Light => SKFontStyleWeight.Light,
            FontWeight.Normal => SKFontStyleWeight.Normal,
            FontWeight.Medium => SKFontStyleWeight.Medium,
            FontWeight.SemiBold => SKFontStyleWeight.SemiBold,
            FontWeight.Bold => SKFontStyleWeight.Bold,
            FontWeight.ExtraBold => SKFontStyleWeight.ExtraBold,
            FontWeight.Black => SKFontStyleWeight.Black,
            FontWeight.ExtraBlack => SKFontStyleWeight.ExtraBlack,
            _ => throw new ArgumentOutOfRangeException(nameof(fontWeight), fontWeight, null)
        };
    }

    public static FontWeight ToFontWeight(this SKFontStyleWeight fontWeight)
    {
        return fontWeight switch
        {
            SKFontStyleWeight.Thin => FontWeight.Thin,
            SKFontStyleWeight.ExtraLight => FontWeight.ExtraLight,
            SKFontStyleWeight.Light => FontWeight.Light,
            SKFontStyleWeight.Normal => FontWeight.Normal,
            SKFontStyleWeight.Medium => FontWeight.Medium,
            SKFontStyleWeight.SemiBold => FontWeight.SemiBold,
            SKFontStyleWeight.Bold => FontWeight.Bold,
            SKFontStyleWeight.ExtraBold => FontWeight.ExtraBold,
            SKFontStyleWeight.Black => FontWeight.Black,
            SKFontStyleWeight.ExtraBlack => FontWeight.ExtraBlack,
            _ => throw new ArgumentOutOfRangeException(nameof(fontWeight), fontWeight, null)
        };
    }

    public static FontStretch ToFontStretch(this SKFontStyleWidth stretch)
     => stretch switch
     {
         SKFontStyleWidth.UltraCondensed => FontStretch.UltraCondensed,
         SKFontStyleWidth.ExtraCondensed => FontStretch.ExtraCondensed,
         SKFontStyleWidth.Condensed => FontStretch.Condensed,
         SKFontStyleWidth.SemiCondensed => FontStretch.SemiCondensed,
         SKFontStyleWidth.Normal => FontStretch.Normal,
         SKFontStyleWidth.SemiExpanded => FontStretch.SemiExpanded,
         SKFontStyleWidth.Expanded => FontStretch.Expanded,
         SKFontStyleWidth.ExtraExpanded => FontStretch.ExtraExpanded,
         SKFontStyleWidth.UltraExpanded => FontStretch.UltraExpanded,
         _ => throw new ArgumentOutOfRangeException(nameof(stretch), stretch, null)
     };

    public static SKFontStyleWidth ToSKFontStyleWidth(this FontStretch stretch)
     => stretch switch
     {
         FontStretch.UltraCondensed => SKFontStyleWidth.UltraCondensed,
         FontStretch.ExtraCondensed => SKFontStyleWidth.ExtraCondensed,
         FontStretch.Condensed => SKFontStyleWidth.Condensed,
         FontStretch.SemiCondensed => SKFontStyleWidth.SemiCondensed,
         FontStretch.Normal => SKFontStyleWidth.Normal,
         FontStretch.SemiExpanded => SKFontStyleWidth.SemiExpanded,
         FontStretch.Expanded => SKFontStyleWidth.Expanded,
         FontStretch.ExtraExpanded => SKFontStyleWidth.ExtraExpanded,
         FontStretch.UltraExpanded => SKFontStyleWidth.UltraExpanded,
         _ => throw new ArgumentOutOfRangeException(nameof(stretch), stretch, null)
     };
}
