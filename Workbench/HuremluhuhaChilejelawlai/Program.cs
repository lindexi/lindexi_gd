using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FinayfuweewawWakibawlu;

public class App : IFrameworkViewSource, IFrameworkView
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
        _coreWindow  = window;
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
}

internal class Program
{
    unsafe static void Main(string[] args)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        CoreApplication.Run(new App());
    }
}
