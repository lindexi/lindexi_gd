using Avalonia;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace AvaloniaApp;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        //Task.Run(() =>
        //{
        //    while (true)
        //    {
        //        Console.ReadLine();

        //        Dispatcher.UIThread.InvokeAsync(() =>
        //        {
        //            var mainWindow = new MainWindow();
        //            mainWindow.Show();
        //        });
        //    }
        //});

        LinuxDockerEnvironmentHelper.EnsureX11Ready();

        IsStarted = true;

        //Task.Run(async () =>
        //{
        //    var appManager = new AppManager();
        //    var imageFile = await appManager.TakeAsync();
        //    Process.Start("explorer.exe", [imageFile]);
        //});

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static bool IsStarted { get; set; }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new FontManagerOptions()
            {
                DefaultFamilyName = "Noto Sans CJK SC",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = "文泉驿正黑" },
                    new FontFallback { FontFamily = "DejaVu Sans" },
                ],
            });
}