using System.Windows;
using CoursewarePptxGenerator.Core.Analysis;
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

        var slideSummaryService = new CoursewareSlideSummaryService();
        var slidePromptBuilder = new CoursewareSlidePromptBuilder(
            slideSummaryService,
            new CoursewareThemePageDesignAdapter());
        var coursewareFolderLoader = new CoursewareFolderLoader();
        var themeAnalysisSnapshotStore = new CoursewareThemeAnalysisSnapshotStore();
        var workspaceFolderLoader = new CoursewareWorkspaceFolderLoader(
            coursewareFolderLoader,
            themeAnalysisSnapshotStore);

        var mainWindow = new MainWindow
        {
            DataContext = new CoursewareWorkspaceViewModel(
                coursewareFolderLoader,
                WpfViewModelDispatcher.Instance,
                themeAnalysisService: new CoursewareThemeAnalysisService(),
                slideChatManagerFactory: new SlideChatManagerFactory(),
                slideSummaryService,
                slidePromptBuilder,
                themeAnalysisSnapshotStore,
                workspaceFolderLoader),
        };
        mainWindow.Show();
    }
}

