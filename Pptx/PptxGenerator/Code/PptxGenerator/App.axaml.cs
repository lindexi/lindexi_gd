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

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var chatClient = await Program.CreateChatClientFromAgentConfigAsync().ConfigureAwait(true);
            if (chatClient is null)
            {
                return;
            }

            var slideRenderer = new SlideRenderer();
            var slideGenerationService = new SlideGenerationService(chatClient, slideRenderer);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(slideGenerationService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}