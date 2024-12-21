using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LightTextEditorPlus.AvaloniaDemo.Views;
using MainWindow = LightTextEditorPlus.AvaloniaDemo.Views.MainWindow;

namespace LightTextEditorPlus.AvaloniaDemo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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
