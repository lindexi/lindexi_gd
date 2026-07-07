using System.Windows;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var slideChatManagerFactory = new SlideChatManagerFactory();
        var slideChatManager = await slideChatManagerFactory.CreateAsync().ConfigureAwait(true);
        var mainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel(slideChatManagerFactory, slideChatManager),
        };
        mainWindow.Show();
    }
}

