using System.Diagnostics;

namespace dotnetCampus.Mathematics.SpatialGeometry;

[DebuggerDisplay("X = {X}, Y = {Y}")]
public readonly record struct Vector2D(double X, double Y)
{
    public static Vector2D NaN => new(double.NaN, double.NaN);

    /// <summary>
    /// 向量平方长度。
    /// </summary>
    public double SquareLength => this * this;

    /// <summary>
    /// 向量长度。
    /// </summary>
    public double Length => Math.Sqrt(SquareLength);

    /// <summary>
    /// 单位向量。
    /// </summary>
    /// <remarks>
    /// 零向量方向任意，零向量的单位向量会返回<see cref="NaN" />。
    /// </remarks>
    public Vector2D UnitVector => this / Length;

    /// <summary>
    /// 法向量。
    /// </summary>
    public Vector2D NormalVector => new(-Y, X);

    /// <summary>
    /// 向量对应的角大小，范围为 -π～π。
    /// </summary>
    public AngularMeasure Angle => AngularMeasure.FromRadian(Math.Atan2(Y, X));

    /// <summary>
    /// 向量行列式，即向量叉乘在二维空间中的推广。
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public double Det(Vector2D other)
    {
        return (X * other.Y) - (Y * other.X);
    }

    [Obsolete("和 Det 完全相同，请使用 Det 代替", true)]
    public double CrossProduct(Vector2D other) => Det(other);

    /// <summary>
    /// Calculates the cross product of two vectors
    /// 计算两向量叉积
    /// </summary>
    /// <param name="vector1"></param>
    /// <param name="vector2"></param>
    /// <returns></returns>
    public static double CrossProduct(Vector2D vector1, Vector2D vector2) => vector1.Det(vector2);

    /// <summary>
    /// 通过角大小创建单位向量。
    /// </summary>
    /// <param name="angularMeasure"></param>
    /// <returns></returns>
    public static Vector2D CreateUnitVectorFromAngle(AngularMeasure angularMeasure)
    {
        return new Vector2D(Math.Cos(angularMeasure.Radian.Value), Math.Sin(angularMeasure.Radian.Value));
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    #region 运算符重载

    public static Vector2D operator +(Vector2D v1, Vector2D v2)
    {
        return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
    }

    public static Vector2D operator -(Vector2D v1, Vector2D v2)
    {
        return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
    }

    public static Vector2D operator *(Vector2D v, double d)
    {
        return new Vector2D(v.X * d, v.Y * d);
    }

    public static Vector2D operator *(double d, Vector2D v)
    {
        return new Vector2D(v.X * d, v.Y * d);
    }

    public static double operator *(Vector2D v1, Vector2D v2)
    {
        return (v1.X * v2.X) + (v1.Y * v2.Y);
    }

    public static Vector2D operator /(Vector2D v, double d)
    {
        return new Vector2D(v.X / d, v.Y / d);
    }

    public static Vector2D operator -(Vector2D v)
    {
        return new Vector2D(-v.X, -v.Y);
    }

    #endregion
}