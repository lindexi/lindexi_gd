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

namespace FarlenerenalcardemBajarjelqaijelhawar;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Create two Geometry objects.
        Geometry geometry1 = new EllipseGeometry(new Point(50, 50), 20, 30);
        Geometry geometry2 = new RectangleGeometry(new Rect(100, 100, 50, 50));

        // Compute the convex hull of the two Geometry objects.
        Geometry convexHull = ComputeConvexHull(geometry1, geometry2);

        // Display the convex hull.
        Path path = new Path();
        path.Stroke = Brushes.Black;
        path.StrokeThickness = 1;
        path.Data = convexHull;

        Grid.Children.Add(path);
    }

    private Geometry ComputeConvexHull(Geometry geometry1, Geometry geometry2)
    {
        // Combine the two Geometry objects.
        GeometryGroup geometryGroup = new GeometryGroup();
        geometryGroup.Children.Add(geometry1);
        geometryGroup.Children.Add(geometry2);

        var geometry = Geometry.Combine(geometry1, geometry2, GeometryCombineMode.Union, Transform.Identity);

        // Compute the convex hull of the combined Geometry objects.
        PathGeometry pathGeometry = geometry.GetFlattenedPathGeometry(); //geometryGroup.GetFlattenedPathGeometry();

        var list = new List<Point>();
        foreach (var figure in pathGeometry.Figures)
        {
            list.Add(figure.StartPoint);
           
            foreach (var segment in figure.Segments)
            {
                if (segment is LineSegment lineSegment)
                {
                    list.Add(lineSegment.Point);
                }
                else if (segment is PolyLineSegment polyLineSegment)
                {
                    list.AddRange(polyLineSegment.Points);
                }
            }
        }
        list.Add(list[list.Count - 1]);

        //Point[] points = pathGeometry.Figures.SelectMany(f => f.Segments).OfType<LineSegment>().Select(s => s.Point).ToArray();

        //points = list.ToArray();

        var convexHullPoints = ConvexHull.GrahamScan(list);
        convexHullPoints.Add(convexHullPoints[0]);
        //convexHullPoints = list;
        PathFigure pathFigure = new PathFigure();
        pathFigure.StartPoint = convexHullPoints[0];
        pathFigure.Segments.Add(new PolyLineSegment(convexHullPoints.Skip(1), true));
        return new PathGeometry(new[] { pathFigure });
    }
}

public class ConvexHull
{
    public static List<Point> GrahamScan(List<Point> points)
    {
        if (points == null || points.Count < 3)
        {
            throw new ArgumentException("At least 3 points are required to calculate the convex hull");
        }

        var sortedPoints = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

        var stack = new Stack<Point>();
        stack.Push(sortedPoints[0]);
        stack.Push(sortedPoints[1]);

        for (var i = 2; i < sortedPoints.Count; i++)
        {
            var top = stack.Pop();
            while (stack.Count > 0 && Orientation(stack.Peek(), top, sortedPoints[i]) <= 0)
            {
                top = stack.Pop();
            }
            stack.Push(top);
            stack.Push(sortedPoints[i]);
        }

        return stack.ToList();
    }

    private static double Orientation(Point p1, Point p2, Point p3)
    {
        return (p2.Y - p1.Y) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Y - p2.Y);
    }
}