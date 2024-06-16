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

    public override string ToString()
    {
        return $"Segment2D: {PointA} -> {PointB}";
    }
}