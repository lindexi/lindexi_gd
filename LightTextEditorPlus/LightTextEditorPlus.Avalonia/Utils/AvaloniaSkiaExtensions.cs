using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Skia;

using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Primitive;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    public static SkiaTextBrush? ToSkiaTextBrush(this IBrush? brush)
    {
        if (brush is ISolidColorBrush solidColorBrush)
        {
            return solidColorBrush.ToSkiaTextBrush();
        }
        else if (brush is IImmutableSolidColorBrush immutableSolidColorBrush)
        {
            Debug.Fail("由于 IImmutableSolidColorBrush 继承 ISolidColorBrush 接口，除非 Avalonia 修改，否则不能进入此分支");
            var color = immutableSolidColorBrush.Color.ToSKColor();
            color = color.WithAlpha((byte) (color.Alpha * immutableSolidColorBrush.Opacity));
            return color;
        }
        else if (brush is ILinearGradientBrush linearGradientBrush)
        {
            return linearGradientBrush.ToSkiaTextBrush();
        }

        return null;
    }

    internal static SkiaTextBrush ToSkiaTextBrush(this ISolidColorBrush solidColorBrush)
    {
        var color = solidColorBrush.Color.ToSKColor();
        color = color.WithAlpha((byte) (color.Alpha * solidColorBrush.Opacity));
        return color;
    }

    internal static SkiaTextBrush ToSkiaTextBrush(this ILinearGradientBrush linearGradientBrush)
    {
        var linearGradientSkiaTextBrush = new LinearGradientSkiaTextBrush()
        {
            StartPoint = linearGradientBrush.StartPoint.ToTextRelativePoint(),
            EndPoint = linearGradientBrush.EndPoint.ToTextRelativePoint(),
            GradientStops = linearGradientBrush.GradientStops.ToTextGradientStopCollection(),
            Opacity = linearGradientBrush.Opacity
        };

        return linearGradientSkiaTextBrush;
    }

    private static SkiaTextGradientStopCollection ToTextGradientStopCollection
        (this IReadOnlyList<IGradientStop> gradientStopList)
    {
        SkiaTextGradientStop[] stopList = new SkiaTextGradientStop[gradientStopList.Count];
        for (var i = 0; i < gradientStopList.Count; i++)
        {
            IGradientStop gradientStop = gradientStopList[i];
            var skiaTextGradientStop = new SkiaTextGradientStop(gradientStop.Color.ToSKColor(), (float) gradientStop.Offset);
            stopList[i] = skiaTextGradientStop;
        }

        return new SkiaTextGradientStopCollection(stopList);
    }

    private static GradientSkiaTextBrushRelativePoint ToTextRelativePoint(this RelativePoint relativePoint)
    {
        return new GradientSkiaTextBrushRelativePoint((float) relativePoint.Point.X, (float) relativePoint.Point.Y,
            relativePoint.Unit switch
            {
                RelativeUnit.Relative => GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative,
                RelativeUnit.Absolute => GradientSkiaTextBrushRelativePoint.RelativeUnit.Absolute,
                _ => throw new ArgumentOutOfRangeException()
            });
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
