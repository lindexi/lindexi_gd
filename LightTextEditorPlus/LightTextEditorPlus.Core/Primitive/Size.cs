using System;
using System.Globalization;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本库使用的尺寸
/// </summary>
public readonly struct Size : IEquatable<Size>
{
    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width { get; }
    public double Height { get; }

    public static Size Zero => new Size(0, 0);

    public bool Equals(Size other)
    {
        return Width.Equals(other.Width) && Height.Equals(other.Height);
    }

    public override bool Equals(object? obj)
    {
        return obj is Size other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static Size operator +(Size size1, Size size2)
    {
        return new Size(size1.Width + size2.Width, size1.Height + size2.Height);
    }

    public static Size operator -(Size size1, Size size2)
    {
        return new Size(size1.Width - size2.Width, size1.Height - size2.Height);
    }

    public static Size operator *(Size size1, double value)
    {
        return new Size(size1.Width * value, size1.Height * value);
    }

    public static Size operator /(Size size1, double value)
    {
        return new Size(size1.Width / value, size1.Height / value);
    }

    public static bool operator ==(Size left, Size right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Size left, Size right)
    {
        return !left.Equals(right);
    }

    public void Deconstruct(out double width, out double height)
    {
        width = Width;
        height = Height;
    }

    public static bool TryParse(string value, out Size size)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string[] wh = value.Split(',');
            if (wh.Length == 2
                && double.TryParse(wh[0], NumberStyles.Number, CultureInfo.InvariantCulture, out double w)
                && double.TryParse(wh[1], NumberStyles.Number, CultureInfo.InvariantCulture, out double h))
            {
                size = new Size(w, h);
                return true;
            }
        }

        size = default;
        return false;
    }
}