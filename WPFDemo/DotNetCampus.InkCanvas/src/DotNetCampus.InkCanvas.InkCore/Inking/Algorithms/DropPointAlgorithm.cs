using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Inking.Primitive;
using UnoInk.Inking.InkCore;

namespace DotNetCampus.Inking.Algorithms;

internal static class DropPointAlgorithm
{
    /// <summary>
    /// 按照德熙的玄幻算法，决定传入的点是否能丢掉
    /// </summary>
    /// <param name="pointList"></param>
    /// <param name="currentStylusPoint"></param>
    /// <param name="dropPointCount"></param>
    /// <returns></returns>
    private static bool CanDropLastPoint(IReadOnlyList<InkStylusPoint> pointList, InkStylusPoint currentStylusPoint,
        int dropPointCount)
    {
        if (pointList.Count < 3)
        {
            return false;
        }

        // 已经丢了10个点了，就不继续丢点了
        if (dropPointCount >= 10)
        {
            return false;
        }

        // 假定要丢掉倒数第一个点，所以上一个点是倒数第二个点
        var lastPoint = pointList[^2].Point;
        var currentPoint = currentStylusPoint.Point;

        var lastPointVector = new Vector2((float) lastPoint.X, (float) lastPoint.Y);
        var currentPointVector = new Vector2((float) currentPoint.X, (float) currentPoint.Y);

        var lineVector = currentPointVector - lastPointVector;
        var lineLength = lineVector.Length();

        // 如果移动距离比较长，则不丢点
        if (lineLength > 10)
        {
            return false;
        }

        var last2Point = pointList[^3].Point;
        var line2Vector = lastPointVector - new Vector2((float) last2Point.X, (float) last2Point.Y);
        var line2Length = line2Vector.Length();
        var vector2 = currentPointVector - lastPointVector;
        var distance2 = MathF.Abs(line2Vector.X * vector2.Y - line2Vector.Y * vector2.X) / line2Length;
        if (distance2 > 2)
        {
            return false;
        }

        return true;
    }
}
