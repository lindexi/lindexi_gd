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

namespace KagaikohulekelLufaiqeyihe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        StreamGeometry geometry = new StreamGeometry();
        geometry.FillRule = FillRule.EvenOdd;

        // Open a StreamGeometryContext that can be used to describe this StreamGeometry 
        // object's contents.
        using (StreamGeometryContext ctx = geometry.Open())
        {
            // Set the begin point of the shape.
            ctx.BeginFigure(new Point(10, 100), true /* is filled */, false /* is closed */);

            // Create a collection of Point structures that will be used with the PolyBezierTo 
            // Method to create the Bezier curve.
            List<Point> pointList = new List<Point>();

            // First Bezier curve is specified with these three points.

            // First control point for first Bezier curve.
            pointList.Add(new Point(100, 0));

            // Second control point for first Bezier curve.
            pointList.Add(new Point(200, 200));

            // Destination point for first Bezier curve.
            pointList.Add(new Point(300, 100));

            // Second Bezier curve is specified with these three points.

            //// First control point for second Bezier curve.
            //pointList.Add(new Point(400, 0));

            //// Second control point for second Bezier curve.
            //pointList.Add(new Point(500, 200));

            //// Destination point for second Bezier curve.
            //pointList.Add(new Point(600, 100));

            // Create a Bezier curve using the collection of Point Structures.
            ctx.PolyBezierTo(pointList, true /* is stroked */, false /* is smooth join */);
        }

        // Freeze the geometry (make it unmodifiable)
        // for additional performance benefits.
        geometry.Freeze();
        var text = geometry.ToString();

        Path.Data = geometry;
    }
}