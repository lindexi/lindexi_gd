using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YegeenurcairwheBeahealelbewe;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnStylusDown(object sender, StylusDownEventArgs e)
    {
        var polyline = new Polyline()
        {
            Stroke = Brushes.Black,
            StrokeThickness = 5
        };
        InkCanvas.Children.Add(polyline);

        _pointCache[e.StylusDevice.Id] = polyline;

        foreach (var stylusPoint in e.GetStylusPoints(this))
        {
            polyline.Points.Add(stylusPoint.ToPoint());
        }
    }

    private void MainWindow_OnStylusMove(object sender, StylusEventArgs e)
    {
        if (_pointCache.TryGetValue(e.StylusDevice.Id,out var polyline))
        {
            foreach (var stylusPoint in e.GetStylusPoints(this))
            {
                polyline.Points.Add(stylusPoint.ToPoint());
            }
        }
    }

    private void MainWindow_OnStylusUp(object sender, StylusEventArgs e)
    {
        if (_pointCache.Remove(e.StylusDevice.Id, out var polyline))
        {
            foreach (var stylusPoint in e.GetStylusPoints(this))
            {
                polyline.Points.Add(stylusPoint.ToPoint());
            }
        }
    }

    private readonly Dictionary<int/*StylusDeviceId*/, Polyline> _pointCache=new Dictionary<int, Polyline>();
}