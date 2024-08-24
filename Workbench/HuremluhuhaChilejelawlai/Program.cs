using System.Numerics;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.DirectX;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;

namespace FinayfuweewawWakibawlu;

public class App : Application, IFrameworkViewSource, IFrameworkView
{
    public IFrameworkView CreateView()
    {
        return this;
    }

    private CoreApplicationView? _applicationView;
    private CoreWindow? _coreWindow;

    public void Initialize(CoreApplicationView applicationView)
    {
        _applicationView = applicationView;
    }

    public void SetWindow(CoreWindow window)
    {
        _coreWindow = window;
    }

    public void Load(string entryPoint)
    {
    }

    public void Run()
    {
        //var swapChain = CanvasSwapChain.CreateForCoreWindow(
        //    resourceCreator: CanvasDevice.GetSharedDevice(),
        //    coreWindow: _coreWindow,
        //    dpi: DisplayInformation.GetForCurrentView().LogicalDpi);
    }

    public void Uninitialize()
    {
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new Window()
        {
            Title = "控制台创建应用"
        };
        window.Content = new Grid()
        {
            Children =
            {
                new TextBlock()
                {
                    Text = "控制台应用",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        window.Activated += (sender, eventArgs) =>
        {
            var canvasDevice = new CanvasDevice();

            var compositor = window.Compositor;

            var compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, canvasDevice);

            var compositionDrawingSurface = compositionGraphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(200, 200),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
            using (CanvasDrawingSession? drawingSession =
                   CanvasComposition.CreateDrawingSession(compositionDrawingSurface))
            {
                drawingSession.FillRectangle(new Rect(10, 10, 100, 100),
                    Windows.UI.Color.FromArgb(0xFF, 0x56, 0x56, 0x56));
            }

            // 在 Win2d 渲染到平面完成之后，将这个平面作为一个画刷用于在之后的效果
            CompositionSurfaceBrush surfaceBrush = compositor.CreateSurfaceBrush(compositionDrawingSurface);

            SpriteVisual visual = compositor.CreateSpriteVisual();
            visual.Brush = surfaceBrush;
            visual.Size = new Vector2(200, 200);
            visual.Offset = new Vector3(20, 20, 0);

            Visual elementVisual = ElementCompositionPreview.GetElementVisual(window.Content);
            if (elementVisual is ContainerVisual containerVisual)
            {
                containerVisual.Children.InsertAtTop(visual);
            }
        };

        window.Activate();

        base.OnLaunched(args);
    }
}

internal class Program
{
    unsafe static void Main(string[] args)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        global::Microsoft.UI.Xaml.Application.Start(p =>
        {
            var app = new App();
            //CoreApplication.Run(app);
        });
    }
}