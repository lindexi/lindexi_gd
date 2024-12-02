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
public struct TextPoint
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
    public static TextPoint Zero => new TextPoint();

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
    public TextPoint(double x, double y) : this()
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// 创建给文本库使用的点
    /// </summary>
    /// <param name="sz"></param>
    public TextPoint(TextSize sz) : this()
    {
        X = sz.Width;
        Y = sz.Height;
    }

    /// <summary>
    /// 创建给文本库使用的点
    /// </summary>
    /// <param name="v"></param>
    public TextPoint(Vector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    /// <inheritdoc />
    public override bool Equals(object? o)
    {
        if (o is not TextPoint point)
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
        if (o is not TextPoint compareTo)
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
    public TextPoint Offset(double dx, double dy)
    {
        TextPoint p = this;
        p.X += dx;
        p.Y += dy;
        return p;
    }

    /// <summary>
    /// 对 X 和 Y 取 <see cref="Math.Round(double)"/> 的新点
    /// </summary>
    /// <returns></returns>
    public TextPoint Round()
    {
        return new TextPoint(Math.Round(X), Math.Round(Y));
    }

    /// <summary>
    /// 是否是一个 X 和 Y 都是 0 的点
    /// </summary>
    public bool IsEmpty => X == 0 && Y == 0;

    /// <summary>
    /// 将点转换为尺寸
    /// </summary>
    /// <param name="point"></param>
    public static explicit operator TextSize(TextPoint point)
    {
        return new TextSize(point.X, point.Y);
    }

    /// <summary>
    /// 相减
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <returns></returns>
    public static TextSize operator -(TextPoint pointA, TextPoint pointB)
    {
        return new TextSize(pointA.X - pointB.X, pointA.Y - pointB.Y);
    }

    /// <summary>
    /// 判断等于
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <returns></returns>
    public static bool operator ==(TextPoint pointA, TextPoint pointB)
    {
        return Math.Abs(pointA.X - pointB.X) < TextContext.Epsilon && Math.Abs(pointA.Y - pointB.Y) < TextContext.Epsilon;
    }

    /// <summary>
    /// 判断不等于
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <returns></returns>
    public static bool operator !=(TextPoint pointA, TextPoint pointB)
    {
        return !(pointA == pointB);
    }

    /// <summary>
    /// 判断和给定点的距离
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public double Distance(TextPoint other)
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
    public static implicit operator TextPoint(Vector2 v) => new TextPoint(v);

    /// <summary>
    /// 尝试将给定的字符串转换为点
    /// </summary>
    /// <param name="value"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static bool TryParse(string value, out TextPoint point)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string[] xy = value.Split(',');
            if (xy.Length == 2 && double.TryParse(xy[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var x)
                               && double.TryParse(xy[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var y))
            {
                point = new TextPoint(x, y);
                return true;
            }
        }

        point = default;
        return false;
    }
}