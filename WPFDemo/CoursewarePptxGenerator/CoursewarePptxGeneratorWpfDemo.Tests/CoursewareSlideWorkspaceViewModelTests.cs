using System.Runtime.CompilerServices;
using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareSlideWorkspaceViewModelTests
{
    [TestMethod(DisplayName = "真实工作台激活和切页应按需准备页面草稿附件并创建运行时")]
    [Timeout(60_000)]
    public async Task ActivateAndSelectShouldInitializeOnlyVisitedSlides()
    {
        var session = await CreateSessionAsync();
        var factory = new RecordingSlideChatManagerFactory(["<Page/>"]);
        using var workspace = CreateWorkspace(session, factory);

        Assert.AreEqual(0, factory.CreateCount);
        Assert.IsTrue(workspace.Slides.All(slide => slide.Runtime is null));

        await workspace.ActivateAsync();

        Assert.AreEqual(1, factory.CreateCount);
        Assert.IsNotNull(workspace.Slides[0].Runtime);
        Assert.IsNull(workspace.Slides[1].Runtime);
        Assert.IsTrue(workspace.Slides[0].IsInitialPromptPrepared);
        Assert.IsFalse(workspace.Slides[1].IsInitialPromptPrepared);
        StringAssert.Contains(workspace.Slides[0].InputText, CoursewareSlideGenerationEnvelope.CurrentSchemaVersion);
        Assert.HasCount(1, workspace.Slides[0].AttachedImageFiles);
        Assert.AreEqual(CoursewareChatImageAttachmentKind.SourceScreenshot, workspace.Slides[0].AttachedImageFiles[0].Kind);
        Assert.AreEqual(1, workspace.Summary.ReadyCount);
        Assert.AreEqual(1, workspace.Summary.NotStartedCount);

        workspace.SelectedSlide = workspace.Slides[1];
        await workspace.SelectedSlideInitializationTask;

        Assert.AreEqual(2, factory.CreateCount);
        Assert.IsNotNull(workspace.Slides[1].Runtime);
        Assert.IsTrue(workspace.Slides[1].IsInitialPromptPrepared);
        Assert.HasCount(1, workspace.Slides[1].AttachedImageFiles);
        Assert.AreEqual(2, workspace.Summary.ReadyCount);
    }

    [TestMethod(DisplayName = "统一发送应使用可见结构化首轮 Prompt 并复用同一页面会话")]
    [Timeout(60_000)]
    public async Task GenerateAndFollowUpShouldUseStructuredInitialPromptAndSamePageConversation()
    {
        var session = await CreateSessionAsync();
        var factory = new RecordingSlideChatManagerFactory([
            "<Page><TextElement Id=\"title\" Text=\"首次生成\"/></Page>",
            "<TextElement Id=\"title\" Text=\"追问更新\"/>"
        ]);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();
        var slide = workspace.SelectedSlide!;
        var visibleInitialPrompt = slide.InputText;

        await workspace.SendMessageCommand.ExecuteAsync();

        Assert.AreEqual(CoursewareSlideState.Completed, slide.State);
        Assert.AreEqual(CoursewareSlideGenerationState.Completed, slide.GenerationState);
        Assert.IsTrue(slide.HasStartedGenerationConversation);
        Assert.AreEqual(string.Empty, slide.InputText);
        Assert.IsEmpty(slide.AttachedImageFiles);
        StringAssert.Contains(slide.EditableSlideXml, "首次生成");
        Assert.HasCount(1, factory.CapturedRequests);
        var initialRequest = factory.CapturedRequests[0];
        StringAssert.Contains(initialRequest.UserMessage, CoursewareSlideGenerationEnvelope.CurrentSchemaVersion);
        StringAssert.Contains(initialRequest.UserMessage, "slide-first");
        var initialEnvelope = DeserializeWrappedEnvelope(initialRequest.UserMessage);
        Assert.AreEqual("请根据当前页面完整 Markdown、可用的原始截图和全课件主题完成页面美化，保持教学语义准确。", initialEnvelope.Task.UserInstruction);
        StringAssert.Contains(initialRequest.UserMessage, visibleInitialPrompt);
        Assert.AreEqual(1, initialRequest.DataContentCount);
        CollectionAssert.AreEquivalent(
            new[] { "get_slide_state", "get_slide_preview" },
            initialRequest.ToolNames.ToArray());

        slide.InputText = "把标题改得更简洁";
        await workspace.SendMessageCommand.ExecuteAsync();

        Assert.AreEqual(CoursewareSlideState.Completed, slide.State);
        Assert.HasCount(2, factory.CapturedRequests);
        var followUpRequest = factory.CapturedRequests[1];
        Assert.AreEqual("把标题改得更简洁", followUpRequest.UserMessage);
        Assert.IsFalse(followUpRequest.UserMessage.Contains(CoursewareSlideGenerationEnvelope.CurrentSchemaVersion, StringComparison.Ordinal));
        Assert.AreEqual(0, followUpRequest.DataContentCount);
        Assert.AreEqual(1, factory.CreateCount);
        Assert.IsGreaterThanOrEqualTo(4, slide.CopilotChatManager?.ChatMessages.Count ?? 0);
    }

    [TestMethod(DisplayName = "首轮发送取消后应保留可见草稿附件并允许同页重试")]
    [Timeout(60_000)]
    public async Task CancelGenerationShouldKeepInputAndAllowRetry()
    {
        var session = await CreateSessionAsync();
        var factory = new RecordingSlideChatManagerFactory(
            ["<Page><TextElement Id=\"retry\" Text=\"重试成功\"/></Page>"],
            blockFirstRequest: true);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();
        var slide = workspace.SelectedSlide!;
        var initialPrompt = slide.InputText;
        var generationTask = workspace.SendMessageCommand.ExecuteAsync();
        await factory.FirstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        workspace.CancelSelectedSlideCommand.Execute(null);
        await generationTask;

        Assert.AreEqual(CoursewareSlideState.Canceled, slide.State);
        Assert.AreEqual(CoursewareSlideGenerationState.Canceled, slide.GenerationState);
        Assert.AreEqual(CoursewareScreenshotAttachmentState.Attached, slide.ScreenshotAttachmentState);
        Assert.AreEqual(initialPrompt, slide.InputText);
        Assert.HasCount(1, slide.AttachedImageFiles);
        Assert.IsFalse(slide.HasStartedGenerationConversation);
        Assert.IsTrue(workspace.SendMessageCommand.CanExecute(null));

        await workspace.SendMessageCommand.ExecuteAsync();

        Assert.AreEqual(CoursewareSlideState.Completed, slide.State);
        Assert.IsEmpty(slide.AttachedImageFiles);
        StringAssert.Contains(slide.EditableSlideXml, "重试成功");
    }

    [TestMethod(DisplayName = "发送期间修改输入时成功回调应保留新草稿")]
    [Timeout(60_000)]
    public async Task SuccessfulSendShouldPreserveInputEditedAfterRequestSnapshot()
    {
        var session = await CreateSessionAsync();
        var factory = new RecordingSlideChatManagerFactory(
            ["<Page><TextElement Id=\"done\" Text=\"完成\"/></Page>"],
            blockFirstRequest: true);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();
        var slide = workspace.SelectedSlide!;
        var sendTask = workspace.SendMessageCommand.ExecuteAsync();
        await factory.FirstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        slide.InputText = "发送期间的新草稿";
        factory.ReleaseFirstRequest();
        await sendTask;

        Assert.AreEqual(CoursewareSlideState.Completed, slide.State);
        Assert.AreEqual("发送期间的新草稿", slide.InputText);
        Assert.IsTrue(slide.HasStartedGenerationConversation);
    }

    [TestMethod(DisplayName = "切换页面不应取消仍在生成的原页面任务")]
    [Timeout(60_000)]
    public async Task SelectingAnotherSlideShouldNotCancelRunningGeneration()
    {
        var session = await CreateSessionAsync();
        var factory = new RecordingSlideChatManagerFactory(
            ["<Page><TextElement Id=\"first\" Text=\"第一页完成\"/></Page>"],
            blockFirstRequest: true);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();
        var firstSlide = workspace.Slides[0];
        var generationTask = workspace.SendMessageCommand.ExecuteAsync();
        await factory.FirstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        workspace.SelectedSlide = workspace.Slides[1];
        await workspace.SelectedSlideInitializationTask;

        Assert.AreEqual(CoursewareSlideState.Generating, firstSlide.State);
        Assert.IsFalse(generationTask.IsCompleted);

        firstSlide.CancelActiveOperation();
        await generationTask;
        Assert.AreEqual(CoursewareSlideState.Canceled, firstSlide.State);
    }

    [TestMethod(DisplayName = "不同页面应能并发生成且取消只作用于当前选中页")]
    [Timeout(60_000)]
    public async Task DifferentSlidesShouldGenerateConcurrentlyAndCancelSelectedSlideOnly()
    {
        var session = await CreateSessionAsync();
        var factory = new RecordingSlideChatManagerFactory(
            ["<Page><TextElement Id=\"second\" Text=\"第二页完成\"/></Page>"],
            blockFirstRequest: true);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();
        var firstSlide = workspace.Slides[0];
        var secondSlide = workspace.Slides[1];
        var firstGenerationTask = workspace.SendMessageCommand.ExecuteAsync(firstSlide);
        await factory.FirstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        workspace.SelectedSlide = secondSlide;
        await workspace.SelectedSlideInitializationTask;
        Assert.IsTrue(workspace.SendMessageCommand.CanExecute(secondSlide));

        await workspace.SendMessageCommand.ExecuteAsync(secondSlide);

        Assert.AreEqual(CoursewareSlideState.Completed, secondSlide.State);
        StringAssert.Contains(secondSlide.EditableSlideXml, "第二页完成");
        Assert.AreEqual(CoursewareSlideState.Generating, firstSlide.State);

        workspace.CancelSelectedSlideCommand.Execute(null);
        Assert.AreEqual(CoursewareSlideState.Completed, secondSlide.State);
        Assert.IsFalse(firstGenerationTask.IsCompleted);

        firstSlide.CancelActiveOperation();
        await firstGenerationTask;
        Assert.AreEqual(CoursewareSlideState.Canceled, firstSlide.State);
    }

    [TestMethod(DisplayName = "未重新渲染的手工 SlideML 应阻止统一发送")]
    [Timeout(60_000)]
    public async Task UnsavedSlideXmlShouldBlockGenerationAndFollowUp()
    {
        var session = await CreateSessionAsync();
        var factory = new RecordingSlideChatManagerFactory(["<Page/>"]);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();
        var slide = workspace.SelectedSlide!;
        slide.EditableSlideXml = "<Page/>";
        slide.InputText = "继续美化";

        Assert.IsFalse(workspace.SendMessageCommand.CanExecute(slide));
        Assert.IsTrue(workspace.RerenderCommand.CanExecute(slide));
    }

    [TestMethod(DisplayName = "无截图页面首次生成应明确标记截图缺失并继续生成")]
    [Timeout(60_000)]
    public async Task GenerateWithoutSourceScreenshotShouldContinueWithFileMissingState()
    {
        var builder = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页", "正文"), hasScreenshot: false);
        var package = await new CoursewareFolderLoader().LoadAsync(builder.Build().FullName);
        var session = new CoursewareWorkspaceSession(package)
        {
            ThemeAnalysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package),
        };
        var factory = new RecordingSlideChatManagerFactory(["<Page/>"]);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();

        await workspace.SendMessageCommand.ExecuteAsync();

        Assert.AreEqual(CoursewareSlideState.Completed, workspace.SelectedSlide!.State);
        Assert.AreEqual(CoursewareScreenshotAttachmentState.FileMissing, workspace.SelectedSlide.ScreenshotAttachmentState);
        Assert.AreEqual(0, factory.CapturedRequests.Single().DataContentCount);
    }

    [TestMethod(DisplayName = "模型不可用时工作台应禁用 AI 生成但允许本地重新渲染")]
    [Timeout(60_000)]
    public async Task FallbackRuntimeShouldDisableAiGenerationAndAllowRerender()
    {
        var session = await CreateSessionAsync();
        using var workspace = CreateWorkspace(session, new FailingSlideChatManagerFactory());
        await workspace.ActivateAsync();
        var slide = workspace.SelectedSlide!;

        Assert.IsFalse(slide.IsAiGenerationAvailable);
        Assert.IsFalse(workspace.SendMessageCommand.CanExecute(null));
        slide.EditableSlideXml = "<Page/>";
        Assert.IsTrue(workspace.RerenderCommand.CanExecute(null));

        await workspace.RerenderCommand.ExecuteAsync();

        Assert.AreEqual(CoursewareSlideState.Completed, slide.State);
        Assert.AreEqual("<Page/>", slide.CallbackXml);
    }

    [TestMethod(DisplayName = "工作台汇总应反映跨页完成失败和未开始状态")]
    [Timeout(60_000)]
    public async Task SummaryShouldTrackRealSlideStates()
    {
        var session = await CreateSessionAsync(slideCount: 3);
        var factory = new RecordingSlideChatManagerFactory([
            "<Page><TextElement Id=\"ok\" Text=\"完成\"/></Page>",
            string.Empty
        ]);
        using var workspace = CreateWorkspace(session, factory);
        await workspace.ActivateAsync();
        await workspace.SendMessageCommand.ExecuteAsync();
        workspace.SelectedSlide = workspace.Slides[1];
        await workspace.SelectedSlideInitializationTask;
        await workspace.SendMessageCommand.ExecuteAsync();

        Assert.AreEqual(3, workspace.Summary.TotalCount);
        Assert.AreEqual(1, workspace.Summary.CompletedCount);
        Assert.AreEqual(1, workspace.Summary.FailedCount);
        Assert.AreEqual(1, workspace.Summary.NotStartedCount);
        StringAssert.Contains(workspace.SummaryText, "已完成 1 / 3");
    }

    private static CoursewareSlideWorkspaceViewModel CreateWorkspace(
        CoursewareWorkspaceSession session,
        ISlideChatManagerFactory factory)
    {
        return new CoursewareSlideWorkspaceViewModel(
            session,
            factory,
            new CoursewareSlidePromptBuilder(
                new CoursewareSlideSummaryService(),
                new CoursewareThemePageDesignAdapter()),
            new CoursewareSlideSummaryService(),
            new ImmediateViewModelDispatcher());
    }

    private static async Task<CoursewareWorkspaceSession> CreateSessionAsync(int slideCount = 2)
    {
        var builder = new TestCoursewareExportBuilder();
        for (var index = 0; index < slideCount; index++)
        {
            builder.AddSlide(
                $"slide-{index switch { 0 => "first", 1 => "second", _ => $"{index + 1}" }}",
                CreateSlideMarkdown($"第 {index + 1} 页", $"第 {index + 1} 页正文"));
        }

        var package = await new CoursewareFolderLoader().LoadAsync(builder.Build().FullName);
        return new CoursewareWorkspaceSession(package)
        {
            ThemeAnalysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package),
        };
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }

    private static CoursewareSlideGenerationEnvelope DeserializeWrappedEnvelope(string wrappedPrompt)
    {
        var jsonStart = wrappedPrompt.IndexOf('{');
        if (jsonStart < 0)
        {
            throw new AssertFailedException("未能从 SlideML 包装提示词中提取页面 JSON 信封。");
        }

        var reader = new System.Text.Json.Utf8JsonReader(
            System.Text.Encoding.UTF8.GetBytes(wrappedPrompt[jsonStart..]));
        using var document = System.Text.Json.JsonDocument.ParseValue(ref reader);
        return System.Text.Json.JsonSerializer.Deserialize(
                   document.RootElement.GetRawText(),
                   CoursewareSlideGenerationJsonSerializerContext.Default.CoursewareSlideGenerationEnvelope)
               ?? throw new AssertFailedException("页面 JSON 信封不能为空。");
    }

    private sealed class RecordingSlideChatManagerFactory : ISlideChatManagerFactory
    {
        private readonly Queue<string> _responses;
        private readonly bool _blockFirstRequest;
        private readonly TaskCompletionSource _firstRequestRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requestIndex;

        public RecordingSlideChatManagerFactory(
            IEnumerable<string> responses,
            bool blockFirstRequest = false)
        {
            _responses = new Queue<string>(responses);
            _blockFirstRequest = blockFirstRequest;
        }

        public int CreateCount { get; private set; }

        public List<CapturedRequest> CapturedRequests { get; } = [];

        public TaskCompletionSource FirstRequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReleaseFirstRequest()
        {
            _firstRequestRelease.TrySetResult();
        }

        public Task<SlideChatManager> CreateAsync(
            SlideChatManagerFactoryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCount++;
            return Task.FromResult(CreateManager(options));
        }

        public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
        {
            return CreateManager(options);
        }

        private SlideChatManager CreateManager(SlideChatManagerFactoryOptions? options)
        {
            var fakeChatClient = new FakeChatClient
            {
                OnGetStreamingResponseAsync = CaptureAndStreamAsync,
                OnGetResponseAsync = (_, _, _) => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, "<Page/>"))),
            };
            var chatManager = new CopilotChatManager();
            var model = new FakeLanguageModel(fakeChatClient)
            {
                ModelDefinition = new ModelDefinition
                {
                    Provider = "test",
                    ModelName = "page-test-model",
                    ContextWindowSize = 100_000,
                    MaxOutputTokens = 8_000,
                },
            };
            chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([model]));
            var dispatcher = new FakeMainThreadDispatcher();
            var renderTool = new SlideMlRenderTool(new FakeSlideMlRenderPipeline(), dispatcher);
            return new SlideChatManager(
                chatManager,
                renderTool,
                slideDocumentContext: options?.DocumentContext ?? new SlideDocumentContext());
        }

        private async IAsyncEnumerable<ChatResponseUpdate> CaptureAndStreamAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var messageList = messages.ToList();
            var userMessage = messageList.Last(message => message.Role == ChatRole.User);
            CapturedRequests.Add(new CapturedRequest(
                userMessage.Text,
                userMessage.Contents.OfType<DataContent>().Count(),
                options?.Tools?.Select(tool => tool.Name).ToArray() ?? []));
            var requestIndex = Interlocked.Increment(ref _requestIndex);
            if (_blockFirstRequest && requestIndex == 1)
            {
                FirstRequestStarted.TrySetResult();
                await _firstRequestRelease.Task.WaitAsync(cancellationToken);
            }

            var response = _responses.Count > 0 ? _responses.Dequeue() : "<Page/>";
            if (!string.IsNullOrEmpty(response))
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, response);
            }
        }
    }

    private sealed record CapturedRequest(
        string UserMessage,
        int DataContentCount,
        IReadOnlyList<string> ToolNames);
}
