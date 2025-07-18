using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace NearkugallriweejurgoReequrkuwer.Brushes;
/// <summary>
/// 文本的 Skia 渲染画刷
/// </summary>
public abstract class SkiaTextBrush
{
    /// <summary>
    /// 应用画刷到指定的 <see cref="SKPaint"/> 对象上
    /// </summary>
    /// <param name="paint"></param>
    protected internal abstract void Apply(SKPaint paint);

    ///// <summary>
    ///// 转换纯色画刷
    ///// </summary>
    ///// <param name="color"></param>
    //public static implicit operator SkiaTextBrush(SKColor color)
    //{
    //    return new SolidColorSkiaTextBrush(color);
    //}
}


public sealed class LinearGradientSkiaTextBrush : SkiaTextBrush
{
    public SKPoint StartPoint { get; set; }
    public SKPoint EndPoint { get; set; }
    //public double Opacity { get; set; }
    public SkiaTextGradientStopCollection GradientStops { get; set; } = [];

    /// <inheritdoc />
    protected internal override void Apply(SKPaint paint)
    {
        var (colorList, offsetList) = GradientStops.GetList();

        var linearGradient = SKShader.CreateLinearGradient(StartPoint, EndPoint, colorList, offsetList, SKShaderTileMode.Clamp);
        paint.Shader = linearGradient;
    }
}

public class SkiaTextGradientStopCollection : List<SkiaTextGradientStop>
{
    internal (SKColor[] ColorList, float[] OffsetList) GetList()
    {
        SKColor[] colorList = new SKColor[this.Count];
        float[] offsetList = new float[this.Count];

        for (var i = 0; i < this.Count; i++)
        {
            var (skColor, offset) = this[i];
            colorList[i] = skColor;
            offsetList[i] = offset;
        }

        return (colorList, offsetList);
    }
}

public readonly record struct SkiaTextGradientStop(SKColor Color, float Offset)
{
}