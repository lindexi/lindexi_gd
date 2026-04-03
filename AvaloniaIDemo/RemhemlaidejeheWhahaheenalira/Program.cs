using Avalonia;

using System;

namespace RemhemlaidejeheWhahaheenalira;

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
        // System.PlatformNotSupportedException:“Operation is not supported on this platform.”
        var app = new App();

        return AppBuilder.Configure<App>(() => app)
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
