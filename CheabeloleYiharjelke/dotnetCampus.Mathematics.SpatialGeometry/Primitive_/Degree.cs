namespace dotnetCampus.Mathematics.SpatialGeometry;

/// <summary>
/// 度数制的角度，表示 0-360 度
/// </summary>
/// <param name="Value"></param>
public readonly record struct Degree(double Value)
{
    /// <summary>
    /// 270度
    /// </summary>
    public static Degree Degree270 => new(270.0);

    /// <summary>
    /// 90度
    /// </summary>
    public static Degree Degree90 => new(90);

    /// <summary>
    /// 180度
    /// </summary>
    public static Degree Degree180 => new(180);

    /// <summary>
    /// 0度
    /// </summary>
    public static Degree Zero => new(0);
}