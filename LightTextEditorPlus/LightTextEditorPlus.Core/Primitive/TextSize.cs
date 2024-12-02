using System;
using System.Diagnostics;
using System.Globalization;

namespace LightTextEditorPlus.Core.Primitive;

#pragma warning disable CS1591

/// <summary>
/// 文本库使用的尺寸
/// </summary>
[DebuggerDisplay("W:{Width} H:{Height}")]
public readonly struct TextSize : IEquatable<TextSize>
{
    public TextSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width { get; }
    public double Height { get; }

    public TextSize HorizontalUnion(TextSize other)
    {
        return HorizontalUnion(other.Width, other.Height);
    }

    public TextSize HorizontalUnion(double otherWidth,double otherHeight)
    {
        var width = Width + otherWidth;
        var height = Math.Max(Height, otherHeight);
        return new TextSize(width, height);
    }

    public static TextSize Zero => new TextSize(0, 0);

    public bool Equals(TextSize other)
    {
        return Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override bool Equals(object? obj)
    {
        return obj is TextSize other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static TextSize operator +(TextSize size1, TextSize size2)
    {
        return new TextSize(size1.Width + size2.Width, size1.Height + size2.Height);
    }

    public static TextSize operator -(TextSize size1, TextSize size2)
    {
        return new TextSize(size1.Width - size2.Width, size1.Height - size2.Height);
    }

    public static TextSize operator *(TextSize size1, double value)
    {
        return new TextSize(size1.Width * value, size1.Height * value);
    }

    public static TextSize operator /(TextSize size1, double value)
    {
        return new TextSize(size1.Width / value, size1.Height / value);
    }

    public static bool operator ==(TextSize left, TextSize right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextSize left, TextSize right)
    {
        return !left.Equals(right);
    }

    public void Deconstruct(out double width, out double height)
    {
        width = Width;
        height = Height;
    }

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
}