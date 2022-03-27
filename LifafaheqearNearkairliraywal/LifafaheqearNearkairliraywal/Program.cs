using PInvoke;

using SharpDX;

using System.Windows;
using System.Windows.Interop;

using D2D = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;

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
        hwndRenderTargetProperties.PixelSize = new Size2((int) window.ActualWidth, (int) window.ActualHeight);

        var renderTarget = new D2D.WindowRenderTarget(factory, renderTargetProperties, hwndRenderTargetProperties);
    }
}
