using System;

using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 渐变色的相对点
/// </summary>
public readonly record struct GradientSkiaTextBrushRelativePoint
{
    /// <summary>
    /// 创建渐变色的相对点
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="unit"></param>
    /// <exception cref="ArgumentOutOfRangeException">如果选择相对，则要求 X 和 Y 在 [0-1] 范围内</exception>
    public GradientSkiaTextBrushRelativePoint(float x, float y, RelativeUnit unit = RelativeUnit.Relative)
    {
        X = x;
        Y = y;
        Unit = unit;

        if (unit != RelativeUnit.Relative)
        {
            if (x is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, $"当 RelativeUnit 为 Relative 时，应该是在 [0-1] 范围内");
            }

            if (y is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, $"当 RelativeUnit 为 Relative 时，应该是在 [0-1] 范围内");
            }
        }
    }

    internal SKPoint ToSKPoint(SKRect bounds)
    {
        return Unit switch
        {
            RelativeUnit.Relative => new SKPoint(bounds.Left + X * bounds.Width, bounds.Top + Y * bounds.Height),
            RelativeUnit.Absolute => new SKPoint(X, Y),
            _ => throw new ArgumentOutOfRangeException(nameof(Unit), Unit, null)
        };
    }

    /// <summary>
    /// X 坐标
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// 相对的单位
    /// </summary>
    public RelativeUnit Unit { get; init; }

    /// <summary>
    /// 相对单位枚举
    /// </summary>
    public enum RelativeUnit : byte
    {
        /// <summary>
        /// 相对值
        /// </summary>
        Relative,
        /// <summary>
        /// 绝对值
        /// </summary>
        Absolute,
    }
}