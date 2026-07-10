using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
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
    private const string SampleDirectoryEnvironmentVariable = "COURSEWARE_PPTX_GENERATOR_SAMPLE_DIR";

    [STATestMethod(DisplayName = "主窗口设置数据上下文后子面板应继承同一个工作台视图模型")]
    [Timeout(60_000)]
    public void MainWindowPanelsShouldInheritMainWindowViewModel()
    {
        var mainWindowXaml = XDocument.Load(Path.Join(GetApplicationProjectDirectory(), "MainWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace views = "clr-namespace:CoursewarePptxGeneratorWpfDemo.Views";

        var rootGrid = mainWindowXaml.Root?.Element(presentation + "Grid");

        Assert.IsNotNull(rootGrid, "主窗口根布局应承载工作区子面板。");
        AssertPanelIsDirectChild(rootGrid, views + "LeftSidebarPanel", "左侧缩略图列表");
        AssertPanelIsDirectChild(rootGrid, views + "MainContentPanel", "打开课件按钮所在面板");
        AssertPanelIsDirectChild(rootGrid, views + "CopilotPanel", "右侧聊天框");
    }

    [TestMethod(DisplayName = "真实窗口打开本机课件文件夹后应显示左侧页面和右侧美化输入")]
    [Timeout(60_000)]
    public void MainWindowShouldShowSlidesAndChatInputAfterOpeningRealCoursewareFolder()
    {
        RunOnStaThreadAsync(MainWindowShouldShowSlidesAndChatInputAfterOpeningRealCoursewareFolderAsync).GetAwaiter().GetResult();
    }

    private static async Task MainWindowShouldShowSlidesAndChatInputAfterOpeningRealCoursewareFolderAsync()
    {
        var sampleDirectory = GetRealCoursewareDirectory();
        if (!sampleDirectory.Exists)
        {
            Assert.Inconclusive($"未找到本机课件样例目录：{sampleDirectory.FullName}");
        }

        EnsureApplicationResources();
        var loader = new CoursewareFolderLoader();
        var expectedPackage = await loader.LoadAsync(sampleDirectory.FullName);
        var chatManagerFactory = new FakeSlideChatManagerFactory();
        var firstChatManager = await chatManagerFactory.CreateAsync();
        var viewModel = new MainWindowViewModel(
            chatManagerFactory,
            firstChatManager,
            loader,
            new CoursewareSlideSummaryService());
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        try
        {
            window.Show();
            await PumpDispatcherAsync(window);

            var mainContentPanel = FindVisualChild<MainContentPanel>(window)
                ?? throw new AssertFailedException("未找到打开课件按钮所在面板。");
            var leftSidebarPanel = FindVisualChild<LeftSidebarPanel>(window)
                ?? throw new AssertFailedException("未找到左侧缩略图面板。");
            var copilotPanel = FindVisualChild<CopilotPanel>(window)
                ?? throw new AssertFailedException("未找到右侧聊天面板。");
            var openButton = FindVisualChildByName<Button>(mainContentPanel, "OpenCoursewareFolderButton")
                ?? throw new AssertFailedException("未找到打开课件按钮。");
            Assert.IsTrue(openButton.IsEnabled, "打开课件按钮初始应可点击。");

            mainContentPanel.SetCoursewareFolderPicker(new FakeCoursewareFolderPicker(sampleDirectory.FullName));
            mainContentPanel.OpenSelectedCoursewareFolder();
            await WaitUntilAsync(() => !viewModel.IsBusy, window, TimeSpan.FromSeconds(10));
            await PumpDispatcherAsync(window);

            var slideListBox = FindVisualChildByName<ListBox>(leftSidebarPanel, "SlideListBox")
                ?? throw new AssertFailedException("未找到左侧页面列表控件。");
            var chatInputTextBox = FindVisualChildByName<TextBox>(copilotPanel, "ChatInputTextBox")
                ?? throw new AssertFailedException("未找到右侧聊天输入框。");
            var attachedImageFilesItemsControl = FindVisualChildByName<ItemsControl>(copilotPanel, "AttachedImageFilesItemsControl")
                ?? throw new AssertFailedException("未找到右侧附件列表控件。");
            var statusTextBlock = FindVisualChildByName<TextBlock>(mainContentPanel, "StatusTextBlock")
                ?? throw new AssertFailedException("未找到工作台状态文本。");

            Assert.AreEqual(expectedPackage.SlideCount, slideListBox.Items.Count, "左侧页面列表数量应与课件页面数量一致。");
            Assert.AreSame(viewModel.SelectedSlide, slideListBox.SelectedItem, "左侧页面列表应选中当前页面。");
            Assert.IsInstanceOfType<CoursewareSlideItem>(slideListBox.Items[0], "左侧页面列表应绑定页面项。");
            var firstSlideItem = (CoursewareSlideItem) slideListBox.Items[0];
            var expectedFirstSlide = expectedPackage.Slides[0];
            Assert.AreEqual(expectedFirstSlide.SlideId, firstSlideItem.SlideId, "左侧第一页 Id 应来自真实课件清单。");
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstSlideItem.Title), "左侧第一页标题不应为空。");
            Assert.IsFalse(string.IsNullOrWhiteSpace(firstSlideItem.Status), "左侧第一页状态不应为空。");
            StringAssert.Contains(chatInputTextBox.Text, expectedFirstSlide.SlideId);
            StringAssert.Contains(chatInputTextBox.Text, expectedFirstSlide.MarkdownText.Trim().Split('\n')[0].Trim());
            Assert.AreEqual(viewModel.AttachedImageFiles.Count, attachedImageFilesItemsControl.Items.Count, "右侧附件显示数量应与 ViewModel 一致。");
            Assert.IsGreaterThan(0, chatInputTextBox.Text.Length, "右侧聊天输入框应显示美化提示词。");
            Assert.AreEqual(viewModel.StatusText, statusTextBlock.Text, "工作台应显示 ViewModel 的全局状态。");
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertPanelIsDirectChild(XElement rootGrid, XName panelName, string displayName)
    {
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        ArgumentNullException.ThrowIfNull(rootGrid);

        var panel = rootGrid.Elements(panelName).SingleOrDefault();
        Assert.IsNotNull(panel, $"主窗口应直接包含{displayName}，否则 DataContext 不会按预期从主窗口继承。");
        Assert.IsNull(panel.Attribute("DataContext"), $"{displayName}不应覆盖主窗口 DataContext。");
        Assert.IsNull(panel.Attribute(xaml + "Name"), $"{displayName}不需要通过名称在代码后置里手动转发 DataContext。");
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

    private static DirectoryInfo GetRealCoursewareDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable(SampleDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return new DirectoryInfo(configuredDirectory);
        }

        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? string.Empty;
        return new DirectoryInfo(Path.Join(systemDrive, Environment.UserName, "Work", "CoursewareMarkdownExport"));
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

    private static async Task WaitUntilAsync(Func<bool> predicate, Window window, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(window);

        var startTime = DateTimeOffset.UtcNow;
        while (!predicate())
        {
            if (DateTimeOffset.UtcNow - startTime > timeout)
            {
                Assert.Fail("等待界面加载课件超时。");
            }

            await Task.Delay(50);
            await window.Dispatcher.InvokeAsync(() => { }).Task;
        }
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

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        ArgumentNullException.ThrowIfNull(parent);

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static T? FindVisualChildByName<T>(DependencyObject parent, string name)
        where T : FrameworkElement
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T { Name: var childName } result && string.Equals(childName, name, StringComparison.Ordinal))
            {
                return result;
            }

            var descendant = FindVisualChildByName<T>(child, name);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
