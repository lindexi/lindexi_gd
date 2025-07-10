using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Markup.Xaml;

using KarhelearkuDemkunalhaw.Views;

namespace KarhelearkuDemkunalhaw;

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
            desktop.ShowMainWindowAtStartup = false;
            desktop.MainWindow = new MainWindow
            {
            };

            var embeddableControlRoot = new EmbeddableControlRoot();
            var mainView = new MainView
            {
            };
            embeddableControlRoot.Content = mainView;
            mainView.Loaded += (sender, args) =>
            {

            };
            embeddableControlRoot.Prepare(); // 调用此方法会触发 Loaded 事件
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
