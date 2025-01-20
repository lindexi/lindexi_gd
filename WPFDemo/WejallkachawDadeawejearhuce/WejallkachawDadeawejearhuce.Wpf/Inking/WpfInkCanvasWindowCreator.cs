using System.Windows;
using WejallkachawDadeawejearhuce.Inking.WpfInking;

namespace WejallkachawDadeawejearhuce.Wpf.Inking;

public class WpfInkCanvasWindowCreator
{
    public IWpfInkCanvasWindow Create()
    {
        if (Application.Current is null)
        {
            var manualResetEvent = new ManualResetEvent(false);
            var thread = new Thread(_ =>
            {
                var app = new App();
                // ReSharper disable once AccessToDisposedClosure
                app.Startup += (_, _) => manualResetEvent.Set();
                app.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            manualResetEvent.WaitOne();
            manualResetEvent.Dispose();
        }

        return Application.Current!.Dispatcher.Invoke(() =>
        {
            var wpfInkCanvasWindow = new WpfInkCanvasWindow();
            return wpfInkCanvasWindow;
        });
    }
}