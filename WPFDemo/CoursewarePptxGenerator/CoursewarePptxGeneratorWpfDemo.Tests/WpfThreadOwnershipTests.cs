using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;
using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.Threading;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.Extensions.AI;
using PptxGenerator;
using PptxGenerator.Pipeline;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
[DoNotParallelize]
public sealed class WpfThreadOwnershipTests
{
    [TestMethod(DisplayName = "异步命令应保持 WPF 命令入口的 UI 上下文")]
    [Timeout(60_000)]
    public async Task AsyncRelayCommand_FromWpfCommandEntry_PreservesUiContext()
    {
        var notificationThreadIds = new ConcurrentQueue<int>();
        var executionThreadIds = new ConcurrentQueue<int>();

        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
            var command = new AsyncRelayCommand(async _ =>
            {
                executionThreadIds.Enqueue(Environment.CurrentManagedThreadId);
                await Task.Yield();
                Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
                executionThreadIds.Enqueue(Environment.CurrentManagedThreadId);
            });
            command.CanExecuteChanged += (_, _) =>
            {
                Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
                notificationThreadIds.Enqueue(Environment.CurrentManagedThreadId);
            };

            command.Execute(null);
            await command.ExecutionTask;
        });

        Assert.HasCount(2, notificationThreadIds);
        Assert.HasCount(2, executionThreadIds);
        Assert.AreEqual(1, notificationThreadIds.Concat(executionThreadIds).Distinct().Count());
    }

    [TestMethod(DisplayName = "异步命令异常观察应保持 WPF 命令入口的 UI 上下文")]
    [Timeout(60_000)]
    public async Task AsyncRelayCommand_ExceptionObserver_PreservesUiContext()
    {
        var observedException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        await WpfDispatcher.BackgroundInstance.InvokeAsync(() =>
        {
            var command = new AsyncRelayCommand(
                async _ =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("expected");
                },
                onException: exception =>
                {
                    Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
                    observedException.TrySetResult(exception);
                });

            command.Execute(null);
            return Task.CompletedTask;
        });

        var exception = await observedException.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsInstanceOfType<InvalidOperationException>(exception);
        Assert.AreEqual("expected", exception.Message);
    }

    [TestMethod]
    public async Task StreamingMessages_FromWorkerThread_UpdateCollectionsOnWpfDispatcher()
    {
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        var chatCollectionNotifications = 0;
        var messageItemNotifications = 0;
        var textNotifications = 0;
        var dispatcher = WpfDispatcher.BackgroundInstance;
        var fakeChatClient = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
                StreamResponseAsync("<Page/>", cancellationToken),
        };
        var copilotChatManager = new CopilotChatManager { MainThreadDispatcher = dispatcher };
        copilotChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
            new FakeLanguageModelProvider(fakeChatClient));
        copilotChatManager.ChatMessages.CollectionChanged += OnChatMessagesChanged;
        var renderTool = new SlideMlRenderTool(new FakeSlideMlRenderPipeline(), dispatcher);
        var slideChatManager = new SlideChatManager(copilotChatManager, renderTool);

        await Task.Run(() => slideChatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true));

        Assert.IsEmpty(wrongThreadNotifications);
        Assert.IsGreaterThanOrEqualTo(2, chatCollectionNotifications);
        Assert.IsGreaterThan(0, messageItemNotifications);
        Assert.IsGreaterThan(0, textNotifications);
        return;

        void OnChatMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            VerifyDispatcher("ChatMessages");
            chatCollectionNotifications++;
            if (e.NewItems is null)
            {
                return;
            }

            foreach (CopilotChatMessage message in e.NewItems.OfType<CopilotChatMessage>())
            {
                if (message.Role != ChatRole.Assistant || message.IsPresetInfo)
                {
                    continue;
                }

                message.MessageItems.CollectionChanged += (_, itemArgs) =>
                {
                    VerifyDispatcher("MessageItems");
                    messageItemNotifications++;
                    if (itemArgs.NewItems is null)
                    {
                        return;
                    }

                    foreach (CopilotChatTextItem textItem in itemArgs.NewItems.OfType<CopilotChatTextItem>())
                    {
                        textItem.PropertyChanged += (_, _) =>
                        {
                            VerifyDispatcher("CopilotChatTextItem.PropertyChanged");
                            textNotifications++;
                        };
                    }
                };
            }
        }

        void VerifyDispatcher(string source)
        {
            if (!dispatcher.CheckAccess())
            {
                wrongThreadNotifications.Enqueue(source);
            }
        }
    }

    [TestMethod]
    public async Task Dispose_DuringRuntimeCreation_PreventsLateViewModelCommitOnWpfDispatcher()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "## 元素细节\n\n### 文本.1\n#### 内容\n```\n第一页\n```")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var factory = new IgnoreCancellationSlideChatManagerFactory();
        var dispatcher = new BackgroundWpfViewModelDispatcher();
        var summaryService = new CoursewareSlideSummaryService();
        var item = new CoursewareSlideItemViewModel(
            package.Slides[0],
            summaryService.CreateTitle(package.Slides[0].MarkdownText, package.Slides[0].PageNumber),
            summaryService.CreateSummary(package.Slides[0].MarkdownText),
            factory,
            dispatcher);
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        item.PropertyChanged += (_, e) =>
        {
            if (!WpfDispatcher.BackgroundInstance.CheckAccess())
            {
                wrongThreadNotifications.Enqueue(e.PropertyName ?? string.Empty);
            }
        };

        var runtimeTask = Task.Run(() => item.EnsureRuntimeAsync());
        await factory.CreationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var notificationCountBeforeDispose = wrongThreadNotifications.Count;

        item.Dispose();
        factory.Release.TrySetResult();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await runtimeTask);
        Assert.IsNull(item.Runtime);
        Assert.AreEqual(notificationCountBeforeDispose, wrongThreadNotifications.Count);
        Assert.IsEmpty(wrongThreadNotifications);
    }

    [TestMethod]
    public async Task Dispose_DuringAnalysis_RejectsLateProgressOnWpfDispatcher()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "## 元素细节\n\n### 文本.1\n#### 内容\n```\n第一页\n```")
            .Build();
        var analysisService = new LateReportingThemeAnalysisService();
        var dispatcher = new BackgroundWpfViewModelDispatcher();
        var snapshotRoot = Directory.CreateDirectory(Path.Join(
            Path.GetTempPath(),
            $"WpfThreadOwnershipSnapshot_{Guid.NewGuid():N}"));
        var viewModel = new CoursewareWorkspaceViewModel(
            new CoursewareFolderLoader(),
            dispatcher,
            analysisService,
            new FakeSlideChatManagerFactory(),
            new CoursewareSlideSummaryService(),
            new CoursewareSlidePromptBuilder(),
            new CoursewareThemeAnalysisSnapshotStore(snapshotRoot.FullName));
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        var notificationCount = 0;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (!WpfDispatcher.BackgroundInstance.CheckAccess())
            {
                wrongThreadNotifications.Enqueue(e.PropertyName ?? string.Empty);
            }

            Interlocked.Increment(ref notificationCount);
        };

        var openTask = Task.Run(() => viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName));
        await analysisService.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        viewModel.Dispose();
        var notificationCountAfterDispose = Volatile.Read(ref notificationCount);

        analysisService.Release.TrySetResult();
        await openTask.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(notificationCountAfterDispose, notificationCount);
        Assert.IsEmpty(viewModel.AnalysisEvents);
        Assert.IsEmpty(viewModel.AnalysisChatMessages);
        Assert.IsEmpty(wrongThreadNotifications);
        Assert.IsNull(analysisService.CancellationTokenAccessException);
    }

    [TestMethod(DisplayName = "页面运行时属性应通过 WPF Dispatcher 有序转发")]
    [Timeout(60_000)]
    public async Task SlideRuntimePropertyChanges_FromWorkerThread_AreForwardedOnWpfDispatcher()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "## 元素细节\n\n### 文本.1\n#### 内容\n```\n第一页\n```")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var dispatcher = new BackgroundWpfViewModelDispatcher();
        var summaryService = new CoursewareSlideSummaryService();
        var item = new CoursewareSlideItemViewModel(
            package.Slides[0],
            summaryService.CreateTitle(package.Slides[0].MarkdownText, package.Slides[0].PageNumber),
            summaryService.CreateSummary(package.Slides[0].MarkdownText),
            new FakeSlideChatManagerFactory(),
            dispatcher);
        var runtime = await item.EnsureRuntimeAsync();
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        item.PropertyChanged += (_, e) =>
        {
            if (!WpfDispatcher.BackgroundInstance.CheckAccess())
            {
                wrongThreadNotifications.Enqueue(e.PropertyName ?? string.Empty);
            }
        };

        await Task.Run(() => runtime.SlideChatManager.SlideMlRenderTool.ApplyRenderResultAsync(
            new PptxGenerator.Models.SlideMlRenderResult
            {
                InputXml = "<Page/>",
                OutputXml = "<Page RenderSize=\"1,1\"/>",
                Warnings = Array.Empty<string>(),
                Errors = Array.Empty<string>(),
                PreviewImage = null,
            }));
        await item.WaitForPendingPropertyChangesAsync().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsEmpty(wrongThreadNotifications);
        Assert.AreEqual("<Page/>", item.EditableSlideXml);
        Assert.AreEqual("<Page RenderSize=\"1,1\"/>", item.CallbackXml);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamResponseAsync(
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var character in text)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, character.ToString());
            await Task.Yield();
        }
    }

    private sealed class BackgroundWpfViewModelDispatcher : IViewModelDispatcher
    {
        public Task InvokeAsync(Action action)
        {
            return WpfDispatcher.BackgroundInstance.InvokeAsync(() =>
            {
                action();
                return Task.CompletedTask;
            });
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
            await Release.Task.ConfigureAwait(false);
            return _inner.CreateFallback(options);
        }

        public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
        {
            return _inner.CreateFallback(options);
        }
    }

    private sealed class LateReportingThemeAnalysisService : ICoursewareThemeAnalysisService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Exception? CancellationTokenAccessException { get; private set; }

        public async Task<CoursewarePptxGeneratorWpfDemo.Models.CoursewareThemeAnalysisResult> AnalyzeAsync(
            CoursewarePptxGenerator.Core.Models.CoursewareInputPackage inputPackage,
            IProgress<CoursewarePptxGeneratorWpfDemo.Models.CoursewareAnalysisEvent>? progress = null,
            IProgress<CopilotChatMessage>? messageProgress = null,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Release.Task.ConfigureAwait(false);
            try
            {
                using var registration = cancellationToken.Register(static () => { });
            }
            catch (Exception exception)
            {
                CancellationTokenAccessException = exception;
            }

            progress?.Report(new CoursewarePptxGeneratorWpfDemo.Models.CoursewareAnalysisEvent
            {
                Stage = CoursewarePptxGeneratorWpfDemo.Models.CoursewareAnalysisStage.DesigningTheme,
                Kind = CoursewarePptxGeneratorWpfDemo.Models.CoursewareAnalysisEventKind.Progress,
                State = CoursewarePptxGeneratorWpfDemo.Models.CoursewareAnalysisEventState.Running,
                Title = "late",
                Message = "late",
            });
            messageProgress?.Report(CopilotChatMessage.CreateAssistant("late", isPresetInfo: false));
            return FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(inputPackage);
        }
    }
}
