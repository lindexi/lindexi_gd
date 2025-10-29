using System;
using System.Threading;
using System.Windows;
using Avalonia;
using Avalonia.ReactiveUI;

using RuhuyagayBemkaijearfear;

using WpfInk;
using WpfApplication = System.Windows.Application;

namespace FebairwemliwoNajojali.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var thread = new Thread(() =>
        {
            var application = new WpfApplication
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
            application.Startup += (sender, args) =>
            {
                var wpfInkWindow = new WpfInkWindow();
                wpfInkWindow.Show();
            };
            application.Run();
        })
        {
            Name = "WpfInkingAcceleratorThread",
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        var appBuilder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
        return appBuilder;
    }
}
