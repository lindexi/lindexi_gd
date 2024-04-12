using Avalonia;

using System;
using System.Runtime.InteropServices;

namespace GececurbaiduhaldiFokeejukolu;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
        {
            // 可能跑在麒麟系统上
            var app = new BujeeberehemnaNurgacolarje.X11App();
            app.EnterEraserMode();
            app.Run(IntPtr.Zero);
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
