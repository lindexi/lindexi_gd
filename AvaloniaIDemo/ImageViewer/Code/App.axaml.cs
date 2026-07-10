using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ImageViewer;

public partial class App : Application
{
    private static MainWindow? MainWindowInstance { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowInstance = new MainWindow(desktop.Args);
            desktop.MainWindow = MainWindowInstance;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void OpenFromExternalInstance(string filePath)
    {
        MainWindowInstance?.OpenFromExternalInstance(filePath);
    }
}