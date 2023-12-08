using System.Diagnostics;
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

namespace BenukalliwayaChayjanehall;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        //TouchMove += MainWindow_TouchMove;
        StylusMove += MainWindow_StylusMove;
    }

    private void MainWindow_StylusMove(object sender, StylusEventArgs e)
    {
        foreach (var point in e.GetStylusPoints(this))
        {
            var position = point.ToPoint();
            Debug.WriteLine(position);
            double size = 10;
            var ellipse = new Ellipse()
            {
                Width = size,
                Height = size,
                Fill = Brushes.Black,
                RenderTransform = new TranslateTransform(position.X - size / 2, position.Y - size / 2),
            };
            Canvas.Children.Add(ellipse);
        }
    }

    private void MainWindow_TouchMove(object? sender, TouchEventArgs e)
    {
        var touchPoint = e.GetTouchPoint(this);
        double size = 10;
        var ellipse = new Ellipse()
        {
            Width = size,
            Height = size,
            Fill = Brushes.Black,
            RenderTransform = new TranslateTransform(touchPoint.Position.X- size/2, touchPoint.Position.Y - size / 2),
        };
        Canvas.Children.Add(ellipse);
    }
}