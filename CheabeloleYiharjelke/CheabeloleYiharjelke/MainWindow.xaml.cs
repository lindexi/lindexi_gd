using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static CheabeloleYiharjelke.几何数学计算辅助类;

using dotnetCampus.Mathematics.SpatialGeometry;

namespace CheabeloleYiharjelke;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

public class ArtInkCanvas : FrameworkElement
{
    public ArtInkCanvas()
    {
        MouseDown += Canvas_MouseDown;
        MouseMove += Canvas_MouseMove;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        Connect(position);
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);

        _lastPoint = position;
        _lastGeometry = new RectangleGeometry(new Rect(new Size(50, 50)));
        var bounds = _lastGeometry.Bounds;
        _lastGeometry.Transform = new TranslateTransform(position.X - bounds.Width / 2, position.Y - bounds.Height / 2);
    }

    private Geometry? _lastGeometry;
    private Point? _lastPoint;

    private DrawingGroup? _drawingGroup;

    private void Connect(Point position)
    {
        if (_lastGeometry is null || _lastPoint is null)
        {
            return;
        }

        var currentGeometry = new RectangleGeometry(new Rect(new Size(50, 50)));
        var bounds = currentGeometry.Bounds;
        currentGeometry.Transform =
            new TranslateTransform(position.X - bounds.Width / 2, position.Y - bounds.Height / 2);

        // 不要太接近
        if (_lastGeometry.FillContainsWithDetail(currentGeometry) != IntersectionDetail.Empty)
        {
            // 具备相交，则不参与计算
            return;
        }

        var lastPoint = _lastPoint.Value;
        Vector 运动方向 = position - lastPoint;

        var segment2D = new Segment2D(lastPoint.ToPoint2D(), position.ToPoint2D());

        // 先计算两两在线的左侧的点
        var lastGeometryPoint = GetPointFromGeometry(_lastGeometry);
        Point? lastGeometryLeftTopPoint = null;
        double lastGeometryLeftTopPointDistance = 0;
        Point? lastGeometryRightBottomPoint = null;
        double lastGeometryRightBottomPointDistance = 0;

        for (var i = 0; i < lastGeometryPoint.Count; i++)
        {
            var point = lastGeometryPoint[i];

            var relation = segment2D.CalculatePointWithLineRelation(point.ToPoint2D());
            if (relation.CrossProductResult == PointInLineCrossProductResult.PointInLineLeft)
            {
                if (lastGeometryLeftTopPoint == null)
                {
                    lastGeometryLeftTopPoint = point;
                    lastGeometryLeftTopPointDistance = relation.Distance;
                }
                else
                {
                    if (lastGeometryLeftTopPointDistance < relation.Distance)
                    {
                        lastGeometryLeftTopPoint = point;
                        lastGeometryLeftTopPointDistance = relation.Distance;
                    }
                    else if (lastGeometryLeftTopPointDistance > relation.Distance)
                    {
                        // 啥都不用做
                    }
                    else // 距离相同时
                    {
                        // 判断两个点哪个在向量方向上更远，也就是哪个点更靠后
                        var 两点与向量方向关系 = 判断两点在已知向量方向上的前后关系(运动方向, lastGeometryLeftTopPoint.Value, point);
                        if (两点与向量方向关系 == 两点与向量方向关系.A点在前)
                        {
                            // 也就是说 B 点更远
                            lastGeometryLeftTopPoint = point;
                        }
                    }
                }
            }
            else if (relation.CrossProductResult == PointInLineCrossProductResult.PointInLineRight)
            {
                if (lastGeometryRightBottomPoint == null)
                {
                    lastGeometryRightBottomPoint = point;
                    lastGeometryRightBottomPointDistance = relation.Distance;
                }
                else
                {
                    if (lastGeometryRightBottomPointDistance < relation.Distance)
                    {
                        lastGeometryRightBottomPoint = point;
                        lastGeometryRightBottomPointDistance = relation.Distance;
                    }
                    else if (lastGeometryRightBottomPointDistance > relation.Distance)
                    {
                        // 啥都不用做
                    }
                    else // 距离相同时
                    {
                        // 判断两个点哪个在向量方向上更远，也就是哪个点更靠后
                        var 两点与向量方向关系 = 判断两点在已知向量方向上的前后关系(运动方向, lastGeometryRightBottomPoint.Value, point);
                        if (两点与向量方向关系 == 两点与向量方向关系.A点在前)
                        {
                            // 也就是说 B 点更远
                            lastGeometryRightBottomPoint = point;
                        }
                    }
                }
            }
            else
            {
                // 线上的不用管，理论上中心点移动过程中，必然有左右两侧的
            }
        }

        Point? currentGeometryLeftTopPoint = null;
        double currentGeometryLeftTopPointDistance = 0;
        Point? currentGeometryRightBottomPoint = null;
        double currentGeometryRightBottomPointDistance = 0;

        var currentGeometryPoint = GetPointFromGeometry(currentGeometry);
        foreach (var point in currentGeometryPoint)
        {
            var relation = segment2D.CalculatePointWithLineRelation(point.ToPoint2D());
            if (relation.CrossProductResult == PointInLineCrossProductResult.PointInLineLeft)
            {
                if (currentGeometryLeftTopPoint is null)
                {
                    currentGeometryLeftTopPoint  = point;
                    currentGeometryLeftTopPointDistance = relation.Distance;
                }
                else
                {
                    if (currentGeometryLeftTopPointDistance < relation.Distance)
                    {
                        currentGeometryLeftTopPoint = point;
                        currentGeometryLeftTopPointDistance = relation.Distance;
                    }
                    else if (currentGeometryLeftTopPointDistance > relation.Distance)
                    {
                        // 啥都不用做
                    }
                    else
                    {
                        var 两点与向量方向关系 = 判断两点在已知向量方向上的前后关系(运动方向, currentGeometryLeftTopPoint.Value, point);
                        if (两点与向量方向关系 == 两点与向量方向关系.B点在前)
                        {
                            currentGeometryLeftTopPoint = point;
                        }
                    }
                }
            }
            else if (relation.CrossProductResult == PointInLineCrossProductResult.PointInLineRight)
            {
                if (currentGeometryRightBottomPoint == null)
                {
                    currentGeometryRightBottomPoint = point;
                    currentGeometryRightBottomPointDistance = relation.Distance;
                }
                else
                {
                    if (currentGeometryRightBottomPointDistance < relation.Distance)
                    {
                        currentGeometryRightBottomPoint = point;
                        currentGeometryRightBottomPointDistance = relation.Distance;
                    }
                    else if (currentGeometryRightBottomPointDistance > relation.Distance)
                    {
                        // 啥都不用做
                    }
                    else
                    {
                        var 两点与向量方向关系 = 判断两点在已知向量方向上的前后关系(运动方向, currentGeometryRightBottomPoint.Value, point);
                        if (两点与向量方向关系 == 两点与向量方向关系.B点在前)
                        {
                            currentGeometryRightBottomPoint = point;
                        }
                    }
                }
            }
            else
            {
                // 线上的不用管，理论上中心点移动过程中，必然有左右两侧的
            }
        }

        if (lastGeometryLeftTopPoint is null || lastGeometryRightBottomPoint is null ||
            currentGeometryLeftTopPoint is null || currentGeometryRightBottomPoint is null)
        {
            return;
        }

        _drawingGroup = new DrawingGroup();
        using (var drawingContext = _drawingGroup.Open())
        {
            drawingContext.DrawGeometry(null,new Pen(Brushes.Black,1),_lastGeometry);
            drawingContext.DrawGeometry(null, new Pen(Brushes.Black, 1), currentGeometry);

            var streamGeometry = new StreamGeometry();
            using (var streamGeometryContext = streamGeometry.Open())
            {
                streamGeometryContext.BeginFigure(lastGeometryLeftTopPoint.Value,false,true);
                streamGeometryContext.PolyLineTo(new[]
                {
                    currentGeometryLeftTopPoint.Value,
                    currentGeometryRightBottomPoint.Value,
                    lastGeometryRightBottomPoint.Value,
                }, true,false);
            }

            drawingContext.DrawGeometry(null, new Pen(Brushes.Black, 1), streamGeometry);
        }

        // 如果是完全靠近的，取裁剪之后的一半的值

        InvalidateVisual();

        List<Point> GetPointFromGeometry(Geometry geometry)
        {
            var result = new List<Point>();
            var flattenedPathGeometry = geometry.GetFlattenedPathGeometry();
            foreach (var pathFigure in flattenedPathGeometry.Figures)
            {
                foreach (var segment in pathFigure.Segments)
                {
                    if (segment is LineSegment lineSegment)
                    {
                        result.Add(lineSegment.Point);
                    }
                    else if (segment is PolyLineSegment polyLineSegment)
                    {
                        result.AddRange(polyLineSegment.Points);
                    }
                }
            }

            return result;
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_drawingGroup != null)
        {
            drawingContext.DrawDrawing(_drawingGroup);
        }
    }

    #region 框架

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
    {
        return new GeometryHitTestResult(this, IntersectionDetail.FullyContains);
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    #endregion
}

public static class 几何数学计算辅助类
{
    public static 两点与向量方向关系 判断两点在已知向量方向上的前后关系(Vector vector, Point a, Point b)
    {
        var abVector = b - a;// 向量AB
        // 计算两个向量的夹角，从而了解哪个点在前
        // 由于只是需要判断正负关系，不需要求夹角，只需要算点积判断正负即可
        var result = vector * abVector;
        if (result > 0)
        {
            // 如果是这个夹角为正，那么B点在前;
            return 两点与向量方向关系.B点在前;
        }
        else if (result < 0)
        {
            // 如果这个夹角为负，那么点A点在前
            return 两点与向量方向关系.A点在前;
        }
        else
        {
            return 两点与向量方向关系.A点和B点垂直向量;
        }
    }
}

public enum 两点与向量方向关系
{
    A点在前,
    B点在前,
    /// <summary>
    /// 即没有前后关系，如 A 和 B 两点坐标相同
    /// </summary>
    A点和B点垂直向量,
}

public static class WpfUnitConverter
{
    public static Point2D ToPoint2D(this Point point) => new Point2D(point.X, point.Y);
    public static Point ToPoint(this Point2D point2D) => new Point(point2D.X, point2D.Y);
}