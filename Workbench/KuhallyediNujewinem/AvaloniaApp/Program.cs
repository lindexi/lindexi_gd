using Avalonia;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Threading;

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
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .WithInterFont()
            .LogToTrace();
}
