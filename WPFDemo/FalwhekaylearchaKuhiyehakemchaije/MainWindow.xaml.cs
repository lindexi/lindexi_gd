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
            var match = Regex.Match(line, @"(\d+),(\d+)");
            if (match.Success)
            {
                pointList.Add(new Point(double.Parse(match.Groups[1].ValueSpan), double.Parse(match.Groups[2].ValueSpan)));
            }
        }

        var applyMeanFilter = ApplyMeanFilter(pointList);

        var polyline = new Polyline();
        polyline.Stroke = Brushes.Black;
        polyline.StrokeThickness = 2;
        polyline.Points = new PointCollection(applyMeanFilter);

        RootCanvas.Children.Add(polyline);
    }

    public static List<Point> ApplyMeanFilter(List<Point> pointList, int step = 10)
    {
        var xList = ApplyMeanFilter(pointList.Select(t => t.X).ToList(), step);
        var yList = ApplyMeanFilter(pointList.Select(t => t.Y).ToList(), step);

        var newPointList = new List<Point>();
        for (int i = 0; i < xList.Count && i < yList.Count; i++)
        {
            newPointList.Add(new Point(xList[i], yList[i]));
        }

        return newPointList;
    }

    public static List<double> ApplyMeanFilter(List<double> list, int step)
    {
        var newList = new List<double>(list.Take(step / 2));
        for (int i = step / 2; i < list.Count - step + step / 2; i++)
        {
            newList.Add(list.Skip(i - step / 2).Take(step).Sum() / step);
        }
        newList.AddRange(list.Skip(list.Count - (step - step / 2)));
        return newList;
    }
}