using Windows.Foundation;
using Microsoft.UI;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace UnoInk;

public static class MyInkRender
{
    public static Path? CreatePath(InkInfo inkInfo, int inkSize)
    {
        List<StrokePoint> pointList = inkInfo.PointList;
        if (pointList.Count < 2)
        {
            throw new ArgumentException("小于两个点的无法应用算法");
        }

        var pointCount = pointList.Count * 2/*两边的笔迹轨迹*/  + 1/*首点重复*/+ 1/*末重复*/;

        var outlinePointList = new List<Point>(pointCount);// 待重命名
        for (int i = 0; i < pointCount; i++)
        {
            outlinePointList.Add(new Point());
        }

        double angle = 0.0;
        for (var i = 0; i < pointList.Count; i++)
        {
            var currentPoint = pointList[i];

            if (i < pointList.Count - 1)
            {
                var nextPoint = pointList[i + 1];

                var x = nextPoint.Point.X - currentPoint.Point.X;
                var y = nextPoint.Point.Y - currentPoint.Point.Y;

                angle = Math.Atan2(y, x) - Math.PI / 2;
            }

            var thickness = inkSize * 0.5;

            thickness *= currentPoint.Pressure;
            thickness = Math.Max(0.01, thickness);

            var leftX = currentPoint.Point.X + (Math.Cos(angle) * thickness);
            var leftY = currentPoint.Point.Y + (Math.Sin(angle) * thickness);

            var rightX = currentPoint.Point.X - (Math.Cos(angle) * thickness);
            var rightY = currentPoint.Point.Y - (Math.Sin(angle) * thickness);

            outlinePointList[i + 1] = new Point(leftX, leftY);
            outlinePointList[pointCount - i - 1] = new Point(rightX, rightY);
        }

        outlinePointList[0] = pointList[0].Point;
        outlinePointList[pointList.Count + 1] = pointList[^1].Point;

        var polyLineSegment = new PolyLineSegment();
        foreach (var allPoint in outlinePointList)
        {
            polyLineSegment.Points.Add(allPoint);
        }

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure()
        {
            StartPoint = pointList[0].Point
        };
        pathFigure.Segments.Add(polyLineSegment);
        pathGeometry.Figures.Add(pathFigure);
        if (inkInfo.InkElement is not Path path)
        {
            path = new Path();
        }

        path.Data = pathGeometry;
        //path.Stroke = new SolidColorBrush(Colors.Red);
        path.Fill = new SolidColorBrush(Colors.Red);
        return path;
    }
}