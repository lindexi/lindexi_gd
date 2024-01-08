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
    /// <summary>
    ///  X 坐标
    /// </summary>
    public double X { get; set; }

    /// <summary>
    ///  Y 坐标
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 表示一个 X 和 Y 都是 0 的点
    /// </summary>
    public static Point Zero => new Point();

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{{X={X.ToString(CultureInfo.InvariantCulture)} Y={Y.ToString(CultureInfo.InvariantCulture)}}}";
    }

    /// <summary>
    /// 创建给文本库使用的点
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public Point(double x, double y) : this()
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// 创建给文本库使用的点
    /// </summary>
    /// <param name="sz"></param>
    public Point(Size sz) : this()
    {
        X = sz.Width;
        Y = sz.Height;
    }

    /// <summary>
    /// 创建给文本库使用的点
    /// </summary>
    /// <param name="v"></param>
    public Point(Vector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    /// <inheritdoc />
    public override bool Equals(object? o)
    {
        if (o is not Point point)
        {
            return false;
        }

        return this == point;
    }

    /// <summary>
    /// 判断相等
    /// </summary>
    /// <param name="o"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    public bool Equals(object o, double epsilon)
    {
        if (o is not Point compareTo)
        {
            return false;
        }

        return Math.Abs(compareTo.X - X) < epsilon && Math.Abs(compareTo.Y - Y) < epsilon;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return X.GetHashCode() ^ (Y.GetHashCode() * 397);
    }

    /// <summary>
    /// 获取相对于当前点给定偏移的新点
    /// </summary>
    /// <param name="dx"></param>
    /// <param name="dy"></param>
    /// <returns></returns>
    public Point Offset(double dx, double dy)
    {
        Point p = this;
        p.X += dx;
        p.Y += dy;
        return p;
    }

    /// <summary>
    /// 对 X 和 Y 取 <see cref="Math.Round(double)"/> 的新点
    /// </summary>
    /// <returns></returns>
    public Point Round()
    {
        return new Point(Math.Round(X), Math.Round(Y));
    }

    /// <summary>
    /// 是否是一个 X 和 Y 都是 0 的点
    /// </summary>
    public bool IsEmpty => X == 0 && Y == 0;

    /// <summary>
    /// 将点转换为尺寸
    /// </summary>
    /// <param name="point"></param>
    public static explicit operator Size(Point point)
    {
        return new Size(point.X, point.Y);
    }

    /// <summary>
    /// 相减
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <returns></returns>
    public static Size operator -(Point pointA, Point pointB)
    {
        return new Size(pointA.X - pointB.X, pointA.Y - pointB.Y);
    }

    /// <summary>
    /// 判断等于
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <returns></returns>
    public static bool operator ==(Point pointA, Point pointB)
    {
        return Math.Abs(pointA.X - pointB.X) < TextContext.Epsilon && Math.Abs(pointA.Y - pointB.Y) < TextContext.Epsilon;
    }

    /// <summary>
    /// 判断不等于
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <returns></returns>
    public static bool operator !=(Point pointA, Point pointB)
    {
        return !(pointA == pointB);
    }

    /// <summary>
    /// 判断和给定点的距离
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public double Distance(Point other)
    {
        return (double) Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    }

    /// <summary>
    /// 解开构造
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }

    /// <summary>
    /// 从向量转换为点
    /// </summary>
    /// <param name="v"></param>
    public static implicit operator Point(Vector2 v) => new Point(v);

    /// <summary>
    /// 尝试将给定的字符串转换为点
    /// </summary>
    /// <param name="value"></param>
    /// <param name="point"></param>
    /// <returns></returns>
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