namespace dotnetCampus.Mathematics.SpatialGeometry;

/// <summary>
/// 弧度，表示 0-2π 的弧度
/// </summary>
/// <param name="Value"></param>
public readonly record struct Radian(double Value)
{
    /// <summary>
    /// 获取弧度在0到2π之间的值。
    /// </summary>
    /// <returns></returns>
    public Radian Normalize()
    {
        return Value switch
        {
            > Math.Tau => new Radian(Value % Math.Tau),
            < 0 => new Radian((Value % Math.Tau) + Math.Tau),
            _ => this
        };
    }

    #region 运算符重载

    public static Radian operator +(Radian a1, Radian a2)
    {
        return new Radian(a1.Value + a2.Value);
    }

    public static Radian operator -(Radian a1, Radian a2)
    {
        return new Radian(a1.Value - a2.Value);
    }

    public static Radian operator *(Radian a, double d)
    {
        return new Radian(a.Value * d);
    }

    public static Radian operator *(double d, Radian a)
    {
        return new Radian(a.Value * d);
    }

    public static Radian operator /(Radian a, double d)
    {
        return new Radian(a.Value / d);
    }

    public static Radian operator -(Radian a)
    {
        return new Radian(-a.Value);
    }

    public static double operator /(Radian a1, Radian a2)
    {
        return a1.Value / a2.Value;
    }

    public static bool operator <(Radian a1, Radian a2)
    {
        return a1.Value < a2.Value;
    }

    public static bool operator >(Radian a1, Radian a2)
    {
        return a1.Value > a2.Value;
    }

    public static bool operator <=(Radian a1, Radian a2)
    {
        return a1.Value <= a2.Value;
    }

    public static bool operator >=(Radian a1, Radian a2)
    {
        return a1.Value >= a2.Value;
    }

    #endregion
}