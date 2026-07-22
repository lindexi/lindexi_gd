using System.IO;
using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
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
        Assert.AreEqual("课件文本分析完成", viewModel.AnalysisStatus);
        StringAssert.Contains(viewModel.AnalysisCaption, "主题建议");
        Assert.AreEqual("已通过", viewModel.TextAnalysisCapabilityText);
        Assert.AreEqual("已通过", viewModel.ThemeSuggestionCapabilityText);
        Assert.AreEqual("暂不支持", viewModel.DesignSystemCapabilityText);
        Assert.AreEqual("暂不支持", viewModel.TemplateValidationCapabilityText);
        Assert.AreEqual("未请求", viewModel.VisualAnalysisCapabilityText);
        Assert.AreEqual("尚未生成", viewModel.PageGenerationCapabilityText);
        Assert.HasCount(2, viewModel.CoursewareThumbnails);
        Assert.AreEqual("slide-first", viewModel.CoursewareThumbnails[0].SlideId);
        Assert.AreEqual("第 1 页", viewModel.CoursewareThumbnails[0].AccessibleName);
        Assert.IsTrue(viewModel.CoursewareThumbnails[0].HasScreenshot);
        Assert.AreEqual("slide-second", viewModel.CoursewareThumbnails[1].SlideId);
        Assert.IsFalse(viewModel.CoursewareThumbnails[1].HasScreenshot);
        Assert.IsTrue(viewModel.CoursewareThumbnails[1].HasWarning);
        Assert.IsNotNull(viewModel.SlideWorkspace);
        Assert.HasCount(2, viewModel.SlideWorkspace.Slides);
        Assert.AreEqual("slide-first", viewModel.SlideWorkspace.Slides[0].SlideId);
        Assert.IsTrue(viewModel.SlideWorkspace.Slides.All(slide => slide.Runtime is null));
    }

    [TestMethod(DisplayName = "进入真实工作台应只初始化选中页且往返导航保留页面状态")]
    [Timeout(60_000)]
    public async Task WorkspaceNavigationShouldInitializeSelectedRealSlideAndPreserveState()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .AddSlide("slide-second", CreateSlideMarkdown("第二页标题", "第二页内容"))
            .Build();
        var viewModel = CreateViewModel(
            new FakeCoursewareThemeAnalysisService(),
            new FakeSlideChatManagerFactory());
        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
        var workspace = viewModel.SlideWorkspace!;
        workspace.SelectedSlide = workspace.Slides[1];
        workspace.SelectedSlide.InputText = "保留的页面输入";

        await viewModel.EnterWorkspaceCommand.ExecuteAsync();

        Assert.IsTrue(viewModel.IsWorkspacePage);
        Assert.IsNull(workspace.Slides[0].Runtime);
        Assert.IsNotNull(workspace.Slides[1].Runtime);
        viewModel.BackToAnalysisCommand.Execute(null);
        Assert.IsTrue(viewModel.IsAnalysisPage);

        await viewModel.EnterWorkspaceCommand.ExecuteAsync();

        Assert.AreSame(workspace, viewModel.SlideWorkspace);
        Assert.AreSame(workspace.Slides[1], workspace.SelectedSlide);
        Assert.AreEqual("保留的页面输入", workspace.SelectedSlide.InputText);
    }

    [TestMethod(DisplayName = "分析生成快照后新实例打开快照应跳过分析并直达等价页面美化工作台")]
    [Timeout(60_000)]
    public async Task OpeningSavedSnapshotShouldEnterEquivalentWorkspaceWithoutAnalyzingAgain()
    {
        const string generationInstruction = "保持教学语义并突出当前页结论。";
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容 resource-first"), width: 1600, height: 900)
            .AddSlide("slide-second", CreateSlideMarkdown("第二页标题", "第二页内容"), hasScreenshot: false, width: 1600, height: 900)
            .AddResource("resource-first", "image", "resource-first.png")
            .Build();
        var snapshotOutputRoot = Directory.CreateDirectory(Path.Join(
            Path.GetTempPath(),
            $"CoursewareSnapshotWorkspaceTests_{Guid.NewGuid():N}"));
        var snapshotStore = new CoursewareThemeAnalysisSnapshotStore(snapshotOutputRoot.FullName);
        var normalAnalysisService = new FakeCoursewareThemeAnalysisService();
        var normalViewModel = CreateViewModel(
            normalAnalysisService,
            new FakeSlideChatManagerFactory(),
            snapshotStore);

        await normalViewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
        var snapshotDirectory = snapshotOutputRoot.EnumerateDirectories().Single();
        await normalViewModel.EnterWorkspaceCommand.ExecuteAsync();
        var normalWorkspace = normalViewModel.SlideWorkspace!;
        var normalSelectedSlide = normalWorkspace.SelectedSlide!;
        Assert.IsNotNull(normalSelectedSlide.Runtime);

        var restoredAnalysisService = new FakeCoursewareThemeAnalysisService();
        var restoredViewModel = CreateViewModel(
            restoredAnalysisService,
            new FakeSlideChatManagerFactory(),
            snapshotStore);

        await restoredViewModel.OpenCoursewareFolderAsync(snapshotDirectory.FullName);

        Assert.AreEqual(1, normalAnalysisService.AnalysisCount);
        Assert.AreEqual(0, restoredAnalysisService.AnalysisCount);
        Assert.AreEqual(CoursewareWorkspaceState.AnalysisReady, restoredViewModel.WorkspaceState);
        Assert.IsTrue(restoredViewModel.IsWorkspacePage);
        Assert.IsNotNull(restoredViewModel.CoursewareSession);
        Assert.IsNotNull(restoredViewModel.CoursewareSession.ThemeAnalysisResult);
        Assert.IsNotNull(restoredViewModel.SlideWorkspace);
        Assert.HasCount(2, restoredViewModel.CoursewareThumbnails);
        Assert.HasCount(2, restoredViewModel.SlideWorkspace.Slides);
        Assert.IsEmpty(restoredViewModel.AnalysisEvents);
        Assert.IsEmpty(restoredViewModel.AnalysisChatMessages);

        var restoredWorkspace = restoredViewModel.SlideWorkspace;
        var restoredSelectedSlide = restoredWorkspace.SelectedSlide!;
        Assert.IsNotNull(restoredSelectedSlide.Runtime);
        Assert.AreEqual(normalViewModel.CoursewareTitle, restoredViewModel.CoursewareTitle);
        Assert.AreEqual(normalViewModel.ThemeTitle, restoredViewModel.ThemeTitle);
        Assert.AreEqual(normalViewModel.ThemeDescription, restoredViewModel.ThemeDescription);
        Assert.AreEqual(normalViewModel.FontRecommendationText, restoredViewModel.FontRecommendationText);
        Assert.AreEqual(normalViewModel.SafeAreaText, restoredViewModel.SafeAreaText);
        Assert.AreEqual(normalViewModel.ColorRationale, restoredViewModel.ColorRationale);
        CollectionAssert.AreEqual(normalViewModel.StyleKeywords.ToArray(), restoredViewModel.StyleKeywords.ToArray());
        CollectionAssert.AreEqual(normalViewModel.LayoutPrinciples.ToArray(), restoredViewModel.LayoutPrinciples.ToArray());
        CollectionAssert.AreEqual(normalViewModel.AnalysisWarnings.ToArray(), restoredViewModel.AnalysisWarnings.ToArray());
        Assert.AreEqual(normalSelectedSlide.SlideId, restoredSelectedSlide.SlideId);
        Assert.AreEqual(normalSelectedSlide.SourceMarkdownText, restoredSelectedSlide.SourceMarkdownText);
        Assert.AreEqual(normalSelectedSlide.CanvasWidth, restoredSelectedSlide.CanvasWidth);
        Assert.AreEqual(normalSelectedSlide.CanvasHeight, restoredSelectedSlide.CanvasHeight);
        Assert.AreEqual(normalSelectedSlide.HasSourceScreenshot, restoredSelectedSlide.HasSourceScreenshot);
        Assert.AreEqual(
            normalSelectedSlide.Runtime!.SlideChatManager.Pipeline.PromptProvider.BuildSystemPrompt(),
            restoredSelectedSlide.Runtime!.SlideChatManager.Pipeline.PromptProvider.BuildSystemPrompt());

        var promptBuilder = new CoursewareSlidePromptBuilder(
            new CoursewareSlideSummaryService(),
            new CoursewareThemePageDesignAdapter());
        var normalPrompt = promptBuilder.Build(
            normalViewModel.CoursewareSession!.InputPackage,
            normalViewModel.CoursewareSession.ThemeAnalysisResult!,
            normalSelectedSlide.SlideIndex,
            generationInstruction,
            screenshotAttached: true);
        var restoredPrompt = promptBuilder.Build(
            restoredViewModel.CoursewareSession.InputPackage,
            restoredViewModel.CoursewareSession.ThemeAnalysisResult!,
            restoredSelectedSlide.SlideIndex,
            generationInstruction,
            screenshotAttached: true);

        Assert.AreEqual(normalPrompt.Prompt, restoredPrompt.Prompt);
        Assert.AreEqual(normalPrompt.EstimatedTokenCount, restoredPrompt.EstimatedTokenCount);
        Assert.AreEqual(normalPrompt.Envelope.DesignContext.ReferenceCanvasWidth, restoredPrompt.Envelope.DesignContext.ReferenceCanvasWidth);
        Assert.AreEqual(normalPrompt.Envelope.DesignContext.ReferenceCanvasHeight, restoredPrompt.Envelope.DesignContext.ReferenceCanvasHeight);
    }

    [TestMethod(DisplayName = "打开新课件后应替换旧真实工作台且不复用页面运行时")]
    [Timeout(60_000)]
    public async Task OpeningAnotherCoursewareShouldReplaceSlideWorkspace()
    {
        var firstDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var secondDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-new", CreateSlideMarkdown("新课件标题", "新课件内容"))
            .Build();
        var viewModel = CreateViewModel(
            new FakeCoursewareThemeAnalysisService(),
            new FakeSlideChatManagerFactory());
        await viewModel.OpenCoursewareFolderAsync(firstDirectory.FullName);
        var firstWorkspace = viewModel.SlideWorkspace!;
        await viewModel.EnterWorkspaceCommand.ExecuteAsync();
        Assert.IsNotNull(firstWorkspace.SelectedSlide?.Runtime);

        await viewModel.OpenCoursewareFolderAsync(secondDirectory.FullName);

        Assert.IsNotNull(viewModel.SlideWorkspace);
        Assert.AreNotSame(firstWorkspace, viewModel.SlideWorkspace);
        Assert.AreEqual("slide-new", viewModel.SlideWorkspace.Slides[0].SlideId);
        Assert.IsNull(viewModel.SlideWorkspace.Slides[0].Runtime);
        Assert.IsTrue(viewModel.IsAnalysisPage);
    }

    [TestMethod(DisplayName = "重新分析主题应保留既有工作台页面状态并更新未来生成主题")]
    [Timeout(60_000)]
    public async Task ReanalyzeShouldPreserveWorkspaceStateAndUpdateThemeSource()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var analysisCount = 0;
        var analysisService = new FakeCoursewareThemeAnalysisService((inputPackage, _, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            analysisCount++;
            var result = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(inputPackage);
            return Task.FromResult(analysisCount == 1
                ? result
                : result with
                {
                    Theme = result.Theme with
                    {
                        Title = "更新主题",
                        Summary = "重新分析后的主题",
                    },
                });
        });
        var viewModel = CreateViewModel(analysisService, new FakeSlideChatManagerFactory());
        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
        var workspace = viewModel.SlideWorkspace!;
        workspace.SelectedSlide!.InputText = "保留页面输入";

        await viewModel.ReanalyzeCommand.ExecuteAsync();

        Assert.AreSame(workspace, viewModel.SlideWorkspace);
        Assert.AreEqual("保留页面输入", workspace.SelectedSlide.InputText);
        Assert.AreEqual("更新主题", workspace.ThemeTitle);
        Assert.AreEqual("更新主题", viewModel.ThemeTitle);
        Assert.AreEqual(CoursewareWorkspaceState.AnalysisReady, viewModel.WorkspaceState);
    }

    [TestMethod(DisplayName = "重新分析主题应保存新快照且不覆盖已有版本")]
    [Timeout(60_000)]
    public async Task ReanalyzeShouldPublishAnotherSnapshotVersion()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var snapshotOutputRoot = Directory.CreateDirectory(Path.Join(
            Path.GetTempPath(),
            $"CoursewareReanalysisSnapshotTests_{Guid.NewGuid():N}"));
        var timestamp = DateTimeOffset.Parse("2026-07-22T03:44:47.123+08:00");
        var snapshotStore = new CoursewareThemeAnalysisSnapshotStore(
            snapshotOutputRoot.FullName,
            () => timestamp);
        var analysisService = new FakeCoursewareThemeAnalysisService();
        var viewModel = CreateViewModel(analysisService, snapshotStore: snapshotStore);

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
        await viewModel.ReanalyzeCommand.ExecuteAsync();

        Assert.AreEqual(2, analysisService.AnalysisCount);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "CoursewareThemeAnalysis_20260722_034447_123",
                "CoursewareThemeAnalysis_20260722_034447_123_1",
            },
            snapshotOutputRoot.EnumerateDirectories().Select(directory => directory.Name).ToArray());
        Assert.AreEqual(CoursewareWorkspaceState.AnalysisReady, viewModel.WorkspaceState);
        Assert.IsNotNull(viewModel.SlideWorkspace);
    }

    [TestMethod(DisplayName = "主题分析完成但快照保存失败时不应提交结果或创建工作台")]
    [Timeout(60_000)]
    public async Task SnapshotSaveFailureShouldPreventAnalysisResultPublication()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var analysisService = new FakeCoursewareThemeAnalysisService();
        var viewModel = CreateViewModel(
            analysisService,
            snapshotStore: new FailingCoursewareThemeAnalysisSnapshotStore());

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.AreEqual(1, analysisService.AnalysisCount);
        Assert.AreEqual(CoursewareWorkspaceState.AnalysisFailed, viewModel.WorkspaceState);
        Assert.IsNotNull(viewModel.CoursewareSession);
        Assert.IsNull(viewModel.CoursewareSession.ThemeAnalysisResult);
        Assert.IsNull(viewModel.SlideWorkspace);
        Assert.AreEqual("测试快照保存失败", viewModel.LoadErrorMessage);
        Assert.IsFalse(viewModel.EnterWorkspaceCommand.CanExecute(null));
    }

    [TestMethod(DisplayName = "目录存在损坏快照标记时应明确失败且不得回退普通主题分析")]
    [Timeout(60_000)]
    public async Task CorruptedSnapshotMarkerShouldFailWithoutFallingBackToThemeAnalysis()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        await File.WriteAllTextAsync(
            Path.Join(exportDirectory.FullName, CoursewareThemeAnalysisSnapshotManifest.FileName),
            "{ invalid snapshot json");
        var analysisService = new FakeCoursewareThemeAnalysisService();
        var viewModel = CreateViewModel(analysisService);

        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);

        Assert.AreEqual(0, analysisService.AnalysisCount);
        Assert.AreEqual(CoursewareWorkspaceState.LoadFailed, viewModel.WorkspaceState);
        Assert.IsNull(viewModel.CoursewareSession);
        Assert.IsNull(viewModel.SlideWorkspace);
        StringAssert.Contains(viewModel.LoadErrorMessage, CoursewareThemeAnalysisSnapshotManifest.FileName);
    }

    [TestMethod(DisplayName = "课件级页面生成状态应优先显示进行中和全部取消")]
    [Timeout(60_000)]
    public async Task PageGenerationCapabilityTextShouldReflectInProgressAndCanceledStates()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页内容"))
            .Build();
        var viewModel = CreateViewModel(
            new FakeCoursewareThemeAnalysisService(),
            new FakeSlideChatManagerFactory());
        await viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
        var slide = viewModel.SlideWorkspace!.SelectedSlide!;

        SetSlideState(slide, CoursewareSlideState.Generating);
        Assert.AreEqual("正在处理 1 页", viewModel.PageGenerationCapabilityText);

        SetSlideState(slide, CoursewareSlideState.Canceled);
        Assert.AreEqual("已取消 1 页", viewModel.PageGenerationCapabilityText);
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
        ICoursewareThemeAnalysisService? themeAnalysisService = null,
        ISlideChatManagerFactory? slideChatManagerFactory = null,
        ICoursewareThemeAnalysisSnapshotStore? snapshotStore = null)
    {
        var summaryService = new CoursewareSlideSummaryService();
        snapshotStore ??= CreateSnapshotStore();
        return new CoursewareWorkspaceViewModel(
            new CoursewareFolderLoader(),
            new ImmediateViewModelDispatcher(),
            themeAnalysisService,
            slideChatManagerFactory,
            summaryService,
            new CoursewareSlidePromptBuilder(
                summaryService,
                new CoursewareThemePageDesignAdapter()),
            snapshotStore);
    }

    private static CoursewareThemeAnalysisSnapshotStore CreateSnapshotStore()
    {
        var outputRoot = Directory.CreateDirectory(Path.Join(
            Path.GetTempPath(),
            $"CoursewareThemeSnapshotTests_{Guid.NewGuid():N}"));
        return new CoursewareThemeAnalysisSnapshotStore(outputRoot.FullName);
    }

    private static void SetSlideState(
        CoursewareSlideItemViewModel slide,
        CoursewareSlideState state)
    {
        typeof(CoursewareSlideItemViewModel)
            .GetProperty(nameof(CoursewareSlideItemViewModel.State))!
            .SetValue(slide, state);
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

    private sealed class FailingCoursewareThemeAnalysisSnapshotStore : ICoursewareThemeAnalysisSnapshotStore
    {
        public string ManifestFileName => CoursewareThemeAnalysisSnapshotManifest.FileName;

        public Task<DirectoryInfo> SaveAsync(
            CoursewareInputPackage inputPackage,
            CoursewareThemeAnalysisResult analysisResult,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromException<DirectoryInfo>(new IOException("测试快照保存失败"));
        }

        public Task<CoursewareThemeAnalysisSnapshot> LoadAsync(
            string folderPath,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}