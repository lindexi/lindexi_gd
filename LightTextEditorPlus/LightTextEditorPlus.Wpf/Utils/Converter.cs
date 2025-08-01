using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LightTextEditorPlus.Utils;

internal static class Converter
{
    /// <summary>
    /// 比较 <see cref="System.Windows.Media.Brush"/> 实例所设置的文本是否会渲染成相同效果。
    /// 实例可以为 null。
    /// </summary>
    public static bool AreEquals(System.Windows.Media.Brush? value1, System.Windows.Media.Brush? value2)
    {
        if (ReferenceEquals(value1, value2))
        {
            return true;
        }

        if (value1 is null || value2 is null)
        {
            return false;
        }

        if (value1.GetType() != value2.GetType())
        {
            return false;
        }

        if (value1 is SolidColorBrush colorBrush1 && value2 is SolidColorBrush colorBrush2)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return colorBrush1.Color == colorBrush2.Color && colorBrush1.Opacity == colorBrush2.Opacity;
        }

        if (value1 is ImageBrush imageBrush1
            && value2 is ImageBrush imageBrush2)
        {
            return imageBrush1.ImageSource.ToString().Equals(imageBrush2.ImageSource.ToString())
                   && imageBrush1.Viewbox.Equals(imageBrush2.Viewbox)
                   && imageBrush1.Stretch == imageBrush2.Stretch
                   // ReSharper disable once CompareOfFloatsByEqualityOperator
                   && imageBrush1.Opacity == imageBrush2.Opacity
                   && imageBrush1.TileMode == imageBrush2.TileMode;
        }

        if (value1 is LinearGradientBrush linearGradientBrush1 && value2 is LinearGradientBrush linearGradientBrush2)
        {
            return AreEquals(linearGradientBrush1, linearGradientBrush2);
        }

        return Equals(value1, value2);
    }

    /// <summary>
    /// 判断颜色相等
    /// </summary>
    /// <param name="linearGradientBrush1"></param>
    /// <param name="linearGradientBrush2"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public static bool AreEquals(LinearGradientBrush linearGradientBrush1,
        LinearGradientBrush linearGradientBrush2)
    {
        if (linearGradientBrush1.ColorInterpolationMode !=
            linearGradientBrush2.ColorInterpolationMode
            || linearGradientBrush1.EndPoint !=
            linearGradientBrush2.EndPoint
            || linearGradientBrush1.MappingMode !=
            linearGradientBrush2.MappingMode
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            || linearGradientBrush1.Opacity !=
            linearGradientBrush2.Opacity
            || linearGradientBrush1.StartPoint !=
            linearGradientBrush2.StartPoint
            || linearGradientBrush1.SpreadMethod !=
            linearGradientBrush2.SpreadMethod
            || linearGradientBrush1.GradientStops.Count !=
            linearGradientBrush2.GradientStops.Count)
        {
            return false;
        }

        for (int i = 0; i < linearGradientBrush1.GradientStops.Count; i++)
        {
            if (linearGradientBrush1.GradientStops[i].Color !=
                linearGradientBrush2.GradientStops[i].Color
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                || linearGradientBrush1.GradientStops[i].Offset !=
                linearGradientBrush2.GradientStops[i].Offset)
            {
                return false;
            }
        }

        return true;
    }
}
