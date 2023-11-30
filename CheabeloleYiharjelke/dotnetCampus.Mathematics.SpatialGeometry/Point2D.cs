using System.Diagnostics;

namespace dotnetCampus.Mathematics.SpatialGeometry;

[DebuggerDisplay("X = {X}, Y = {Y}")]
public readonly record struct Point2D(double X, double Y)
{
    /// <summary>
    /// 到另一个点的平方距离。
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public double SquareDistanceTo(Point2D other)
    {
        return (other - this).SquareLength;
    }

    /// <summary>
    /// 到另一个点的距离。
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public double DistanceTo(Point2D other)
    {
        return (other - this).Length;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    #region 运算符重载

    public static Point2D operator +(Point2D p1, Point2D p2)
    {
        return new Point2D(p1.X + p2.X, p1.Y + p2.Y);
    }

    public static Point2D operator +(Point2D p, Vector2D v)
    {
        return new Point2D(p.X + v.X, p.Y + v.Y);
    }

    public static Point2D operator +(Vector2D v, Point2D p)
    {
        return new Point2D(p.X + v.X, p.Y + v.Y);
    }

    public static Vector2D operator -(Point2D p1, Point2D p2)
    {
        return new Vector2D(p1.X - p2.X, p1.Y - p2.Y);
    }

    public static Point2D operator *(Point2D p, double d)
    {
        return new Point2D(p.X * d, p.Y * d);
    }

    public static Point2D operator *(double d, Point2D p)
    {
        return new Point2D(p.X * d, p.Y * d);
    }

    public static Point2D operator /(Point2D p, double d)
    {
        return new Point2D(p.X / d, p.Y / d);
    }

    //public static implicit operator Point2D(Point point)
    //{
    //    return new Point2D(point.X, point.Y);
    //}

    //public static implicit operator Point(Point2D point)
    //{
    //    return new Point(point.X, point.Y);
    //}

    #endregion
}