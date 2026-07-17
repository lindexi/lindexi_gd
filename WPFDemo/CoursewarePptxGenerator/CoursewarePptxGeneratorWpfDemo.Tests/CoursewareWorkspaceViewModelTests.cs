using System.IO;
using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareWorkspaceViewModelTests
{
    [TestMethod(DisplayName = "打开课件文件夹后应加载真实缩略图并自动完成主题分析")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldLoadRealThumbnailsAndAnalyzeThemeAutomatically()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页标题", "第二页内容"), hasScreenshot: false)
            .Build();
        var analysisService = new FakeCoursewareThemeAnalysisService();
        var viewModel = CreateViewModel(analysisService);

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.AreEqual(CoursewareWorkspaceState.AnalysisReady, viewModel.WorkspaceState);
        Assert.AreEqual(1, analysisService.AnalysisCount);
        Assert.IsNotNull(viewModel.CoursewareSession);
        Assert.IsNotNull(viewModel.CoursewareSession.ThemeAnalysisResult);
        Assert.AreEqual("测试主题", viewModel.ThemeTitle);
        Assert.HasCount(5, viewModel.ThemeColors);
        Assert.HasCount(4, viewModel.TypographyLevels);
        Assert.HasCount(3, viewModel.StyleKeywords);
        Assert.HasCount(4, viewModel.PageTypeRecommendations);
        Assert.AreEqual("测试课件", viewModel.CoursewareTitle);
        Assert.HasCount(2, viewModel.CoursewareThumbnails);
        Assert.AreEqual("slide-first", viewModel.CoursewareThumbnails[0].SlideId);
        Assert.AreEqual("第 1 页", viewModel.CoursewareThumbnails[0].AccessibleName);
        Assert.IsTrue(viewModel.CoursewareThumbnails[0].HasScreenshot);
        Assert.AreEqual("slide-second", viewModel.CoursewareThumbnails[1].SlideId);
        Assert.IsFalse(viewModel.CoursewareThumbnails[1].HasScreenshot);
        Assert.IsTrue(viewModel.CoursewareThumbnails[1].HasWarning);
    }

    [TestMethod(DisplayName = "主题分析期间应固定显示分析对话并拒绝选择结果")]
    [Timeout(60_000)]
    public async Task AnalyzingThemeShouldKeepConversationTabSelected()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var analysisStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseAnalysis = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var analysisService = new FakeCoursewareThemeAnalysisService(async (inputPackage, _, _, cancellationToken) =>
        {
            analysisStarted.TrySetResult();
            await releaseAnalysis.Task.WaitAsync(cancellationToken);
            return FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(inputPackage);
        });
        var viewModel = CreateViewModel(analysisService);
        var analysisTask = viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        await analysisStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        try
        {
            Assert.AreEqual(CoursewareAnalysisTab.Conversation, viewModel.SelectedAnalysisTab);
            Assert.AreEqual(0, viewModel.SelectedAnalysisTabIndex);

            viewModel.SelectedAnalysisTab = CoursewareAnalysisTab.ThemeResult;
            viewModel.SelectedAnalysisTabIndex = 1;

            Assert.AreEqual(CoursewareAnalysisTab.Conversation, viewModel.SelectedAnalysisTab);
            Assert.AreEqual(0, viewModel.SelectedAnalysisTabIndex);
        }
        finally
        {
            releaseAnalysis.TrySetResult();
            await analysisTask;
        }
    }

    [TestMethod(DisplayName = "主题分析完成后应自动显示结果并允许切回对话")]
    [Timeout(60_000)]
    public async Task CompletedThemeAnalysisShouldSelectResultAndAllowConversationSelection()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var viewModel = CreateViewModel(new FakeCoursewareThemeAnalysisService());

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.AreEqual(CoursewareAnalysisTab.ThemeResult, viewModel.SelectedAnalysisTab);
        Assert.AreEqual(1, viewModel.SelectedAnalysisTabIndex);

        viewModel.SelectedAnalysisTabIndex = 0;

        Assert.AreEqual(CoursewareAnalysisTab.Conversation, viewModel.SelectedAnalysisTab);
        Assert.AreEqual(0, viewModel.SelectedAnalysisTabIndex);
    }

    [TestMethod(DisplayName = "主题分析失败后应保留已加载课件并显示分析错误")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldKeepLoadedCoursewareWhenAnalysisFails()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var analysisService = new FakeCoursewareThemeAnalysisService(
            (_, _, _, _) => Task.FromException<CoursewareThemeAnalysisResult>(new InvalidOperationException("主题服务不可用")));
        var viewModel = CreateViewModel(analysisService);

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.AreEqual(CoursewareWorkspaceState.AnalysisFailed, viewModel.WorkspaceState);
        Assert.IsNotNull(viewModel.CoursewareSession);
        Assert.HasCount(1, viewModel.CoursewareThumbnails);
        Assert.AreEqual("主题服务不可用", viewModel.LoadErrorMessage);
    }

    [TestMethod(DisplayName = "主题分析过程应归并阶段并保留完整 Copilot 消息")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldMergeStagesAndStreamCopilotMessages()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var analysisService = new FakeCoursewareThemeAnalysisService((inputPackage, progress, messageProgress, _) =>
        {
            progress?.Report(CreateAnalysisEvent(
                CoursewareAnalysisStage.DesigningTheme,
                CoursewareAnalysisEventKind.Progress,
                "正在形成主题方案",
                "正在形成主题。",
                CoursewareAnalysisEventState.Running));
            var message = CopilotChatMessage.CreateAssistant(string.Empty, false);
            message.AppendReasoning("先分析内容层级。");
            message.AppendText("推荐采用清晰理性的视觉方向。");
            messageProgress?.Report(message);
            progress?.Report(CreateAnalysisEvent(
                CoursewareAnalysisStage.DesigningTheme,
                CoursewareAnalysisEventKind.Progress,
                "主题方案形成完成",
                "正在校验。",
                CoursewareAnalysisEventState.Completed));
            progress?.Report(CreateAnalysisEvent(
                CoursewareAnalysisStage.ValidatingTheme,
                CoursewareAnalysisEventKind.ToolSubmission,
                "主题结构校验通过",
                "结构化主题有效。",
                CoursewareAnalysisEventState.Completed));

            return Task.FromResult(FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(inputPackage));
        });
        var viewModel = CreateViewModel(analysisService);

        Assert.IsFalse(viewModel.EnterWorkspaceCommand.CanExecute(null));

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.HasCount(2, viewModel.AnalysisEvents);
        Assert.AreEqual(CoursewareAnalysisStage.DesigningTheme, viewModel.AnalysisEvents[0].Stage);
        Assert.AreEqual(CoursewareAnalysisEventState.Completed, viewModel.AnalysisEvents[0].State);
        Assert.AreEqual(CoursewareAnalysisStage.ValidatingTheme, viewModel.AnalysisEvents[1].Stage);
        Assert.HasCount(1, viewModel.AnalysisChatMessages);
        Assert.AreEqual("推荐采用清晰理性的视觉方向。", viewModel.AnalysisChatMessages[0].Content);
        Assert.AreEqual("先分析内容层级。", viewModel.AnalysisChatMessages[0].Reason);
        Assert.IsTrue(viewModel.AnalysisChatMessages[0].MessageItems.OfType<CopilotChatReasoningItem>().Any());
        Assert.IsTrue(viewModel.EnterWorkspaceCommand.CanExecute(null));
    }

    [TestMethod(DisplayName = "快速 Copilot 响应即使校验失败也应完整显示可读输出")]
    [Timeout(60_000)]
    public async Task FastCopilotResponseShouldRemainVisibleWhenThemeValidationFails()
    {
        const string assistantText = "推荐采用清晰、克制且适合课堂投影的视觉方向。";
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var agent = new CopilotCoursewareThemeAgent(
            new FastCopilotChatManagerFactory(assistantText),
            new CoursewareThemeValidator());
        var analysisService = new CoursewareThemeAnalysisService(
            new CoursewareAnalysisInputBuilder(),
            agent);
        var viewModel = CreateViewModel(analysisService);

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.AreEqual(CoursewareWorkspaceState.AnalysisFailed, viewModel.WorkspaceState);
        Assert.HasCount(4, viewModel.AnalysisChatMessages);
        CollectionAssert.AreEqual(
            new[] { ChatRole.User, ChatRole.Assistant, ChatRole.User, ChatRole.Assistant },
            viewModel.AnalysisChatMessages.Select(message => message.Role).ToArray());
        Assert.IsTrue(viewModel.AnalysisChatMessages.Where(message => message.Role == ChatRole.User).All(message => !string.IsNullOrWhiteSpace(message.Content)));
        Assert.IsTrue(viewModel.AnalysisChatMessages.Where(message => message.Role == ChatRole.Assistant).All(message => message.Content == assistantText));
        var validationEvent = viewModel.AnalysisEvents.Single(item => item.Stage == CoursewareAnalysisStage.ValidatingTheme);
        Assert.AreEqual("主题结构校验", validationEvent.Title);
        Assert.AreEqual(CoursewareAnalysisEventState.Warning, validationEvent.State);
    }

    [TestMethod(DisplayName = "打开无效课件文件夹后应清空缩略图并显示错误")]
    [Timeout(60_000)]
    public async Task OpenCoursewareFolderAsyncShouldClearThumbnailsAndShowErrorWhenLoadingFails()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var invalidDirectory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"InvalidCourseware_{Guid.NewGuid():N}"));
        var viewModel = CreateViewModel(new FakeCoursewareThemeAnalysisService());
        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        await viewModel.OpenCoursewareFolderAsync(invalidDirectory.FullName);

        Assert.AreEqual(CoursewareWorkspaceState.LoadFailed, viewModel.WorkspaceState);
        Assert.IsNull(viewModel.CoursewareSession);
        Assert.IsEmpty(viewModel.CoursewareThumbnails);
        StringAssert.Contains(viewModel.LoadErrorMessage, "Courseware.json");
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.LoadErrorDetails));
    }

    [TestMethod(DisplayName = "未选择课件文件夹时应保持欢迎状态")]
    public async Task OpenCoursewareFolderAsyncShouldKeepWelcomeStateWhenFolderIsNotSelected()
    {
        var viewModel = CreateViewModel();

        await viewModel.OpenCoursewareFolderAsync(null);

        Assert.AreEqual(CoursewareWorkspaceState.Welcome, viewModel.WorkspaceState);
        Assert.IsNull(viewModel.CoursewareSession);
        Assert.IsEmpty(viewModel.CoursewareThumbnails);
    }

    private static CoursewareWorkspaceViewModel CreateViewModel(
        ICoursewareThemeAnalysisService? themeAnalysisService = null)
    {
        return new CoursewareWorkspaceViewModel(
            new CoursewareFolderLoader(),
            new ImmediateViewModelDispatcher(),
            themeAnalysisService);
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 页面信息\n\n- Id: slide-id\n- 尺寸: 1280×720\n- 序号(1-base): 1\n\n---\n\n## 元素简要信息\n\n- 文本.1: (100, 80) 400×60\n\n---\n\n## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }

    private static CoursewareAnalysisEvent CreateAnalysisEvent(
        CoursewareAnalysisStage stage,
        CoursewareAnalysisEventKind kind,
        string title,
        string message,
        CoursewareAnalysisEventState state,
        Guid? id = null)
    {
        return new CoursewareAnalysisEvent
        {
            Id = id ?? Guid.NewGuid(),
            Stage = stage,
            Kind = kind,
            Title = title,
            Message = message,
            State = state,
        };
    }

    private sealed class FastCopilotChatManagerFactory(string assistantText) : ICopilotChatManagerFactory
    {
        public Task<CopilotChatManager> CreateAsync(
            AgentWorkload workload,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fakeChatClient = new FakeChatClient
            {
                OnGetStreamingResponseAsync = (_, _, token) => StreamResponseAsync(assistantText, token),
            };
            var chatManager = new CopilotChatManager();
            var fakeModel = new FakeLanguageModel(fakeChatClient)
            {
                ModelDefinition = new ModelDefinition
                {
                    Provider = "test",
                    ModelName = "test-theme-model",
                    ContextWindowSize = 100_000,
                    MaxOutputTokens = 8_000,
                },
            };
            chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([fakeModel]));
            return Task.FromResult(chatManager);
        }

        private static async IAsyncEnumerable<ChatResponseUpdate> StreamResponseAsync(
            string text,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, text);
            await Task.CompletedTask;
        }
    }
}