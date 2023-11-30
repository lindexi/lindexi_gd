namespace dotnetCampus.Mathematics.SpatialGeometry;

/// <summary>
/// 圆弧。
/// </summary>
/// <param name="Center">圆心。</param>
/// <param name="Radius">半径。</param>
/// <param name="StartAngle">开始角。</param>
/// <param name="Angle">圆心角。</param>
public readonly record struct Arc(Point2D Center, double Radius, AngularMeasure StartAngle, AngularMeasure Angle)
{
    public AngularMeasure EndAngle => StartAngle + Angle;

    public Point2D StartPoint => GetPoint(StartAngle);

    public Point2D EndPoint => GetPoint(EndAngle);

    /// <summary>
    /// 获取 <paramref name="point" /> 在圆弧上的比例。即使 <paramref name="point" /> 不在圆弧角度范围内，也会返回比例，如果点更接近开始点，比例为负，如果点更接近结束点，比例为正。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public double GetRatio(Point2D point)
    {
        var pointVector = point - Center;
        var angle = pointVector.Angle;
        var angleDiff = (angle - StartAngle).Normalize();
        if (angleDiff > (Angle + AngularMeasure.Tau) / 2)
        {
            angleDiff -= AngularMeasure.Tau;
        }

        return angleDiff / Angle;
    }

    /// <summary>
    /// 获取圆弧上指定比例对应的角度。即使 <paramref name="ratio" /> 不在 [0, 1] 范围内，也会返回角度。
    /// </summary>
    /// <param name="ratio"></param>
    /// <returns></returns>
    public AngularMeasure GetAngle(double ratio)
    {
        return StartAngle + (Angle * ratio);
    }

    /// <summary>
    /// 获取圆心指向指定点的角度。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public AngularMeasure GetAngle(Point2D point)
    {
        var pointVector = point - Center;
        return pointVector.Angle;
    }

    /// <summary>
    /// 获取圆心指向指定角度在圆弧上对应的点。
    /// </summary>
    /// <param name="angularMeasure"></param>
    /// <returns></returns>
    public Point2D GetPoint(AngularMeasure angularMeasure)
    {
        return Center + (Vector2D.CreateUnitVectorFromAngle(angularMeasure) * Radius);
    }

    /// <summary>
    /// 获取圆弧上指定比例对应的点。即使 <paramref name="ratio" /> 不在 [0, 1] 范围内，也会返回点。
    /// </summary>
    /// <param name="ratio"></param>
    /// <returns></returns>
    public Point2D GetPoint(double ratio)
    {
        return GetPoint(GetAngle(ratio));
    }

    public override string ToString()
    {
        return $"Arc: {Center} {Radius} {StartAngle} {Angle}";
    }
}