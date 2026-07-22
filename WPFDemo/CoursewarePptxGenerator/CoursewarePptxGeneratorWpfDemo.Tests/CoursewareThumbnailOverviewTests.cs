using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using CoursewarePptxGeneratorWpfDemo.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThumbnailOverviewTests
{
    [TestMethod(DisplayName = "第一张可用截图为四比三时应统一使用四比三外框")]
    [Timeout(60_000)]
    public void WhenRepresentativeScreenshotIsStandardThenUseStandardFrame()
    {
        RunOnStaThreadAsync(async () =>
        {
            EnsureApplicationResources();
            var screenshotPath = CreateScreenshot(1024, 768);
            var viewModel = new CoursewareWorkspaceViewModel();
            var view = new CoursewareAnalysisView
            {
                DataContext = viewModel,
            };
            var window = new Window
            {
                Content = view,
            };

            try
            {
                window.Show();
                viewModel.CoursewareThumbnails.Add(CreateThumbnail(1, null));
                viewModel.CoursewareThumbnails.Add(CreateThumbnail(2, screenshotPath));
                await WaitForThumbnailAspectRatioAsync(view, ThumbnailAspectRatio.Standard, window);

                Assert.AreEqual(ThumbnailAspectRatio.Standard, view.ThumbnailAspectRatio);
            }
            finally
            {
                window.Close();
            }
        }).GetAwaiter().GetResult();
    }

    [TestMethod(DisplayName = "第一张非空截图无效时应保持十六比九且不使用后续截图")]
    [Timeout(60_000)]
    public void WhenRepresentativeScreenshotIsInvalidThenKeepWidescreenFrame()
    {
        RunOnStaThreadAsync(async () =>
        {
            EnsureApplicationResources();
            var validStandardScreenshotPath = CreateScreenshot(1024, 768);
            var missingScreenshotPath = Path.Join(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");
            var viewModel = new CoursewareWorkspaceViewModel();
            var view = new CoursewareAnalysisView
            {
                DataContext = viewModel,
            };
            var window = new Window
            {
                Content = view,
            };

            try
            {
                window.Show();
                viewModel.CoursewareThumbnails.Add(CreateThumbnail(1, missingScreenshotPath));
                viewModel.CoursewareThumbnails.Add(CreateThumbnail(2, validStandardScreenshotPath));
                await PumpDispatcherAsync(window);

                Assert.AreEqual(ThumbnailAspectRatio.Widescreen, view.ThumbnailAspectRatio);
            }
            finally
            {
                window.Close();
            }
        }).GetAwaiter().GetResult();
    }

    [TestMethod(DisplayName = "缩略图模板应由共享比例和主题资源决定尺寸")]
    public void ThumbnailTemplateShouldUseSharedAspectRatioAndThemeDimensions()
    {
        var projectDirectory = GetApplicationProjectDirectory();
        var overviewXaml = XDocument.Load(Path.Join(projectDirectory, "Views", "CoursewareThumbnailOverview.xaml"));
        var analysisXaml = XDocument.Load(Path.Join(projectDirectory, "Views", "CoursewareAnalysisView.xaml"));
        var resourcesXaml = XDocument.Load(Path.Join(projectDirectory, "Resources", "CoursewareAnalysisStyles.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace views = "clr-namespace:CoursewarePptxGeneratorWpfDemo.Views";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var image = overviewXaml.Descendants(presentation + "Image").Single();
        var aspectRatioTrigger = overviewXaml.Descendants(presentation + "DataTrigger")
            .Single(element => ((string?) element.Attribute("Binding"))?.Contains("ThumbnailAspectRatio", StringComparison.Ordinal) == true);
        var overviews = analysisXaml.Descendants(views + "CoursewareThumbnailOverview").ToList();
        var resourceKeys = resourcesXaml.Root?.Elements()
            .Select(element => (string?) element.Attribute(x + "Key"))
            .Where(key => key is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.AreEqual("Uniform", (string?) image.Attribute("Stretch"));
        Assert.AreEqual("{x:Static views:ThumbnailAspectRatio.Standard}", (string?) aspectRatioTrigger.Attribute("Value"));
        Assert.AreEqual(3, overviews.Count);
        Assert.IsTrue(overviews.All(element => ((string?) element.Attribute("ThumbnailAspectRatio"))?.Contains("CoursewareAnalysisView", StringComparison.Ordinal) == true));
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "CoursewareThumbnailFrameWidth",
                "CoursewareWidescreenThumbnailFrameHeight",
                "CoursewareStandardThumbnailFrameHeight",
            },
            resourceKeys?.ToList());
    }

    private static CoursewareThumbnailItemViewModel CreateThumbnail(int pageNumber, string? screenshotFilePath) => new()
    {
        PageNumber = pageNumber,
        SlideId = $"slide-{pageNumber}",
        Width = 1920,
        Height = 1080,
        ScreenshotFilePath = screenshotFilePath,
    };

    private static string CreateScreenshot(int pixelWidth, int pixelHeight)
    {
        var directoryPath = Path.Join(Path.GetTempPath(), "CoursewarePptxGeneratorWpfDemo.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        var filePath = Path.Join(directoryPath, "screenshot.png");
        var pixels = new byte[pixelWidth * pixelHeight * 4];
        var bitmap = BitmapSource.Create(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, pixelWidth * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(filePath);
        encoder.Save(stream);
        return filePath;
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
                "CoursewarePptxGeneratorWpfDemo.csproj");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("未找到 CoursewarePptxGeneratorWpfDemo 项目目录。");
    }

    private static void EnsureApplicationResources()
    {
        var application = Application.Current;
        if (application?.Resources.Contains("AnalysisPageBackgroundBrush") == true)
        {
            return;
        }

        application ??= new App();
        ((App) application).InitializeComponent();
    }

    private static async Task PumpDispatcherAsync(Window window)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
    }

    private static async Task WaitForThumbnailAspectRatioAsync(
        CoursewareAnalysisView view,
        ThumbnailAspectRatio expected,
        Window window)
    {
        var timeoutAt = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (view.ThumbnailAspectRatio != expected && DateTime.UtcNow < timeoutAt)
        {
            await PumpDispatcherAsync(window);
            await Task.Delay(10);
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
}
