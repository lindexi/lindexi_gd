using Microsoft.UI.Xaml.Input;

namespace LacebayjeejiBehebilawla;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void Canvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(this);
        HelloTextBlockTransform.X = currentPoint.Position.X;
        HelloTextBlockTransform.Y = currentPoint.Position.Y;
    }

    private void Canvas_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var mouseWheelDelta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        HelloTextBlockScale.ScaleX += mouseWheelDelta / 100.0;
        HelloTextBlockScale.ScaleY += mouseWheelDelta / 100.0;
    }
}
