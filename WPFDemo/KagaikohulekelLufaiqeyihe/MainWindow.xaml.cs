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

            // Create an arc. Draw the arc from the begin point to 200,100 with the specified parameters.
            ctx.ArcTo(new Point(203, 161), new Size(131, 56), 45 /* rotation angle */, true /* is large arc */,
                SweepDirection.Counterclockwise, true /* is stroked */, false /* is smooth join */);
            // M10,100A131,56,45,1,0,203,161
            // A Size, rotationAngle, isLargeArc(1), SweepDirection.Counterclockwise(0), Point
        }

        // Freeze the geometry (make it unmodifiable)
        // for additional performance benefits.
        geometry.Freeze();
        var text = geometry.ToString();

        Path.Data = geometry;
    }
}