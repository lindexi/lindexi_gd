namespace dotnetCampus.Mathematics.SpatialGeometry;

public static class UnitConverter
{
    /// <summary>
    /// 转换成为 2D 几何描述的角大小，用于参与几何计算
    /// </summary>
    /// <param name="radian"></param>
    /// <returns></returns>
    public static AngularMeasure ToAngularMeasure(this Radian radian)
    {
        return new AngularMeasure(radian);
    }

    /// <summary>
    /// 转换为 0-360 度的角度
    /// </summary>
    public static Degree ToDegree(this Radian radian) => new Degree(radian.Value / Math.PI * 180);

    /// <summary>
    /// 转换为 0-2π 的弧度
    /// </summary>
    /// <returns></returns>
    public static Radian ToRadian(this Degree degree) => new Radian(degree.Value / 180 * Math.PI);
}