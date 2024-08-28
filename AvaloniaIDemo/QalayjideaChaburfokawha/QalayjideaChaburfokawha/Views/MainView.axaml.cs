using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace QalayjideaChaburfokawha.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.PointerReleased += RootGrid_PointerReleased;
    }

    private Polyline? _polyline;

    private void RootGrid_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(RootGrid);
        var position = pointerPoint.Position;

        if (_polyline == null)
        {
            _polyline = new Polyline()
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            _polyline.Points.Add(position);
            RootGrid.Children.Add(_polyline);
        }
        else
        {
            _polyline.Points.Add(position);
        }

        RootGrid.InvalidateVisual();
    }

    private void RootGrid_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _polyline = null;
    }
}
