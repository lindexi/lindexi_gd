using System;
using System.Diagnostics;
using System.Globalization;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本库使用的尺寸
/// </summary>
[DebuggerDisplay("{DebugText}")]
public readonly struct TextSize : IEquatable<TextSize>
{
    /// <summary>
    /// 创建文本库使用的尺寸
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public TextSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 宽度
    /// </summary>
    public double Width { get; init; }
    /// <summary>
    /// 高度
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// 水平方向上的合并，高度取最大值
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public TextSize HorizontalUnion(TextSize other)
    {
        return HorizontalUnion(other.Width, other.Height);
    }

    /// <summary>
    /// 水平方向上的合并，高度取最大值
    /// </summary>
    /// <param name="otherWidth"></param>
    /// <param name="otherHeight"></param>
    /// <returns></returns>
    public TextSize HorizontalUnion(double otherWidth, double otherHeight)
    {
        var width = Width + otherWidth;
        var height = Math.Max(Height, otherHeight);
        return new TextSize(width, height);
    }

    /// <summary>
    /// 零值
    /// </summary>
    public static TextSize Zero => new TextSize(0, 0);

    /// <summary>
    /// 无效的尺寸
    /// </summary>
    public static TextSize Invalid => new TextSize(-1, -1);

    /// <summary>
    /// 是否无效的尺寸
    /// </summary>
    public bool IsInvalid => Width < 0 || Height < 0;

    /// <inheritdoc />
    public bool Equals(TextSize other)
    {
        return Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is TextSize other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    /// <summary>
    /// 相加
    /// </summary>
    /// <param name="size1"></param>
    /// <param name="size2"></param>
    /// <returns></returns>
    public static TextSize operator +(TextSize size1, TextSize size2)
    {
        return new TextSize(size1.Width + size2.Width, size1.Height + size2.Height);
    }

    /// <summary>
    /// 相减
    /// </summary>
    /// <param name="size1"></param>
    /// <param name="size2"></param>
    /// <returns></returns>
    public static TextSize operator -(TextSize size1, TextSize size2)
    {
        return new TextSize(size1.Width - size2.Width, size1.Height - size2.Height);
    }

    /// <summary>
    /// 相乘
    /// </summary>
    /// <param name="size"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static TextSize operator *(TextSize size, double value)
    {
        return new TextSize(size.Width * value, size.Height * value);
    }

    /// <summary>
    /// 相除
    /// </summary>
    /// <param name="size"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static TextSize operator /(TextSize size, double value)
    {
        return new TextSize(size.Width / value, size.Height / value);
    }

    /// <summary>
    /// 判断相等
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(TextSize left, TextSize right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 判断不相等
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(TextSize left, TextSize right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// 尝试从字符串转换
    /// </summary>
    /// <param name="value"></param>
    /// <param name="textSize"></param>
    /// <returns></returns>
    public static bool TryParse(string value, out TextSize textSize)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string[] wh = value.Split(',');
            if (wh.Length == 2
                && double.TryParse(wh[0], NumberStyles.Number, CultureInfo.InvariantCulture, out double w)
                && double.TryParse(wh[1], NumberStyles.Number, CultureInfo.InvariantCulture, out double h))
            {
                textSize = new TextSize(w, h);
                return true;
            }
        }

        textSize = default;
        return false;
    }

    internal string DebugText => this.ToDebugText();

    /// <summary>
    /// 交换宽高
    /// </summary>
    /// <returns></returns>
    public TextSize SwapWidthAndHeight() => new TextSize(Height, Width);
}