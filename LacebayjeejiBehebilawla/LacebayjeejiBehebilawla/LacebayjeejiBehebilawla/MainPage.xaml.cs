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
}
