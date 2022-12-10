using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 给文本库使用的点
/// </summary>
/// Copy From https://github.com/dotnet/Microsoft.Maui.Graphics
[DebuggerDisplay("X={X}, Y={Y}")]
//[TypeConverter(typeof(Converters.PointTypeConverter))]
public struct Point
{
    public double X { get; set; }

    public double Y { get; set; }

    public static Point Zero => new Point();

    public override string ToString()
    {
        return $"{{X={X.ToString(CultureInfo.InvariantCulture)} Y={Y.ToString(CultureInfo.InvariantCulture)}}}";
    }

    public Point(double x, double y) : this()
    {
        X = x;
        Y = y;
    }

    public Point(Size sz) : this()
    {
        X = sz.Width;
        Y = sz.Height;
    }

    public Point(Vector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    public override bool Equals(object? o)
    {
        if (o is not Point point)
        {
            return false;
        }

        return this == point;
    }

    public bool Equals(object o, double epsilon)
    {
        if (o is not Point compareTo)
        {
            return false;
        }

        return Math.Abs(compareTo.X - X) < epsilon && Math.Abs(compareTo.Y - Y) < epsilon;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ (Y.GetHashCode() * 397);
    }

    public Point Offset(double dx, double dy)
    {
        Point p = this;
        p.X += dx;
        p.Y += dy;
        return p;
    }

    public Point Round()
    {
        return new Point(Math.Round(X), Math.Round(Y));
    }

    public bool IsEmpty => X == 0 && Y == 0;

    public static explicit operator Size(Point point)
    {
        return new Size(point.X, point.Y);
    }

    public static Size operator -(Point pointA, Point pointB)
    {
        return new Size(pointA.X - pointB.X, pointA.Y - pointB.Y);
    }

    public static bool operator ==(Point pointA, Point pointB)
    {
        return Math.Abs(pointA.X - pointB.X) < TextContext.Epsilon && Math.Abs(pointA.Y - pointB.Y) < TextContext.Epsilon;
    }

    public static bool operator !=(Point pointA, Point pointB)
    {
        return !(pointA == pointB);
    }

    public double Distance(Point other)
    {
        return (double) Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    }

    public void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }

    public static implicit operator Point(Vector2 v) => new Point(v);

    public static bool TryParse(string value, out Point point)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string[] xy = value.Split(',');
            if (xy.Length == 2 && double.TryParse(xy[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var x)
                               && double.TryParse(xy[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var y))
            {
                point = new Point(x, y);
                return true;
            }
        }

        point = default;
        return false;
    }
}