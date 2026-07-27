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
        var slidePromptBuilder = new CoursewareSlidePromptBuilder();
        var coursewareFolderLoader = new CoursewareFolderLoader();
        var themeAnalysisSnapshotStore = new CoursewareThemeAnalysisSnapshotStore();
        var workspaceFolderLoader = new CoursewareWorkspaceFolderLoader(
            coursewareFolderLoader,
            themeAnalysisSnapshotStore);
        var themeValidator = new CoursewareThemeValidator(new CoursewareThemeSlideMlValidator());
        var themeAnalysisService = new CoursewareThemeAnalysisService(
            new CoursewareStyleUsageSummaryBuilder(),
            new CoursewareThemeAnalysisPromptBuilder(),
            new CopilotCoursewareThemeAgent(new CopilotChatManagerFactory()),
            themeValidator);

        var mainWindow = new MainWindow
        {
            DataContext = new CoursewareWorkspaceViewModel(
                coursewareFolderLoader,
                WpfViewModelDispatcher.Instance,
                themeAnalysisService,
                slideChatManagerFactory: new SlideChatManagerFactory(),
                slideSummaryService,
                slidePromptBuilder,
                themeAnalysisSnapshotStore,
                workspaceFolderLoader),
        };
        mainWindow.Show();
    }
}

