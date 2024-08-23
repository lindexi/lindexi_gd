using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
        var swapChain = CanvasSwapChain.CreateForCoreWindow(
            resourceCreator: CanvasDevice.GetSharedDevice(),
            coreWindow: _coreWindow,
            dpi: DisplayInformation.GetForCurrentView().LogicalDpi);
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
            var xamlRoot = window.Content.XamlRoot;

            if (xamlRoot != null)
            {
                var rasterizationScale = xamlRoot.RasterizationScale;
            }

            var currentThread = CoreWindow.GetForCurrentThread();
            var displayInformation = DisplayInformation.GetForCurrentView();
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
