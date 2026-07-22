using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using CoursewarePptxGeneratorWpfDemo.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class MainWindowCompositionTests
{
    [TestMethod(DisplayName = "主窗口应直接承载全课件分析页和真实单页工作台")]
    [Timeout(60_000)]
    public void MainWindowShouldHostAnalysisAndWorkspacePages()
    {
        var mainWindowXaml = XDocument.Load(Path.Join(GetApplicationProjectDirectory(), "MainWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace views = "clr-namespace:CoursewarePptxGeneratorWpfDemo.Views";

        var rootGrid = mainWindowXaml.Root?.Element(presentation + "Grid");

        Assert.IsNotNull(rootGrid, "主窗口根布局应承载双页视图。");
        Assert.IsNotNull(rootGrid.Element(views + "CoursewareAnalysisView"), "主窗口应直接承载全课件分析页。");
        Assert.IsNotNull(rootGrid.Element(views + "SlideWorkspaceView"), "主窗口应直接承载单页工作台。");
    }

    [TestMethod(DisplayName = "首页打开课件按钮应调用真实文件夹选择事件")]
    [Timeout(60_000)]
    public void WelcomeOpenCoursewareButtonShouldUseFolderPickerClickHandler()
    {
        var analysisViewXaml = XDocument.Load(Path.Join(GetApplicationProjectDirectory(), "Views", "CoursewareAnalysisView.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var openButton = analysisViewXaml
            .Descendants(presentation + "Button")
            .Single(element => string.Equals((string?) element.Attribute(x + "Name"), "OpenCoursewareButton", StringComparison.Ordinal));

        Assert.AreEqual("OpenCoursewareButton_OnClick", (string?) openButton.Attribute("Click"));
        Assert.IsNull(openButton.Attribute("Command"), "首页按钮不应直接执行缺少文件夹路径的命令。");
    }

    [TestMethod(DisplayName = "已加载课件后应删除第二次打开入口并保留失败或取消后的工作台入口")]
    [Timeout(60_000)]
    public void LoadedCoursewareViewsShouldRemoveReplacementAndPreserveWorkspaceActions()
    {
        var analysisViewXaml = XDocument.Load(Path.Join(GetApplicationProjectDirectory(), "Views", "CoursewareAnalysisView.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var folderPickerButtons = analysisViewXaml
            .Descendants(presentation + "Button")
            .Where(element => string.Equals((string?) element.Attribute("Click"), "OpenCoursewareButton_OnClick", StringComparison.Ordinal))
            .ToArray();
        Assert.HasCount(2, folderPickerButtons, "文件夹选择入口只应存在于欢迎页和加载失败页。");

        var preservedWorkspaceButtons = analysisViewXaml
            .Descendants(presentation + "Button")
            .Where(element => string.Equals((string?) element.Attribute("Command"), "{Binding EnterWorkspaceCommand}", StringComparison.Ordinal))
            .ToArray();
        Assert.IsGreaterThanOrEqualTo(3, preservedWorkspaceButtons.Length);
        Assert.IsFalse(preservedWorkspaceButtons.Any(button => button.Attribute("IsEnabled") is not null),
            "进入工作台按钮应由命令 CanExecute 决定，避免重新分析失败或取消后错误禁用。");
    }

    [TestMethod(DisplayName = "分析结果与工作台应明确标记当前能力边界")]
    [Timeout(60_000)]
    public void AnalysisAndWorkspaceViewsShouldDescribeCapabilityBoundary()
    {
        var projectDirectory = GetApplicationProjectDirectory();
        var analysisView = XDocument.Load(Path.Join(projectDirectory, "Views", "CoursewareAnalysisView.xaml"));
        var themeView = XDocument.Load(Path.Join(projectDirectory, "Views", "CoursewareThemeSummaryView.xaml"));
        var workspaceView = XDocument.Load(Path.Join(projectDirectory, "Views", "SlideWorkspaceView.xaml"));
        var combinedXaml = string.Concat(
            analysisView.ToString(SaveOptions.DisableFormatting),
            themeView.ToString(SaveOptions.DisableFormatting),
            workspaceView.ToString(SaveOptions.DisableFormatting));

        StringAssert.Contains(combinedXaml, "WorkspaceButtonText");
        StringAssert.Contains(combinedXaml, "ThemeSuggestionWarningText");
        StringAssert.Contains(combinedXaml, "DesignSystemCapabilityText");
        StringAssert.Contains(combinedXaml, "TemplateValidationCapabilityText");
        StringAssert.Contains(combinedXaml, "VisualAnalysisCapabilityText");
        StringAssert.Contains(combinedXaml, "PageGenerationCapabilityText");
        Assert.IsFalse(combinedXaml.Contains("演示数据", StringComparison.Ordinal));
        Assert.IsFalse(combinedXaml.Contains("工作台原型", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "真实工作台应由左中右三个面板绑定统一页面工作台")]
    [Timeout(60_000)]
    public void SlideWorkspaceShouldComposeRealThreeColumnPanels()
    {
        var projectDirectory = GetApplicationProjectDirectory();
        var workspaceXaml = XDocument.Load(Path.Join(projectDirectory, "Views", "SlideWorkspaceView.xaml"));
        var sidebarXaml = XDocument.Load(Path.Join(projectDirectory, "Views", "LeftSidebarPanel.xaml"));
        var contentXaml = XDocument.Load(Path.Join(projectDirectory, "Views", "MainContentPanel.xaml"));
        var copilotXaml = XDocument.Load(Path.Join(projectDirectory, "Views", "CopilotPanel.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace views = "clr-namespace:CoursewarePptxGeneratorWpfDemo.Views";

        Assert.IsNotNull(workspaceXaml.Descendants(views + "LeftSidebarPanel").SingleOrDefault());
        Assert.IsNotNull(workspaceXaml.Descendants(views + "MainContentPanel").SingleOrDefault());
        Assert.IsNotNull(workspaceXaml.Descendants(views + "CopilotPanel").SingleOrDefault());
        StringAssert.Contains(sidebarXaml.ToString(SaveOptions.DisableFormatting), "CoursewareSlideItemViewModel");
        StringAssert.Contains(sidebarXaml.ToString(SaveOptions.DisableFormatting), "PreviewImage");
        StringAssert.Contains(contentXaml.ToString(SaveOptions.DisableFormatting), "SelectedSlide.CanvasWidth");
        StringAssert.Contains(contentXaml.ToString(SaveOptions.DisableFormatting), "SelectedSlide.CanvasHeight");
        StringAssert.Contains(contentXaml.ToString(SaveOptions.DisableFormatting), "SelectedSlide.EditableSlideXml");
        StringAssert.Contains(contentXaml.ToString(SaveOptions.DisableFormatting), "IsReadOnly=\"{Binding SelectedSlide.IsBusy}\"");
        StringAssert.Contains(copilotXaml.ToString(SaveOptions.DisableFormatting), "SelectedSlide.CopilotChatManager.ChatMessages");
        StringAssert.Contains(copilotXaml.ToString(SaveOptions.DisableFormatting), "GenerateSelectedSlideCommand");
        StringAssert.Contains(copilotXaml.ToString(SaveOptions.DisableFormatting), "CommandParameter=\"{Binding SelectedSlide}\"");
        Assert.IsFalse(contentXaml.Descendants(presentation + "Button")
            .Any(button => string.Equals((string?) button.Attribute("Content"), "打开课件文件夹", StringComparison.Ordinal)));
        Assert.IsFalse(sidebarXaml.ToString(SaveOptions.DisableFormatting).Contains("添加空页面", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "主题分析失败页应绑定真实错误和技术详情")]
    [Timeout(60_000)]
    public void AnalysisFailureViewShouldBindRealErrorAndTechnicalDetails()
    {
        var analysisViewXaml = XDocument.Load(Path.Join(GetApplicationProjectDirectory(), "Views", "CoursewareAnalysisView.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var failureView = analysisViewXaml
            .Descendants(presentation + "ScrollViewer")
            .Single(element => element
                .Descendants(presentation + "DataTrigger")
                .Any(trigger => string.Equals((string?) trigger.Attribute("Binding"), "{Binding IsAnalysisFailed}", StringComparison.Ordinal)));
        var errorMessage = failureView
            .Descendants(presentation + "TextBlock")
            .Single(element => ((string?) element.Attribute("Text"))?.Contains("LoadErrorMessage", StringComparison.Ordinal) == true);
        var errorDetails = failureView
            .Descendants(presentation + "TextBox")
            .Single(element => ((string?) element.Attribute("Text"))?.Contains("LoadErrorDetails", StringComparison.Ordinal) == true);

        Assert.IsNotNull(errorMessage);
        Assert.IsNotNull(errorDetails);
        Assert.IsFalse(analysisViewXaml.ToString(SaveOptions.DisableFormatting).Contains("THEME_VALIDATION_FAILED", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "双页真实工作台往返导航应保留页面状态")]
    [Timeout(60_000)]
    public void WorkspaceNavigationShouldPreserveWorkspaceState()
    {
        RunOnStaThreadAsync(WorkspaceNavigationShouldPreserveWorkspaceStateAsync).GetAwaiter().GetResult();
    }

    private static async Task WorkspaceNavigationShouldPreserveWorkspaceStateAsync()
    {
        EnsureApplicationResources();
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "第一页正文"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页", "第二页正文"))
            .Build();
        var summaryService = new CoursewareSlideSummaryService();
        var viewModel = new CoursewareWorkspaceViewModel(
            new CoursewareFolderLoader(),
            new ImmediateViewModelDispatcher(),
            new FakeCoursewareThemeAnalysisService(),
            new FakeSlideChatManagerFactory(),
            summaryService,
            new CoursewareSlidePromptBuilder(summaryService, new CoursewareThemePageDesignAdapter()));
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        try
        {
            window.Show();
            await PumpDispatcherAsync(window);

            var analysisView = window.FindName("AnalysisView") as CoursewareAnalysisView;
            var workspaceView = window.FindName("WorkspaceView") as SlideWorkspaceView;
            Assert.IsNotNull(analysisView, "未找到全课件分析页。");
            Assert.IsNotNull(workspaceView, "未找到单页工作台。");
            Assert.IsTrue(viewModel.IsCoursewareWelcome, "应用启动时应显示课件欢迎状态。");
            Assert.AreEqual(Visibility.Visible, analysisView.Visibility, "应用启动时应显示分析页。");
            Assert.AreEqual(Visibility.Collapsed, workspaceView.Visibility, "应用启动时不应显示单页工作台。");

            await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
            viewModel.SlideWorkspace!.SelectedSlide = viewModel.SlideWorkspace.Slides[1];
            viewModel.SlideWorkspace.SelectedSlide.InputText = "保留输入";
            await viewModel.EnterWorkspaceCommand.ExecuteAsync();
            await PumpDispatcherAsync(window);

            Assert.AreEqual(Visibility.Collapsed, analysisView.Visibility, "进入工作台后应隐藏分析页。");
            Assert.AreEqual(Visibility.Visible, workspaceView.Visibility, "分析完成后应能进入单页工作台。");

            viewModel.BackToAnalysisCommand.Execute(null);
            await viewModel.EnterWorkspaceCommand.ExecuteAsync();
            await PumpDispatcherAsync(window);

            Assert.AreSame(viewModel.SlideWorkspace.Slides[1], viewModel.SlideWorkspace.SelectedSlide, "往返导航不应重置真实工作台当前页面选择。");
            Assert.AreEqual("保留输入", viewModel.SlideWorkspace.SelectedSlide.InputText);
        }
        finally
        {
            window.Close();
        }
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }

    private static string GetApplicationProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Join(
                directory.FullName,
                "Pptx",
                "PptxGenerator",
                "Code",
                "CoursewarePptxGeneratorWpfDemo",
                "MainWindow.xaml");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("未找到 Pptx/PptxGenerator/Code/CoursewarePptxGeneratorWpfDemo/MainWindow.xaml。");
    }

    private static void EnsureApplicationResources()
    {
        var application = Application.Current;
        if (application?.Resources.Contains("TextPrimaryBrush") == true)
        {
            return;
        }

        application ??= new App();
        ((App) application).InitializeComponent();
    }

    private static async Task PumpDispatcherAsync(Window window)
    {
        await window.Dispatcher.InvokeAsync(() => { }).Task;
        await Task.Delay(50);
        await window.Dispatcher.InvokeAsync(() => { }).Task;
    }

    private static Task RunOnStaThreadAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            _ = action().ContinueWith(task =>
            {
                if (task.Exception is not null)
                {
                    taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCanceled)
                {
                    taskCompletionSource.TrySetCanceled();
                }
                else
                {
                    taskCompletionSource.TrySetResult();
                }

                dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return taskCompletionSource.Task;
    }
}