namespace dotnetCampus.Mathematics.SpatialGeometry;

/// <summary>
/// 线段。
/// </summary>
/// <param name="PointA">开始点。</param>
/// <param name="PointB">结束点。</param>
public readonly record struct Segment2D(Point2D PointA, Point2D PointB)
{
    /// <summary>
    /// 线段的向量。
    /// </summary>
    public Vector2D Vector => PointB - PointA;

    public double SquareLength => Vector.SquareLength;

    public double Length => Vector.Length;

    public Vector2D UnitVector => Vector.UnitVector;

    /// <summary>
    /// 获取指定比例在线段上对应的点。
    /// </summary>
    /// <param name="ratio"></param>
    /// <returns></returns>
    public Point2D GetPoint(double ratio)
    {
        return PointA + (Vector * ratio);
    }

    /// <summary>
    /// 获取点在线段上的比例。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public double GetRatio(Point2D point)
    {
        var pointVector = point - PointA;
        return pointVector * Vector / Vector.SquareLength;
    }

    /// <summary>
    /// 获取点到线段的距离。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public double GetDistanceToLine(Point2D point)
    {
        var pointVector = point - PointA;
        return Math.Abs(pointVector.Det(Vector)) / Length;
    }

    public override string ToString()
    {
        return $"Segment2D: {PointA} -> {PointB}";
    }
}