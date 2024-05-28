using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml.Shapes;

namespace UnoInk;

public static class MyInkRender
{
    public static Polygon CreatePath(InkInfo inkInfo, int inkSize)
    {
        List<StrokePoint> pointList = inkInfo.PointList;
        var outlinePointList = GetOutlinePointList(pointList, inkSize);

        if (inkInfo.InkElement is not Polygon polygon)
        {
            polygon = new Polygon();
        }

        polygon.Points.Clear();

        foreach (var point in outlinePointList)
        {
            polygon.Points.Add(point);
        }
        polygon.Fill = new SolidColorBrush(Colors.Red);
        return polygon;
    }

    public static PathGeometry CreatePathGeometry(Point[] outlinePointList)
    {
        var polyLineSegment = new PolyLineSegment();
        foreach (var point in outlinePointList)
        {
            polyLineSegment.Points.Add(point);
        }

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure()
        {
            StartPoint = outlinePointList[0]
        };
        pathFigure.Segments.Add(polyLineSegment);
        pathGeometry.Figures.Add(pathFigure);
        return pathGeometry;
    }

    public static Point[] GetOutlinePointList(List<StrokePoint> pointList, int inkSize)
    {
        if (pointList.Count < 2)
        {
            throw new ArgumentException("小于两个点的无法应用算法");
        }

        var pointCount = pointList.Count * 2 /*两边的笔迹轨迹*/ + 1 /*首点重复*/ + 1 /*末重复*/;

        var outlinePointList = new Point[pointCount];

        // 用来计算笔迹点的两点之间的向量角度
        double angle = 0.0;
        for (var i = 0; i < pointList.Count; i++)
        {
            var currentPoint = pointList[i];

            // 如果不是最后一点，那就可以和笔迹当前轨迹点的下一点进行计算向量角度
            if (i < pointList.Count - 1)
            {
                var nextPoint = pointList[i + 1];

                var x = nextPoint.Point.X - currentPoint.Point.X;
                var y = nextPoint.Point.Y - currentPoint.Point.Y;

                // 拿着纸笔自己画一下吧，这个是简单的数学计算
                angle = Math.Atan2(y, x) - Math.PI / 2;
            }

            // 笔迹粗细的一半，一边用一半，合起来就是笔迹粗细了
            var halfThickness = inkSize / 2d;

            // 压感这里是直接乘法而已
            halfThickness *= currentPoint.Pressure;
            // 不能让笔迹粗细太小
            halfThickness = Math.Max(0.01, halfThickness);

            var leftX = currentPoint.Point.X + (Math.Cos(angle) * halfThickness);
            var leftY = currentPoint.Point.Y + (Math.Sin(angle) * halfThickness);

            var rightX = currentPoint.Point.X - (Math.Cos(angle) * halfThickness);
            var rightY = currentPoint.Point.Y - (Math.Sin(angle) * halfThickness);

            outlinePointList[i + 1] = new Point(leftX, leftY);
            outlinePointList[pointCount - i - 1] = new Point(rightX, rightY);
        }

        outlinePointList[0] = pointList[0].Point;
        outlinePointList[pointList.Count + 1] = pointList[^1].Point;
        return outlinePointList;
    }
}