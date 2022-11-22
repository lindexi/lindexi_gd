using PInvoke;

using SharpDX;
using SharpDX.Direct2D1;

using System.Windows;
using System.Windows.Interop;
using SharpDX.Mathematics.Interop;
using D2D = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;
using System.Windows.Media;
using System.Reflection;

namespace LifafaheqearNearkairliraywal;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application application = new Application();
        application.Startup += (s, e) =>
        {
            application.MainWindow.Show();
        };

        Window window = new Window();
        D2DRender render = new D2DRender();
        window.Loaded += (s, e) =>
        {
            render.Init(window);
        };
        
        application.MainWindow=window;
        application.Run();
    }
}

class D2DRender
{
    public void Init(Window window)
    {
        _window = window;

        var factory = new D2D.Factory();

        var pixelFormat = new D2D.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, D2D.AlphaMode.Ignore);

        var renderTargetProperties = new D2D.RenderTargetProperties
        (
              // 默认的行为就是尝试使用硬件优先，否则再使用软件
              D2D.RenderTargetType.Default,
              // 像素格式，对于当前大多数显卡来说，选择 B8G8R8A8 是完全能支持的
              // 而且也方便和其他框架，如 WPF 交互
              pixelFormat,
              dpiX: 96,
              dpiY: 96,
              D2D.RenderTargetUsage.None,
              D2D.FeatureLevel.Level_DEFAULT
        );
        var hwndRenderTargetProperties = new D2D.HwndRenderTargetProperties();
        hwndRenderTargetProperties.Hwnd = new WindowInteropHelper(window).Handle;
        ActualWidth = (int)window.ActualWidth;
        ActualHeight = (int)window.ActualHeight;
        hwndRenderTargetProperties.PixelSize = new Size2(ActualWidth, ActualHeight);

        var renderTarget = new D2D.WindowRenderTarget(factory, renderTargetProperties, hwndRenderTargetProperties);
        _renderTarget = renderTarget;

        window.SizeChanged -= Window_SizeChanged;
        window.SizeChanged += Window_SizeChanged;

        AddRendering();
    }

    private int ActualWidth { set; get; }
    private int ActualHeight { set; get; }

    private void AddRendering()
    {
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
        CompositionTarget.Rendering += CompositionTarget_Rendering;
    }

    public void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        Render();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(_window);
        ArgumentNullException.ThrowIfNull(_renderTarget);

        var window = _window;

        ActualWidth = (int) window.ActualWidth;
        ActualHeight = (int) window.ActualHeight;

        _renderTarget.Resize(new Size2(ActualWidth, ActualHeight));
    }

    public void Render()
    {
        var renderTarget = _renderTarget;
        if (renderTarget == null)
        {
            throw new InvalidOperationException();
        }

        renderTarget.BeginDraw();

        renderTarget.Clear(new RawColor4(0.2f,0.5f,0.5f,1));

        var width = Random.Shared.Next(100, 200);
        var height = width;
        var maxWidth = ActualWidth - width;
        var maxHeight = ActualHeight - height;

        var x = Random.Shared.Next(width, maxWidth);
        var y = Random.Shared.Next(height, maxHeight);

        var ellipse = new D2D.Ellipse(new RawVector2(x, y), width, height);

        using var brush = new D2D.SolidColorBrush(_renderTarget, new RawColor4(1, 0, 0, 1));

        renderTarget.FillEllipse(ellipse, brush);

        renderTarget.EndDraw();
    }

    private D2D.WindowRenderTarget? _renderTarget;
    private Window? _window;
}
