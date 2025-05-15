using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LightTextEditorPlus.AvaloniaDemo.Views;
using MainWindow = LightTextEditorPlus.AvaloniaDemo.Views.MainWindow;

namespace LightTextEditorPlus.AvaloniaDemo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;
    }

    private void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new TextEditorDebugView
            {
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
