using Avalonia.Input;
using Avalonia.Media;

using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;
using Window = Avalonia.Controls.Window;

namespace AvaloniaApplication;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isDown = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isDown)
        {
            var currentPoint = e.GetCurrentPoint(this);

            // Keep center
            var positionX = currentPoint.Position.X;

            var translateTransform = (TranslateTransform) RenderTestBorder.RenderTransform!;
            translateTransform.X = positionX;

            _wpfProxy.MoveBorder(positionX);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isDown = false;
    }

    private bool _isDown;

    private readonly WpfProxy _wpfProxy = new WpfProxy();

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        var platformHandle = this.TryGetPlatformHandle()!;
        var avaloniaWindowHandle = platformHandle.Handle;

        _wpfProxy.ShowWpfWindow(avaloniaWindowHandle);
    }
}