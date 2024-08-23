using Windows.Graphics.Display;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FinayfuweewawWakibawlu;

public class App : Application
{
    public event EventHandler<LaunchActivatedEventArgs>? Launched;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Launched?.Invoke(this, args);
    }
}

internal class Program
{
    [STAThread]
    unsafe static void Main(string[] args)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            // 设置线程同步上下文
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            var app = new App();
            app.Launched += (sender, e) =>
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

                var sharedDevice = CanvasDevice.GetSharedDevice();
                //var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var coreWindow = window.CoreWindow;

                var swapChain = CanvasSwapChain.CreateForCoreWindow(sharedDevice, coreWindow, 96);

                window.Activate();
            };
        });
    }
}
