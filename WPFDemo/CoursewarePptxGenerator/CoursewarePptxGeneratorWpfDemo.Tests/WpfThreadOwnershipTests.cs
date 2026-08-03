using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;
using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.Threading;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.Extensions.AI;
using PptxGenerator;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;

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
    public async Task StreamingMessages_FromWpfDispatcher_PreserveUiOwnership()
    {
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        var chatCollectionNotifications = 0;
        var messageItemNotifications = 0;
        var textNotifications = 0;
        var dispatcher = WpfDispatcher.BackgroundInstance;
        await dispatcher.InvokeAsync(async () =>
        {
            var fakeChatClient = new FakeChatClient
            {
                OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
                    StreamResponseAsync("<Page/>", cancellationToken),
            };
            var copilotChatManager = new CopilotChatManager();
            copilotChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
                new FakeLanguageModelProvider(fakeChatClient));
            copilotChatManager.ChatMessages.CollectionChanged += OnChatMessagesChanged;
            var renderTool = new SlideMlRenderTool(new FakeSlideMlRenderPipeline(), dispatcher);
            var slideChatManager = new SlideChatManager(copilotChatManager, renderTool);

            await slideChatManager.SendMessageAsync(
                "生成页面",
                isFirstMessage: true,
                attachPreview: false,
                useStreaming: true);
        });

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
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            var factory = new IgnoreCancellationSlideChatManagerFactory();
            var threadAccess = new BackgroundWpfViewModelThreadAccess();
            var summaryService = new CoursewareSlideSummaryService();
            var item = new CoursewareSlideItemViewModel(
                package.Slides[0],
                summaryService.CreateTitle(package.Slides[0].MarkdownText, package.Slides[0].PageNumber),
                summaryService.CreateSummary(package.Slides[0].MarkdownText),
                factory,
                threadAccess);
            item.PropertyChanged += (_, e) =>
            {
                if (!WpfDispatcher.BackgroundInstance.CheckAccess())
                {
                    wrongThreadNotifications.Enqueue(e.PropertyName ?? string.Empty);
                }
            };

            var runtimeTask = item.EnsureRuntimeAsync();
            await factory.CreationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var notificationCountBeforeDispose = wrongThreadNotifications.Count;

            item.Dispose();
            factory.Release.TrySetResult();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await runtimeTask);
            Assert.IsNull(item.Runtime);
            Assert.AreEqual(notificationCountBeforeDispose, wrongThreadNotifications.Count);
        });
        Assert.IsEmpty(wrongThreadNotifications);
    }

    [TestMethod]
    public async Task Dispose_DuringAnalysis_RejectsLateProgressOnWpfDispatcher()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "## 元素细节\n\n### 文本.1\n#### 内容\n```\n第一页\n```")
            .Build();
        var snapshotRoot = Directory.CreateDirectory(Path.Join(
            Path.GetTempPath(),
            $"WpfThreadOwnershipSnapshot_{Guid.NewGuid():N}"));
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        var notificationCount = 0;
        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            var analysisService = new LateReportingThemeAnalysisService();
            var threadAccess = new BackgroundWpfViewModelThreadAccess();
            var viewModel = new CoursewareWorkspaceViewModel(
                new CoursewareFolderLoader(),
                threadAccess,
                analysisService,
                new FakeSlideChatManagerFactory(),
                new CoursewareSlideSummaryService(),
                new CoursewareSlidePromptBuilder(),
                new CoursewareThemeAnalysisSnapshotStore(snapshotRoot.FullName));
            viewModel.PropertyChanged += (_, e) =>
            {
                if (!WpfDispatcher.BackgroundInstance.CheckAccess())
                {
                    wrongThreadNotifications.Enqueue(e.PropertyName ?? string.Empty);
                }

                notificationCount++;
            };

            var openTask = viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
            await analysisService.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            viewModel.Dispose();
            var notificationCountAfterDispose = notificationCount;

            analysisService.Release.TrySetResult();
            await openTask.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.AreEqual(notificationCountAfterDispose, notificationCount);
            Assert.IsEmpty(viewModel.AnalysisEvents);
            Assert.IsEmpty(viewModel.AnalysisChatMessages);
            Assert.IsNull(analysisService.CancellationTokenAccessException);
        });
        Assert.IsEmpty(wrongThreadNotifications);
    }

    [TestMethod(DisplayName = "页面运行时属性应通过 WPF Dispatcher 有序转发")]
    [Timeout(60_000)]
    public async Task SlideRuntimePropertyChanges_FromWorkerThread_AreForwardedOnWpfDispatcher()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "## 元素细节\n\n### 文本.1\n#### 内容\n```\n第一页\n```")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            var threadAccess = new BackgroundWpfViewModelThreadAccess();
            var summaryService = new CoursewareSlideSummaryService();
            using var item = new CoursewareSlideItemViewModel(
                package.Slides[0],
                summaryService.CreateTitle(package.Slides[0].MarkdownText, package.Slides[0].PageNumber),
                summaryService.CreateSummary(package.Slides[0].MarkdownText),
                new WorkerCompletingSlideChatManagerFactory(blockResponse: false),
                threadAccess);
            var runtime = await item.EnsureRuntimeAsync();
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

            Assert.AreEqual("<Page/>", item.EditableSlideXml);
            Assert.AreEqual("<Page RenderSize=\"1,1\"/>", item.CallbackXml);
        });
        Assert.IsEmpty(wrongThreadNotifications);
    }

    [TestMethod(DisplayName = "页面 ViewModel 的工作线程入口应立即失败而不是自动调度")]
    [Timeout(60_000)]
    public async Task SlideViewModelEntry_FromWorkerThread_FailsFast()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "## 元素细节\n\n### 文本.1\n#### 内容\n```\n第一页\n```")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        CoursewareSlideItemViewModel? item = null;
        await WpfDispatcher.BackgroundInstance.InvokeAsync(() =>
        {
            var summaryService = new CoursewareSlideSummaryService();
            item = new CoursewareSlideItemViewModel(
                package.Slides[0],
                summaryService.CreateTitle(package.Slides[0].MarkdownText, package.Slides[0].PageNumber),
                summaryService.CreateSummary(package.Slides[0].MarkdownText),
                new FakeSlideChatManagerFactory(),
                new BackgroundWpfViewModelThreadAccess());
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => item!.EnsureRuntimeAsync()));

        await WpfDispatcher.BackgroundInstance.InvokeAsync(() =>
        {
            item!.Dispose();
            return Task.CompletedTask;
        });
    }

    [TestMethod(DisplayName = "页面发送完成时所有 ViewModel、集合和命令通知都应保持 WPF Dispatcher 所有权")]
    [Timeout(60_000)]
    public async Task SendPageMessage_WorkerCompletion_CommitsCompleteUiLifecycleOnWpfDispatcher()
    {
        var session = await CreateWorkspaceSessionAsync();
        var factory = new WorkerCompletingSlideChatManagerFactory(blockResponse: false);
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        var pagePropertyNotifications = 0;
        var workspacePropertyNotifications = 0;
        var attachmentNotifications = 0;
        var chatCollectionNotifications = 0;
        var commandNotifications = 0;

        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            using var workspace = CreateWorkspace(session, factory);
            var slide = workspace.SelectedSlide!;
            workspace.PropertyChanged += (_, e) => VerifyDispatcher($"Workspace.{e.PropertyName}");
            slide.PropertyChanged += (_, e) =>
            {
                VerifyDispatcher($"Slide.{e.PropertyName}");
                pagePropertyNotifications++;
            };
            slide.AttachedImageFiles.CollectionChanged += (_, _) =>
            {
                VerifyDispatcher("AttachedImageFiles");
                attachmentNotifications++;
            };
            workspace.SendMessageCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
            workspace.RerenderCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
            workspace.CancelSelectedSlideCommand.CanExecuteChanged += OnCommandCanExecuteChanged;

            await workspace.ActivateAsync();
            slide.CopilotChatManager!.ChatMessages.CollectionChanged += (_, _) =>
            {
                VerifyDispatcher("ChatMessages");
                chatCollectionNotifications++;
            };

            workspace.SendMessageCommand.Execute(slide);
            await workspace.SendMessageCommand.ExecutionTask;

            Assert.AreEqual(CoursewareSlideState.Completed, slide.State);
            Assert.AreEqual(CoursewareSlideGenerationState.Completed, slide.GenerationState);
            Assert.IsFalse(slide.IsBusy);
            Assert.IsFalse(
                workspace.SendMessageCommand.CanExecute(slide),
                "成功后输入已被消费，发送命令应因空草稿禁用。");
            Assert.IsEmpty(slide.AttachedImageFiles);
            Assert.AreEqual(1, workspace.Summary.CompletedCount);
            return;

            void OnCommandCanExecuteChanged(object? sender, EventArgs e)
            {
                VerifyDispatcher(sender?.GetType().Name ?? "Command");
                commandNotifications++;
            }

            void VerifyDispatcher(string source)
            {
                if (!WpfDispatcher.BackgroundInstance.CheckAccess())
                {
                    wrongThreadNotifications.Enqueue(source);
                }

                workspacePropertyNotifications++;
            }
        });

        Assert.IsEmpty(wrongThreadNotifications);
        Assert.IsGreaterThan(0, pagePropertyNotifications);
        Assert.IsGreaterThan(0, workspacePropertyNotifications);
        Assert.IsGreaterThanOrEqualTo(2, attachmentNotifications);
        Assert.IsGreaterThanOrEqualTo(2, chatCollectionNotifications);
        Assert.IsGreaterThan(0, commandNotifications);
    }

    [TestMethod(DisplayName = "页面发送取消时最终状态和命令恢复都应在 WPF Dispatcher 完成")]
    [Timeout(60_000)]
    public async Task CancelPageMessage_WorkerCancellation_CommitsCompleteUiLifecycleOnWpfDispatcher()
    {
        var session = await CreateWorkspaceSessionAsync();
        var factory = new WorkerCompletingSlideChatManagerFactory(blockResponse: true);
        var wrongThreadNotifications = new ConcurrentQueue<string>();
        var commandNotifications = 0;

        await WpfDispatcher.BackgroundInstance.InvokeAsync(async () =>
        {
            using var workspace = CreateWorkspace(session, factory);
            await workspace.ActivateAsync();
            var slide = workspace.SelectedSlide!;
            var initialPrompt = slide.InputText;
            slide.PropertyChanged += (_, e) => VerifyDispatcher($"Slide.{e.PropertyName}");
            workspace.PropertyChanged += (_, e) => VerifyDispatcher($"Workspace.{e.PropertyName}");
            slide.AttachedImageFiles.CollectionChanged += (_, _) => VerifyDispatcher("AttachedImageFiles");
            workspace.SendMessageCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
            workspace.RerenderCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
            workspace.CancelSelectedSlideCommand.CanExecuteChanged += OnCommandCanExecuteChanged;

            workspace.SendMessageCommand.Execute(slide);
            await factory.ResponseStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            workspace.CancelSelectedSlideCommand.Execute(null);
            await workspace.SendMessageCommand.ExecutionTask;

            Assert.AreEqual(CoursewareSlideState.Canceled, slide.State);
            Assert.AreEqual(CoursewareSlideGenerationState.Canceled, slide.GenerationState);
            Assert.IsFalse(slide.IsBusy);
            Assert.AreEqual(initialPrompt, slide.InputText);
            Assert.HasCount(1, slide.AttachedImageFiles);
            Assert.IsTrue(workspace.SendMessageCommand.CanExecute(slide));
            return;

            void OnCommandCanExecuteChanged(object? sender, EventArgs e)
            {
                VerifyDispatcher(sender?.GetType().Name ?? "Command");
                commandNotifications++;
            }

            void VerifyDispatcher(string source)
            {
                if (!WpfDispatcher.BackgroundInstance.CheckAccess())
                {
                    wrongThreadNotifications.Enqueue(source);
                }
            }
        });

        Assert.IsEmpty(wrongThreadNotifications);
        Assert.IsGreaterThan(0, commandNotifications);
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

    private static CoursewareSlideWorkspaceViewModel CreateWorkspace(
        CoursewareWorkspaceSession session,
        ISlideChatManagerFactory factory)
    {
        return new CoursewareSlideWorkspaceViewModel(
            session,
            factory,
            new CoursewareSlidePromptBuilder(),
            new CoursewareSlideSummaryService(),
            new BackgroundWpfViewModelThreadAccess(),
            new SuccessfulImageAttachmentLoader());
    }

    private static async Task<CoursewareWorkspaceSession> CreateWorkspaceSessionAsync()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "## 元素细节\n\n### 文本.1\n#### 内容\n```\n第一页\n```")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        return new CoursewareWorkspaceSession(package)
        {
            ThemeAnalysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package),
        };
    }

    private sealed class BackgroundWpfViewModelThreadAccess : IViewModelThreadAccess
    {
        public bool CheckAccess() => WpfDispatcher.BackgroundInstance.CheckAccess();
    }

    private sealed class SuccessfulImageAttachmentLoader : ICoursewareImageAttachmentLoader
    {
        public Task<IReadOnlyList<DataContent>> LoadAsync(
            IReadOnlyList<CoursewareChatImageAttachmentViewModel> attachments,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<DataContent> contents = attachments
                .Select(_ => new DataContent(new byte[] { 1 }, "image/png"))
                .ToArray();
            return Task.FromResult(contents);
        }
    }

    private sealed class WorkerCompletingSlideChatManagerFactory(bool blockResponse) : ISlideChatManagerFactory
    {
        private readonly bool _blockResponse = blockResponse;

        public TaskCompletionSource ResponseStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SlideChatManager> CreateAsync(
            SlideChatManagerFactoryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateFallback(options));
        }

        public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
        {
            var fakeChatClient = new FakeChatClient
            {
                OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
                    StreamWorkerResponseAsync(cancellationToken),
                OnGetResponseAsync = (_, _, _) => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, "<Page/>"))),
            };
            var copilotChatManager = new CopilotChatManager();
            copilotChatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
                new FakeLanguageModelProvider(fakeChatClient));
            var documentContext = options?.DocumentContext ?? new SlideDocumentContext();
            var renderTool = new SlideMlRenderTool(
                new FakeSlideMlRenderPipeline(),
                WpfDispatcher.BackgroundInstance);
            var promptProvider = new SlideMlPromptProvider(documentContext);
            promptProvider.UpdatePrompts(null, null, streamingUserPromptTemplate: "{USER_INPUT}");
            return new SlideChatManager(
                copilotChatManager,
                renderTool,
                promptProvider: promptProvider,
                slideDocumentContext: documentContext);
        }

        private async IAsyncEnumerable<ChatResponseUpdate> StreamWorkerResponseAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ResponseStarted.TrySetResult();
            await Task.Delay(_blockResponse ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(20), cancellationToken)
                .ConfigureAwait(false);
            yield return new ChatResponseUpdate(ChatRole.Assistant, "<Page/>");
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
