using Windows.Foundation;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace RalllawfairlekolairHemyiqearkice;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        //CornerRadiusRectangleEraserViewManager = new CornerRadiusRectangleEraserViewManager(CornerRadiusRectangleEraserView);
    }

    private void Canvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        //var canvas = (Canvas) sender;
        //var point = e.GetCurrentPoint(canvas);

        //CornerRadiusRectangleEraserViewManager.MoveEraserVisual(new EraserTouchEventArgs(point.Position.X, point.Position.Y));
    }

    private void FullScreenButton_OnClick(object sender, RoutedEventArgs e)
    {
        var toggleButton = (ToggleButton) sender;
        if (toggleButton.IsChecked is true)
        {
            PlatformHelper.PlatformProvider?.EnterFullScreen();
        }
        else
        {
            PlatformHelper.PlatformProvider?.ExitFullScreen();
        }
    }
}
