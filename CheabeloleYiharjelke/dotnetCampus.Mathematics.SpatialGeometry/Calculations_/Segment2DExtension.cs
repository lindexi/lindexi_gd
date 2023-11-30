using System.Numerics;

namespace dotnetCampus.Mathematics.SpatialGeometry;

public static class Segment2DExtension
{
    /// <summary>
    /// 获取指定比例在线段上对应的点。
    /// </summary>
    /// <param name="ratio"></param>
    /// <returns></returns>
    public static Point2D GetPoint(this Segment2D segment2D, double ratio)
    {
        return segment2D.PointA + (segment2D.Vector * ratio);
    }

    /// <summary>
    /// 获取点在线段上的投影比例。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static double GetRatio(this Segment2D segment2D, Point2D point)
    {
        var pointVector = point - segment2D.PointA;
        return pointVector * segment2D.Vector / segment2D.SquareLength;
    }

    /// <summary>
    /// 获取点到线段的距离。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static double GetDistanceToLine(this Segment2D segment2D, Point2D point)
    {
        var pointVector = point - segment2D.PointA;
        return Math.Abs(pointVector.Det(segment2D.Vector)) / segment2D.Length;
    }

    /// <summary>
    /// 求线点关系
    /// 使用叉积方法求点在线的左侧还是右侧
    /// 使用向量法求点到线的距离
    /// 详细请看 “叉积法”或“向量法”
    /// </summary>
    /// <param name="line"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static PointWithLineRelation CalculatePointWithLineRelation(this Segment2D line, Point2D point)
    {
        var v1 = line.Vector;
        var v2 = (point - line.PointA);
        var crossProductValue = v1.Det(v2);
        var distance = Math.Abs(crossProductValue) / line.Length;
        return new PointWithLineRelation(crossProductValue, distance);
    }

    ///// <summary>
    ///// 使用叉积方法求点在线的左侧还是右侧
    ///// 详细请看 “叉积法”或“向量法”
    ///// </summary>
    ///// <param name="line"></param>
    ///// <param name="point"></param>
    ///// <returns></returns>
    //public static PointInLineCrossProductResult CalculatePointInLineByCrossProduct(this Segment2D line, Point2D point)
    //{
    //    var v1 = line.Vector;
    //    var v2 = (point - line.PointA);
    //    var result = v1.Det(v2);
    //    if (result > 0)
    //    {
    //        return PointInLineCrossProductResult.PointInLineLeft;
    //    }
    //    else if (result < 0)
    //    {
    //        return PointInLineCrossProductResult.PointInLineRight;
    //    }
    //    else
    //    {
    //        return PointInLineCrossProductResult.PointInLine;
    //    }
    //}
}

/// <summary>
/// 点与线的关系
/// </summary>
/// <param name="CrossProductValue"></param>
/// <param name="Distance"></param>
public readonly record struct PointWithLineRelation(double CrossProductValue, double Distance)
{
    public PointInLineCrossProductResult CrossProductResult
    {
        get
        {
            if (CrossProductValue > 0)
            {
                return PointInLineCrossProductResult.PointInLineLeft;
            }
            else if (CrossProductValue < 0)
            {
                return PointInLineCrossProductResult.PointInLineRight;
            }
            else
            {
                return PointInLineCrossProductResult.PointInLine;
            }
        }
    }
};

public enum PointInLineCrossProductResult
{
    PointInLineLeft,
    PointInLine,
    PointInLineRight,
}