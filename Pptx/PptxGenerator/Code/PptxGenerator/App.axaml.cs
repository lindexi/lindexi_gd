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
            var slideChatManager = await Program.CreateSlideChatManagerAsync().ConfigureAwait(true);
            if (slideChatManager is null)
            {
                return;
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(slideChatManager)
            };

            desktop.MainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}