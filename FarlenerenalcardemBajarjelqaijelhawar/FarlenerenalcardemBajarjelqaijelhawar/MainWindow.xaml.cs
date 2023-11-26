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

        var geometry = Geometry.Combine(geometry1,geometry2,GeometryCombineMode.Union,Transform.Identity);

        // Compute the convex hull of the combined Geometry objects.
        PathGeometry pathGeometry = geometry.GetFlattenedPathGeometry(); //geometryGroup.GetFlattenedPathGeometry();

        var list = new List<Point>();
        foreach (var figure in pathGeometry.Figures)
        {
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

        Point[] points = pathGeometry.Figures.SelectMany(f => f.Segments).OfType<LineSegment>().Select(s => s.Point).ToArray();

        points = list.ToArray();

        Point[] convexHullPoints = GrahamScan(points);
        convexHullPoints = points;
        PathFigure pathFigure = new PathFigure();
        pathFigure.StartPoint = convexHullPoints[0];
        pathFigure.Segments.Add(new PolyLineSegment(convexHullPoints.Skip(1), true));
        return new PathGeometry(new[] { pathFigure });
    }

    private Point[] GrahamScan(Point[] points)
    {
        // Find the point with the lowest y-coordinate.
        Point lowestPoint = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();

        // Sort the points by polar angle with respect to the lowest point.
        Point[] sortedPoints = points.OrderBy(p => GetPolarAngle(lowestPoint, p)).ToArray();

        // Build the convex hull.
        Stack<Point> stack = new Stack<Point>();
        stack.Push(sortedPoints[0]);
        stack.Push(sortedPoints[1]);
        for (int i = 2; i < sortedPoints.Length; i++)
        {
            while (stack.Count >= 2 && IsCounterClockwise(stack.ElementAt(1), stack.Peek(), sortedPoints[i]))
            {
                stack.Pop();
            }
            stack.Push(sortedPoints[i]);
        }
        return stack.Reverse().ToArray();
    }

    private double GetPolarAngle(Point origin, Point point)
    {
        double dx = point.X - origin.X;
        double dy = point.Y - origin.Y;
        return Math.Atan2(dy, dx);
    }

    private bool IsCounterClockwise(Point a, Point b, Point c)
    {
        double area = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        return area > 0;
    }
}