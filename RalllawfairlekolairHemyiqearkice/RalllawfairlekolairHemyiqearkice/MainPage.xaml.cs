using Windows.Foundation;
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
        var canvas = (Canvas) sender;
        var point = e.GetCurrentPoint(canvas);

        //CornerRadiusRectangleEraserViewManager.MoveEraserVisual(new EraserTouchEventArgs(point.Position.X, point.Position.Y));
        canvas.Clip = new RectangleGeometry()
        {
            Rect = new Rect(10, 10, 300, 300)
        };
        
    }

    private CornerRadiusRectangleEraserViewManager CornerRadiusRectangleEraserViewManager { get; }
}
