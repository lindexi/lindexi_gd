using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace PptxGenerator;

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
            var chatClientCreator = new ChatClientCreator();
            var slideRenderer = new SlideRenderer();
            var slideGenerationService = new SlideGenerationService(chatClientCreator, slideRenderer);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(slideGenerationService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}