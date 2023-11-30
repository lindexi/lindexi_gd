using System.Runtime.CompilerServices;

namespace dotnetCampus.Mathematics.SpatialGeometry;

/// <summary>
/// 交点计算帮助类。
/// </summary>
public static class IntersectionHelper
{
    /// <summary>
    /// 容差。
    /// </summary>
    /// <remarks>
    /// 为了避免浮点数计算带来的误差，边界判断时需要使用容差以包含边界。<br />
    /// 判断容差时最好进行归一化处理。
    /// </remarks>
    public const double Tolerance = 1e-10;

    /// <summary>
    /// 容差 <see cref="Tolerance"/> 的平方。
    /// </summary>
    public const double SquareTolerance = Tolerance * Tolerance;

    /// <summary>
    /// 判断一个值是否在单位区间 [0, 1] 内。
    /// </summary>
    /// <param name="value">已经进行归一化处理的值。</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInUnitInterval(double value)
    {
        return value is >= 0 - Tolerance and <= 1 + Tolerance;
    }

    /// <summary>
    /// 几乎为 0。
    /// </summary>
    /// <param name="value">已经进行归一化处理的值。</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNearlyZero(double value)
    {
        return Math.Abs(value) < Tolerance;
    }

    /// <summary>
    /// 一个非负数是否几乎为 0。
    /// </summary>
    /// <param name="valueNonNegative">已经进行归一化处理的非负数值。</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNonNegativeAlmostZero(double valueNonNegative)
    {
        return valueNonNegative < Tolerance;
    }

    /// <summary>
    /// 计算两条线段的交点。如果没有交点，返回 null。
    /// </summary>
    /// <remarks>
    /// 如果两条线段重合，也视为没有交点。
    /// </remarks>
    /// <param name="segment1"></param>
    /// <param name="segment2"></param>
    /// <returns></returns>
    public static Point2D? GetIntersection(this Segment2D segment1, Segment2D segment2)
    {
        // 使用克莱姆法则求解交点
        var lineVector1 = segment1.Vector;
        var lineVector2 = segment2.Vector;

        var lineLength1 = lineVector1.Length;
        var lineLength2 = lineVector2.Length;
        if (IsNonNegativeAlmostZero(lineLength1) || IsNonNegativeAlmostZero(lineLength2))
        {
            // 线段长度为 0，无交点
            return null;
        }

        /*
         * 假设参数方程为：
         * L1: p = p1 + r1 * v1
         * L2: p = p2 + r2 * v2
         * 可以得到：
         * v1*r1 - v2*r2 = p2 - p1 => (v1, -v2) * (r1, r2) = p2 - p1
         * 根据克莱姆法则解得：
         * r1 = -(p2 - p1)×v2 / (v1×v2) = v2×(p2 - p1) / (v1×v2)
         * r2 = v1×(p1 - p2) / (v1×v2)
         * 代入原方程得到交点：
         * p = p1 + r1 * v1 = p2 + r2 * v2
         */
        var startVector = segment2.PointA - segment1.PointA;
        var det = lineVector1.Det(lineVector2);
        // det == 0 时，两线平行或重合
        // 需要进行归一化计算，也就是转成 cos 值
        if (IsNearlyZero(det / lineLength1 / lineLength2))
        {
            // 如果有端点重合而且线段其他位置没有重叠，返回重合的端点
            if (lineVector1 * lineVector2 > 0)
            {
                // 线段同向
                if ((segment1.PointA - segment2.PointB).SquareLength < SquareTolerance)
                {
                    return (segment1.PointA + segment2.PointB) / 2;
                }

                if ((segment1.PointB - segment2.PointA).SquareLength < SquareTolerance)
                {
                    return (segment1.PointB + segment2.PointA) / 2;
                }
            }
            else
            {
                // 线段反向
                if ((segment1.PointA - segment2.PointA).SquareLength < SquareTolerance)
                {
                    return (segment1.PointA + segment2.PointA) / 2;
                }

                if ((segment1.PointB - segment2.PointB).SquareLength < SquareTolerance)
                {
                    return (segment1.PointB + segment2.PointB) / 2;
                }
            }

            // 没有重合的端点
            return null;
        }

        // 判断交点是否在两条线段上
        var ratio1 = -startVector.Det(-lineVector2) / det;
        var ratio2 = -lineVector1.Det(startVector) / det;
        if (IsInUnitInterval(ratio1) && IsInUnitInterval(ratio2))
        {
            return segment1.GetPoint(ratio1);
        }

        return null;
    }

    public static Point2D? GetIntersection(this Segment2D segment, Arc arc)
    {
        var distance = segment.GetDistanceToLine(arc.Center);
        if (distance > arc.Radius)
        {
            return null;
        }

        var middleLineRatio = segment.GetRatio(arc.Center);
        var d = Math.Sqrt((arc.Radius * arc.Radius) - (distance * distance));

        var lineRatio1 = middleLineRatio - (d / segment.Length);
        var intersection1 = segment.GetPoint(lineRatio1);
        var arcRatio1 = arc.GetRatio(intersection1);
        if (IsInUnitInterval(lineRatio1) && IsInUnitInterval(arcRatio1))
        {
            return intersection1;
        }

        var lineRatio2 = middleLineRatio + (d / segment.Length);
        var intersection2 = segment.GetPoint(lineRatio2);
        var arcRatio2 = arc.GetRatio(intersection2);
        if (IsInUnitInterval(lineRatio2) && IsInUnitInterval(arcRatio2))
        {
            return intersection2;
        }

        return null;
    }

    public static Point2D? GetIntersection(this Segment2D segment, Arc arc, int index)
    {
        var distance = segment.GetDistanceToLine(arc.Center);
        if (distance > arc.Radius)
        {
            return null;
        }

        var middleLineRatio = segment.GetRatio(arc.Center);
        var d = Math.Sqrt((arc.Radius * arc.Radius) - (distance * distance));

        var lineRatio = middleLineRatio + ((index == 0 ? -d : d) / segment.Length);
        var intersection = segment.GetPoint(lineRatio);
        var arcRatio = arc.GetRatio(intersection);
        if (IsInUnitInterval(lineRatio) && IsInUnitInterval(arcRatio))
        {
            return intersection;
        }

        return null;
    }

    public static Point2D? GetIntersection(Arc arc1, Arc arc2)
    {
        var centerPoint = arc2.Center - arc1.Center;
        var distance = centerPoint.Length;
        if (distance > arc1.Radius + arc2.Radius || distance < Math.Abs(arc1.Radius - arc2.Radius))
        {
            return null;
        }

        var d = ((arc1.Radius * arc1.Radius) - (arc2.Radius * arc2.Radius) + (distance * distance)) / (2 * distance);
        var h = Math.Sqrt((arc1.Radius * arc1.Radius) - (d * d));

        var intersection1 = arc1.Center + ((centerPoint.UnitVector * d) - (centerPoint.NormalVector.UnitVector * h));
        var radio11 = arc1.GetRatio(intersection1);
        var radio12 = arc2.GetRatio(intersection1);
        if (IsInUnitInterval(radio11) && IsInUnitInterval(radio12))
        {
            return intersection1;
        }

        var intersection2 = arc1.Center + ((centerPoint.UnitVector * d) + (centerPoint.NormalVector.UnitVector * h));
        var radio21 = arc1.GetRatio(intersection2);
        var radio22 = arc2.GetRatio(intersection2);
        if (IsInUnitInterval(radio21) && IsInUnitInterval(radio22))
        {
            return intersection2;
        }

        return null;
    }

    public static Point2D? GetIntersection(Arc arc1, Arc arc2, int index)
    {
        var centerPoint = arc2.Center - arc1.Center;
        var distance = centerPoint.Length;
        if (distance > arc1.Radius + arc2.Radius || distance < Math.Abs(arc1.Radius - arc2.Radius))
        {
            return null;
        }

        var d = ((arc1.Radius * arc1.Radius) - (arc2.Radius * arc2.Radius) + (distance * distance)) / (2 * distance);
        var h = Math.Sqrt((arc1.Radius * arc1.Radius) - (d * d));

        var intersection = arc1.Center + ((centerPoint.UnitVector * d) +
                                          (centerPoint.NormalVector.UnitVector * h * (index == 0 ? -1 : 1)));
        var radio1 = arc1.GetRatio(intersection);
        var radio2 = arc2.GetRatio(intersection);
        if (IsInUnitInterval(radio1) && IsInUnitInterval(radio2))
        {
            return intersection;
        }

        return null;
    }
}