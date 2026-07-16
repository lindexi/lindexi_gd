using System.Windows;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Threading;
using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow
        {
            DataContext = new CoursewareWorkspaceViewModel(
                new CoursewareFolderLoader(),
                WpfViewModelDispatcher.Instance,
                themeAnalysisService: new CoursewareThemeAnalysisService()),
        };
        mainWindow.Show();
    }
}

