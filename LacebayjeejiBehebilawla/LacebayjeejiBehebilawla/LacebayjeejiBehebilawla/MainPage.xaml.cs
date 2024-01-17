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

        DependencyObject? dp = this;

        while (dp != null && !(dp is Window))
        {
            dp = VisualTreeHelper.GetParent(dp);
            // 最顶层就是 Uno.UI.Xaml.Core.RootVisual 了，没有其他咯
        }

        var size = new Size(Random.Shared.Next(200, 1000), Random.Shared.Next(200, 1000));
        WindowHelper.WindowActivator.ResizeMainWindow(size);
    }
}
