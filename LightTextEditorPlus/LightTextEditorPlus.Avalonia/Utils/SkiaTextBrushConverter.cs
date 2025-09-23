using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;

using LightTextEditorPlus.Primitive;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LightTextEditorPlus.Utils;

/// <summary>
/// 从 Skia 的文本库的画刷进行转换的工具类
/// </summary>
public static class SkiaTextBrushConverter
{
    /// <summary>
    /// 转换为 Skia 文本画刷
    /// </summary>
    /// <param name="brush"></param>
    /// <returns></returns>
    public static SkiaTextBrush? ToSkiaTextBrush(IBrush? brush)
    {
        if (brush is ISolidColorBrush solidColorBrush)
        {
            return ToSkiaTextBrush(solidColorBrush);
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
            return ToSkiaTextBrush(linearGradientBrush);
        }

        return null;
    }

    /// <summary>
    /// 转换为 Skia 文本画刷
    /// </summary>
    /// <param name="solidColorBrush"></param>
    /// <returns></returns>
    public static SkiaTextBrush ToSkiaTextBrush(ISolidColorBrush solidColorBrush)
    {
        var color = solidColorBrush.Color.ToSKColor();
        color = color.WithAlpha((byte) (color.Alpha * solidColorBrush.Opacity));
        return color;
    }

    /// <summary>
    /// 转换为 Skia 文本画刷
    /// </summary>
    /// <param name="linearGradientBrush"></param>
    /// <returns></returns>
    public static SkiaTextBrush ToSkiaTextBrush(ILinearGradientBrush linearGradientBrush)
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
}