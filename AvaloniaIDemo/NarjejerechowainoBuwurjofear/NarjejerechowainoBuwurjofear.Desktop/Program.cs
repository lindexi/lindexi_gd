using System;
using System.IO;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

namespace NarjejerechowainoBuwurjofear.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var streamReader = new StreamReader(Stream.Null);
        if (streamReader.EndOfStream)
        {
            
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
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
