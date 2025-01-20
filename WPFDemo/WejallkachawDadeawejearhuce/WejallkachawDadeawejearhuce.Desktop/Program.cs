using System;
using System.IO;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using WejallkachawDadeawejearhuce.Inking.WpfInking;
using WejallkachawDadeawejearhuce.Wpf.Inking;

namespace WejallkachawDadeawejearhuce.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var wpfInkCanvasWindowCreator = new WpfInkCanvasWindowCreator();
        WpfInkCanvasProvider.RegisterWpfInkCanvasWindowCreator(wpfInkCanvasWindowCreator.Create);

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new DispatcherOptions()
            {
                InputStarvationTimeout = TimeSpan.FromDays(1),
                // 修复笔迹延迟
                // https://github.com/AvaloniaUI/Avalonia/pull/16896
                InstantRendering = true,
            })
            .LogToTrace()
            .UseReactiveUI();
    }
}
