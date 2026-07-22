using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareSlideItemViewModelTests
{
    [TestMethod(DisplayName = "真实页面项应延迟创建运行时且并发初始化只执行一次")]
    [Timeout(60_000)]
    public async Task EnsureRuntimeAsyncShouldCreateRuntimeLazilyAndDeduplicateConcurrentCalls()
    {
        var package = await LoadPackageAsync();
        var factory = new CountingSlideChatManagerFactory();
        var item = CreateItem(package.Slides[0], factory);

        Assert.IsNull(item.Runtime);
        Assert.AreEqual(CoursewareSlideState.NotStarted, item.State);
        Assert.IsEmpty(item.AttachedImageFiles);
        Assert.AreEqual(CoursewareScreenshotAttachmentState.NotPrepared, item.ScreenshotAttachmentState);
        Assert.AreEqual(CoursewareSlideRuntimeState.NotCreated, item.RuntimeState);

        var firstTask = item.EnsureRuntimeAsync();
        var secondTask = item.EnsureRuntimeAsync();
        var runtimes = await Task.WhenAll(firstTask, secondTask);

        Assert.AreSame(runtimes[0], runtimes[1]);
        Assert.AreSame(runtimes[0], item.Runtime);
        Assert.AreEqual(1, factory.CreateCount);
        Assert.AreEqual(CoursewareSlideState.Ready, item.State);
        Assert.AreEqual(CoursewareSlideRuntimeState.Ready, item.RuntimeState);
        Assert.IsTrue(item.IsAiGenerationAvailable);
        Assert.AreEqual(1280, item.CanvasWidth);
        Assert.AreEqual(720, item.CanvasHeight);
    }

    [TestMethod(DisplayName = "模型初始化失败时页面项应保留同画布 fallback 并禁用 AI 生成")]
    [Timeout(60_000)]
    public async Task EnsureRuntimeAsyncShouldUseFallbackAndExposeInitializationError()
    {
        var package = await LoadPackageAsync();
        var item = CreateItem(package.Slides[0], new FailingSlideChatManagerFactory());

        var runtime = await item.EnsureRuntimeAsync();

        Assert.IsFalse(runtime.IsAiGenerationAvailable);
        Assert.IsFalse(item.IsAiGenerationAvailable);
        Assert.IsNull(item.SelectedModelItem);
        Assert.AreEqual(CoursewareSlideState.Ready, item.State);
        Assert.AreEqual(CoursewareSlideRuntimeState.ModelUnavailable, item.RuntimeState);
        StringAssert.Contains(item.ErrorMessage, "聊天管理器初始化失败");
        StringAssert.Contains(runtime.SlideChatManager.Pipeline.PromptProvider.BuildInitialUserPrompt("测试"), "1280x720");
    }

    [TestMethod(DisplayName = "两个真实页面项的输入附件和执行状态应互不串扰")]
    [Timeout(60_000)]
    public async Task SlideItemsShouldKeepIndependentObservableState()
    {
        var package = await LoadPackageAsync();
        var first = CreateItem(package.Slides[0], new FakeSlideChatManagerFactory());
        var second = CreateItem(package.Slides[1], new FakeSlideChatManagerFactory());

        await first.EnsureRuntimeAsync();
        first.InputText = "第一页追问";
        first.EditableSlideXml = "<Page/>";
        first.AttachedImageFiles.Clear();

        Assert.AreEqual("第一页追问", first.InputText);
        Assert.AreEqual(string.Empty, second.InputText);
        Assert.AreEqual("<Page/>", first.EditableSlideXml);
        Assert.AreEqual(string.Empty, second.EditableSlideXml);
        Assert.AreEqual(CoursewareSlideState.Ready, first.State);
        Assert.AreEqual(CoursewareSlideState.NotStarted, second.State);
        Assert.IsEmpty(first.AttachedImageFiles);
        Assert.IsEmpty(second.AttachedImageFiles);
    }

    [TestMethod(DisplayName = "页面运行时初始化取消后应进入已取消状态并允许重试")]
    [Timeout(60_000)]
    public async Task EnsureRuntimeAsyncShouldBecomeCanceledAndAllowRetry()
    {
        var package = await LoadPackageAsync();
        var factory = new CancelThenSucceedSlideChatManagerFactory();
        var item = CreateItem(package.Slides[0], factory);
        using var cancellationTokenSource = new CancellationTokenSource();
        var firstTask = item.EnsureRuntimeAsync(cancellationTokenSource.Token);
        await factory.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        cancellationTokenSource.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => firstTask);

        Assert.AreEqual(CoursewareSlideState.Canceled, item.State);
        Assert.AreEqual(CoursewareSlideRuntimeState.Canceled, item.RuntimeState);
        var runtime = await item.EnsureRuntimeAsync();
        Assert.IsNotNull(runtime);
        Assert.AreEqual(2, factory.CreateCount);
        Assert.AreEqual(CoursewareSlideState.Ready, item.State);
    }

    [TestMethod(DisplayName = "页面释放后迟到的运行时不得重新挂接")]
    [Timeout(60_000)]
    public async Task DisposeShouldRejectLateRuntimeCreationResult()
    {
        var package = await LoadPackageAsync();
        var factory = new IgnoreCancellationSlideChatManagerFactory();
        var item = CreateItem(package.Slides[0], factory);
        var runtimeTask = item.EnsureRuntimeAsync();
        await factory.CreationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        item.Dispose();
        factory.Release.TrySetResult();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => runtimeTask);
        Assert.IsNull(item.Runtime);
    }

    private static CoursewareSlideItemViewModel CreateItem(
        CoursewarePptxGenerator.Core.Models.CoursewareSlideInput input,
        ISlideChatManagerFactory factory)
    {
        var summaryService = new CoursewareSlideSummaryService();
        return new CoursewareSlideItemViewModel(
            input,
            summaryService.CreateTitle(input.MarkdownText, input.PageNumber),
            summaryService.CreateSummary(input.MarkdownText),
            factory,
            new ImmediateViewModelDispatcher());
    }

    private static async Task<CoursewarePptxGenerator.Core.Models.CoursewareInputPackage> LoadPackageAsync()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "第一页正文"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页", "第二页正文"))
            .Build();
        return await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }

    private sealed class CountingSlideChatManagerFactory : ISlideChatManagerFactory
    {
        private readonly FakeSlideChatManagerFactory _inner = new();

        public int CreateCount { get; private set; }

        public Task<SlideChatManager> CreateAsync(
            SlideChatManagerFactoryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CreateCount++;
            return _inner.CreateAsync(options, cancellationToken);
        }

        public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
        {
            return _inner.CreateFallback(options);
        }
    }

    private sealed class CancelThenSucceedSlideChatManagerFactory : ISlideChatManagerFactory
    {
        private readonly FakeSlideChatManagerFactory _inner = new();

        public int CreateCount { get; private set; }

        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<SlideChatManager> CreateAsync(
            SlideChatManagerFactoryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CreateCount++;
            if (CreateCount == 1)
            {
                FirstCallStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return await _inner.CreateAsync(options, cancellationToken);
        }

        public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
        {
            return _inner.CreateFallback(options);
        }
    }

    private sealed class IgnoreCancellationSlideChatManagerFactory : ISlideChatManagerFactory
    {
        private readonly FakeSlideChatManagerFactory _inner = new();

        public TaskCompletionSource CreationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<SlideChatManager> CreateAsync(
            SlideChatManagerFactoryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CreationStarted.TrySetResult();
            await Release.Task;
            return _inner.CreateFallback(options);
        }

        public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
        {
            return _inner.CreateFallback(options);
        }
    }
}
