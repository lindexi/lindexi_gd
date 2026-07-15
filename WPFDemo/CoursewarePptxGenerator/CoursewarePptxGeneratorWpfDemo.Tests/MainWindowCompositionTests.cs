using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using CoursewarePptxGeneratorWpfDemo.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class MainWindowCompositionTests
{
    [TestMethod(DisplayName = "主窗口应直接承载全课件分析页和单页工作台")]
    public void MainWindowShouldHostBothPrototypePages()
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

    [TestMethod(DisplayName = "双页原型往返导航应保留工作台界面状态")]
    [Timeout(60_000)]
    public void PrototypeNavigationShouldPreserveWorkspaceState()
    {
        RunOnStaThreadAsync(PrototypeNavigationShouldPreserveWorkspaceStateAsync).GetAwaiter().GetResult();
    }

    private static async Task PrototypeNavigationShouldPreserveWorkspaceStateAsync()
    {
        EnsureApplicationResources();
        var viewModel = new CoursewareWorkspaceViewModel();
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
            Assert.IsTrue(viewModel.IsWelcome, "应用启动时应显示 Welcome 演示状态。");
            Assert.AreEqual(Visibility.Visible, analysisView.Visibility, "应用启动时应显示分析页。");
            Assert.AreEqual(Visibility.Collapsed, workspaceView.Visibility, "应用启动时不应显示单页工作台。");

            viewModel.ShowDemoStageCommand.Execute(nameof(CoursewareAnalysisStage.AnalysisReady));
            viewModel.SelectedSlide = viewModel.Slides[8];
            viewModel.EnterWorkspaceCommand.Execute(null);
            await PumpDispatcherAsync(window);

            Assert.AreEqual(Visibility.Collapsed, analysisView.Visibility, "进入工作台后应隐藏分析页。");
            Assert.AreEqual(Visibility.Visible, workspaceView.Visibility, "分析完成后应能进入单页工作台。");

            viewModel.BackToAnalysisCommand.Execute(null);
            viewModel.EnterWorkspaceCommand.Execute(null);
            await PumpDispatcherAsync(window);

            Assert.AreSame(viewModel.Slides[8], viewModel.SelectedSlide, "往返导航不应重置工作台当前页面选择。");
        }
        finally
        {
            window.Close();
        }
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