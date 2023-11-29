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

namespace BawawlakicheKurawlelwe;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InkCanvas_OnStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        var stylusPointCollection = e.Stroke.StylusPoints;
        List<Point> points = stylusPointCollection.Select(t => t.ToPoint()).ToList();

        points = SmoothPoints(points);

        var size = 6.0;
        foreach (var point in points)
        {
            var ellipse = new Ellipse()
            {
                Width = size,
                Height = size,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                RenderTransform = new TranslateTransform(point.X-size/2,point.Y - size / 2)
            };
            InkCanvas.Children.Add(ellipse);
        }
    }

    public static List<Point> SmoothPoints(List<Point> points)
    {
        List<Point> smoothedPoints = new ();
        smoothedPoints.Add(new System.Windows.Point(points[0].X, points[0].Y));

        for (int i = 1; i < points.Count - 2; i++)
        {
            Point p0 = points[i - 1];
            Point p1 = points[i];
            Point p2 = points[i + 1];
            Point p3 = points[i + 2];

            for (int j = 0; j <= 2; j++)
            {
                double t = (double) j / 10;
                double tt = t * t;
                double ttt = tt * t;

                double q1 = -ttt + 3 * tt - 3 * t + 1;
                double q2 = 3 * ttt - 6 * tt + 3 * t;
                double q3 = -3 * ttt + 3 * tt;
                double q4 = ttt;

                double x =  ((p0.X * q1) + (p1.X * q2) + (p2.X * q3) + (p3.X * q4));
                double y =  ((p0.Y * q1) + (p1.Y * q2) + (p2.Y * q3) + (p3.Y * q4));

                smoothedPoints.Add(new System.Windows.Point(x, y));
            }
        }

        smoothedPoints.Add(new System.Windows.Point(points[points.Count - 1].X, points[points.Count - 1].Y));

        return smoothedPoints;
    }
}