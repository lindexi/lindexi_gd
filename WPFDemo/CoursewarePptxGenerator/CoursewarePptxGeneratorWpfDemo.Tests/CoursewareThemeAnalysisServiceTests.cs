using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeAnalysisServiceTests
{
    [TestMethod(DisplayName = "正式分析服务应返回 Agent 主题并发布业务过程事件")]
    public async Task AnalyzeAsyncShouldReturnAgentThemeAndPublishEvents()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "#### 内容\n```\n测试标题\n测试内容\n```")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var events = new List<CoursewareAnalysisEvent>();
        var service = new CoursewareThemeAnalysisService(
            new CoursewareAnalysisInputBuilder(),
            new FakeThemeAgent(CoursewareThemeValidatorTests.CreateTheme()));

        var result = await service.AnalyzeAsync(
            package,
            new SynchronousProgress<CoursewareAnalysisEvent>(events.Add));

        Assert.AreEqual("清晰课堂", result.Theme.Title);
        Assert.AreEqual(1, result.AnalyzedSlideCount);
        Assert.IsTrue(events.Any(item => item.Stage == CoursewareAnalysisStage.PreparingInput));
        Assert.IsTrue(events.Any(item => item.Stage == CoursewareAnalysisStage.Completed));
    }

    private sealed class FakeThemeAgent : ICoursewareThemeAgent
    {
        private readonly CoursewareTheme _theme;

        public FakeThemeAgent(CoursewareTheme theme)
        {
            _theme = theme;
        }

        public Task<CoursewareTheme> AnalyzeAsync(
            CoursewareAnalysisInput analysisInput,
            double slideWidth,
            double slideHeight,
            IProgress<CoursewareAnalysisEvent>? progress = null,
            IProgress<CopilotChatMessage>? messageProgress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new CoursewareAnalysisEvent
            {
                Stage = CoursewareAnalysisStage.DesigningTheme,
                Kind = CoursewareAnalysisEventKind.Progress,
                Title = "测试 Agent",
                Message = "正在生成测试主题。",
                State = CoursewareAnalysisEventState.Running,
            });
            return Task.FromResult(_theme);
        }
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler;
        }

        public void Report(T value)
        {
            _handler(value);
        }
    }
}