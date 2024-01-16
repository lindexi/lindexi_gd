using Windows.Foundation;
using Microsoft.UI.Xaml.Input;

using Windows.UI.ViewManagement;

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

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        var currentView = ApplicationView.GetForCurrentView();
        currentView.Title = Random.Shared.Next().ToString();

        // 此方法重新设置窗口的大小是无效的
        currentView.SetPreferredMinSize(new Size(Random.Shared.Next(1000), Random.Shared.Next(1000)));
    }
}
