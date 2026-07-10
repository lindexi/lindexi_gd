using Avalonia;
using System;

namespace ImageViewer;

class Program
{
    private static SingleInstanceCoordinator? _singleInstanceCoordinator;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (SingleInstanceCoordinator.TrySendToExistingInstance(args))
        {
            return;
        }

        _singleInstanceCoordinator = new SingleInstanceCoordinator();
        _singleInstanceCoordinator.TryStart(path => App.OpenFromExternalInstance(path));

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            _singleInstanceCoordinator.Dispose();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
