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
            throw new ArgumentException("С����������޷�Ӧ���㷨");
        }

        var pointCount = pointList.Count * 2 /*���ߵıʼ��켣*/ + 1 /*�׵��ظ�*/ + 1 /*ĩ�ظ�*/;

        var outlinePointList = new Point[pointCount];

        // ��������ʼ��������֮��������Ƕ�
        double angle = 0.0;
        for (var i = 0; i < pointList.Count; i++)
        {
            var currentPoint = pointList[i];

            // ����������һ�㣬�ǾͿ��Ժͱʼ���ǰ�켣�����һ����м��������Ƕ�
            if (i < pointList.Count - 1)
            {
                var nextPoint = pointList[i + 1];

                var x = nextPoint.Point.X - currentPoint.Point.X;
                var y = nextPoint.Point.Y - currentPoint.Point.Y;

                // ����ֽ���Լ���һ�°ɣ�����Ǽ򵥵���ѧ����
                angle = Math.Atan2(y, x) - Math.PI / 2;
            }

            // �ʼ���ϸ��һ�룬һ����һ�룬���������Ǳʼ���ϸ��
            var halfThickness = inkSize / 2d;

            // ѹ��������ֱ�ӳ˷�����
            halfThickness *= currentPoint.Pressure;
            // �����ñʼ���ϸ̫С
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