using System.Diagnostics;

namespace dotnetCampus.Mathematics.SpatialGeometry;

/// <summary>
/// 角大小。
/// </summary>
/// <param name="Radian">弧度。</param>
[DebuggerDisplay("Radian = {Radian}, Degree = {Degree}")]
public readonly record struct AngularMeasure(Radian Radian)
{
    public Degree Degree => Radian.ToDegree();

    /// <summary>
    /// 2π。
    /// </summary>
    public static AngularMeasure Tau => FromRadian(Math.Tau);

    /// <summary>
    /// π。
    /// </summary>
    public static AngularMeasure Pi => FromRadian(Math.PI);

    /// <summary>
    /// π/2。
    /// </summary>
    public static AngularMeasure HalfPi => FromRadian(Math.PI / 2);

    /// <summary>
    /// 0。
    /// </summary>
    public static AngularMeasure Zero => FromRadian(0);

    /// <summary>
    /// 对应的单位方向向量。
    /// </summary>
    public Vector2D UnitDirectionVector => new(Math.Cos(Radian.Value), Math.Sin(Radian.Value));

    /// <summary>
    /// 几乎等于 0。
    /// </summary>
    public bool IsAlmostZero => Math.Abs(Radian.Value) < IntersectionHelper.Tolerance * Math.Tau;

    /// <summary>
    /// 几乎等于 π。
    /// </summary>
    public bool IsAlmostPi => Math.Abs(Radian.Value - Math.PI) < IntersectionHelper.Tolerance * Math.Tau;

    /// <summary>
    /// 几乎等于 π/2。
    /// </summary>
    public bool IsAlmostHalfPi => Math.Abs(Radian.Value - (Math.PI / 2)) < IntersectionHelper.Tolerance * Math.Tau;

    /// <summary>
    /// 几乎等于 2π。
    /// </summary>
    public bool IsAlmostTau => Math.Abs(Radian.Value - Math.Tau) < IntersectionHelper.Tolerance * Math.Tau;

    /// <summary>
    /// 该角范围在 0 到 2π 之间。
    /// </summary>
    public bool IsNormal => Radian.Value is >= 0 and < Math.Tau;

    /// <summary>
    /// 从角度创建。
    /// </summary>
    /// <param name="degree"></param>
    /// <returns></returns>
    public static AngularMeasure FromDegree(double degree)
    {
        return new AngularMeasure(new Degree(degree).ToRadian());
    }

    /// <summary>
    /// 从弧度创建。
    /// </summary>
    /// <param name="radian"></param>
    /// <returns></returns>
    public static AngularMeasure FromRadian(double radian)
    {
        return new AngularMeasure(new Radian(radian));
    }

    /// <summary>
    /// 获取弧度在0到2π之间的值。
    /// </summary>
    /// <returns></returns>
    public AngularMeasure Normalize()
    {
        return new AngularMeasure(Radian.Normalize());
    }

    public override string ToString()
    {
        return $"{Radian} rad";
    }

    #region 运算符重载

    public static AngularMeasure operator +(AngularMeasure a1, AngularMeasure a2)
    {
        return new AngularMeasure(a1.Radian + a2.Radian);
    }

    public static AngularMeasure operator -(AngularMeasure a1, AngularMeasure a2)
    {
        return new AngularMeasure(a1.Radian - a2.Radian);
    }

    public static AngularMeasure operator *(AngularMeasure a, double d)
    {
        return new AngularMeasure(a.Radian * d);
    }

    public static AngularMeasure operator *(double d, AngularMeasure a)
    {
        return new AngularMeasure(a.Radian * d);
    }

    public static AngularMeasure operator /(AngularMeasure a, double d)
    {
        return new AngularMeasure(a.Radian / d);
    }

    public static AngularMeasure operator -(AngularMeasure a)
    {
        return new AngularMeasure(-a.Radian);
    }

    public static double operator /(AngularMeasure a1, AngularMeasure a2)
    {
        return a1.Radian / a2.Radian;
    }

    public static bool operator <(AngularMeasure a1, AngularMeasure a2)
    {
        return a1.Radian < a2.Radian;
    }

    public static bool operator >(AngularMeasure a1, AngularMeasure a2)
    {
        return a1.Radian > a2.Radian;
    }

    public static bool operator <=(AngularMeasure a1, AngularMeasure a2)
    {
        return a1.Radian <= a2.Radian;
    }

    public static bool operator >=(AngularMeasure a1, AngularMeasure a2)
    {
        return a1.Radian >= a2.Radian;
    }

    #endregion
}