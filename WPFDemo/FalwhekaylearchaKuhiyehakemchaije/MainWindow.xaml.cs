using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FalwhekaylearchaKuhiyehakemchaije;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var file = "output.txt";
        var lines = System.IO.File.ReadAllLines(file);
        var pointList = new List<Point>();
        foreach (var line in lines)
        {
            var match = Regex.Match(line,@"(\d+),(\d+)");
            if (match.Success)
            {
                pointList.Add(new Point(double.Parse(match.Groups[1].ValueSpan), double.Parse(match.Groups[2].ValueSpan)));
            }
        }

        var polyline = new Polyline();
        polyline.Stroke = Brushes.Black;
        polyline.StrokeThickness = 2;
        polyline.Points = new PointCollection(pointList);

        RootCanvas.Children.Add(polyline);
    }
}