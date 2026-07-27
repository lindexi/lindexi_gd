using System.Runtime.CompilerServices;
using System.Text.Json;
using System.IO;
using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;
using PptxGenerator.Prompt;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CopilotCoursewareThemeAgentTests
{
    private const string OriginalPrompt = "原始课件 Markdown\n### 标题\n正文与样式汇总，不得改写。";

    [TestMethod(DisplayName = "主题Agent应使用共享完整文档规范和唯一提交工具并逐字发送首轮Prompt")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldUseSharedSpecificationSingleToolAndOriginalPromptAsync()
    {
        var theme = CreateValidTheme();
        var script = new ScriptedChatClient([ToolRound(theme)]);
        var factory = new RecordingFactory(script.ChatClient);
        var specification = "共享完整 SlideML 规范标记";
        var agent = CreateAgent(factory, new StubPromptProvider(specification));

        var result = await agent.AnalyzeAsync(OriginalPrompt, CreateCanvas(), EmptyResources);

        AssertThemeMatches(theme, result);
        Assert.AreEqual(OriginalPrompt, script.UserPrompts[0]);
        StringAssert.Contains(script.SystemPrompts[0], specification);
        StringAssert.Contains(script.SystemPrompts[0], "Theme 2.0");
        StringAssert.Contains(script.SystemPrompts[0], "禁止输出流式 SlideML 协议");
        CollectionAssert.AreEqual(new[] { "submit_courseware_theme_analysis" }, script.ToolNames[0]);
    }

    [TestMethod(DisplayName = "主题Agent未调用工具时应在同一会话发送短修复Prompt且不重发Markdown")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldRepairMissingToolCallInSameSessionWithoutRepeatingMarkdownAsync()
    {
        var script = new ScriptedChatClient([TextRound("未调用工具"), ToolRound(CreateValidTheme())]);
        var factory = new RecordingFactory(script.ChatClient);
        var agent = CreateAgent(factory);

        await agent.AnalyzeAsync(OriginalPrompt, CreateCanvas(), EmptyResources);

        Assert.AreEqual(1, factory.Manager!.ChatSessions.Count);
        Assert.AreEqual(2, script.UserPrompts.Count);
        StringAssert.Contains(script.UserPrompts[1], "未调用 submit_courseware_theme_analysis");
        Assert.IsFalse(script.UserPrompts[1].Contains(OriginalPrompt, StringComparison.Ordinal));
        Assert.IsFalse(script.UserPrompts[1].Contains("### 标题", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "主题Agent第二轮深度校验成功时应返回修复主题并保留每轮完整消息")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldReturnSecondRoundThemeAndPublishEveryMessageAsync()
    {
        var firstTheme = CreateValidTheme();
        var secondTheme = CreateValidTheme() with { Style = "第二轮主题" };
        var slideValidator = new QueueSlideMlValidator([
            new CoursewareThemeValidationResult { Errors = ["CoverPageSlideMl: 超出画布。"] },
            new CoursewareThemeValidationResult(),
        ]);
        var script = new ScriptedChatClient([ToolRound(firstTheme), ToolRound(secondTheme)]);
        var messages = new List<CopilotChatMessage>();
        var events = new List<CoursewareAnalysisEvent>();
        var agent = CreateAgent(new RecordingFactory(script.ChatClient), slideValidator: slideValidator);

        var canvas = CreateCanvas();
        var result = await agent.AnalyzeAsync(
            OriginalPrompt,
            canvas,
            new HashSet<string>(["resource-1"], StringComparer.Ordinal),
            new ImmediateProgress<CoursewareAnalysisEvent>(events.Add),
            new ImmediateProgress<CopilotChatMessage>(messages.Add));

        AssertThemeMatches(secondTheme, result);
        Assert.AreEqual(2, messages.Count);
        Assert.IsTrue(messages.All(message => message.MessageItems.OfType<CopilotChatToolItem>().Any()));
        Assert.IsTrue(events.Any(item => item.Title.Contains("第 1 轮未通过", StringComparison.Ordinal)));
        Assert.IsTrue(events.Any(item => item.Title.Contains("第 2 轮通过", StringComparison.Ordinal)));
        Assert.AreSame(canvas, slideValidator.LastCanvas);
        CollectionAssert.AreEqual(new[] { "resource-1" }, slideValidator.LastResourceIds!.ToArray());
    }

    [TestMethod(DisplayName = "主题Agent字段校验失败时应仅发送问题列表和重新调用要求")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldUseShortRepairPromptForFieldErrorsAsync()
    {
        var invalidTheme = CreateValidTheme() with { LayoutPrinciples = string.Empty };
        var script = new ScriptedChatClient([ToolRound(invalidTheme), ToolRound(CreateValidTheme())]);
        var agent = CreateAgent(new RecordingFactory(script.ChatClient));

        await agent.AnalyzeAsync(OriginalPrompt, CreateCanvas(), EmptyResources);

        StringAssert.Contains(script.UserPrompts[1], "LayoutPrinciples 不能为空");
        StringAssert.Contains(script.UserPrompts[1], "重新调用 submit_courseware_theme_analysis");
        Assert.IsFalse(script.UserPrompts[1].Contains(OriginalPrompt, StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "主题Agent第三轮仍失败时应抛出包含最近问题的可读异常")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldThrowAfterThirdFailedRoundAsync()
    {
        var script = new ScriptedChatClient([TextRound("一"), TextRound("二"), TextRound("三")]);
        var agent = CreateAgent(new RecordingFactory(script.ChatClient));

        var exception = await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => agent.AnalyzeAsync(OriginalPrompt, CreateCanvas(), EmptyResources));

        StringAssert.Contains(exception.Message, "3 轮");
        StringAssert.Contains(exception.Message, "未调用 submit_courseware_theme_analysis");
        Assert.AreEqual(3, script.UserPrompts.Count);
    }

    [TestMethod(DisplayName = "主题Agent每轮发送前都应执行上下文预算检查")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldCheckContextBudgetBeforeEveryRoundAsync()
    {
        var script = new ScriptedChatClient([TextRound("未调用")]);
        var factory = new RecordingFactory(script.ChatClient, contextWindowSize: 20_000, maxOutputTokens: 1_000);
        var agent = CreateAgent(factory);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => agent.AnalyzeAsync(new string('课', 100_000), CreateCanvas(), EmptyResources));

        StringAssert.Contains(exception.Message, "上下文预算");
        Assert.IsEmpty(script.UserPrompts);
    }

    [TestMethod(DisplayName = "主题Agent修复轮超预算时应明确失败且不发送超预算Prompt")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldCheckRepairRoundBudgetAsync()
    {
        var largeErrors = Enumerable.Range(0, 200).Select(index => $"问题 {index}: {new string('错', 100)}").ToArray();
        var slideValidator = new QueueSlideMlValidator([
            new CoursewareThemeValidationResult { Errors = largeErrors },
        ]);
        var script = new ScriptedChatClient([ToolRound(CreateValidTheme())]);
        var factory = new RecordingFactory(script.ChatClient, contextWindowSize: 2_500, maxOutputTokens: 500);
        var agent = CreateAgent(factory, slideValidator: slideValidator);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => agent.AnalyzeAsync("短首轮", CreateCanvas(), EmptyResources));

        StringAssert.Contains(exception.Message, "上下文预算");
        Assert.HasCount(1, script.UserPrompts);
    }

    [TestMethod(DisplayName = "同一主题Agent并发分析时应隔离会话工具和提交结果")]
    [Timeout(10_000)]
    public async Task AnalyzeAsyncShouldIsolateConcurrentCallsAsync()
    {
        var firstTheme = CreateValidTheme() with { Style = "并发一" };
        var secondTheme = CreateValidTheme() with { Style = "并发二" };
        var factory = new ConcurrentFactory([firstTheme, secondTheme]);
        var agent = CreateAgent(factory);

        var results = await Task.WhenAll(
            agent.AnalyzeAsync("并发 Prompt 一", CreateCanvas(), EmptyResources),
            agent.AnalyzeAsync("并发 Prompt 二", CreateCanvas(), EmptyResources));

        CollectionAssert.AreEquivalent(new[] { "并发一", "并发二" }, results.Select(item => item.Style).ToArray());
        Assert.AreEqual(2, factory.CreatedManagers.Count);
        Assert.IsTrue(factory.CreatedManagers.All(manager => manager.ChatSessions.Count == 1));
    }

    private static readonly IReadOnlySet<string> EmptyResources = new HashSet<string>(StringComparer.Ordinal);

    private static CopilotCoursewareThemeAgent CreateAgent(
        ICopilotChatManagerFactory factory,
        ISlideMlPromptProvider? promptProvider = null,
        QueueSlideMlValidator? slideValidator = null)
    {
        return new CopilotCoursewareThemeAgent(
            factory,
            promptProvider,
            new CoursewareThemeValidator(slideValidator ?? new QueueSlideMlValidator([new CoursewareThemeValidationResult()])));
    }

    private static SlideDocumentContext CreateCanvas()
    {
        return new SlideDocumentContext(1280, 720);
    }

    private static CoursewareTheme CreateValidTheme()
    {
        const string slideMl = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Page />";
        return new CoursewareTheme
        {
            ColorSuggestions =
            [
                new CoursewareColorSuggestion { Name = "纸白", Usage = "背景", Hex = "#FFFFFF" },
                new CoursewareColorSuggestion { Name = "墨色", Usage = "正文", Hex = "#0F172A" },
                new CoursewareColorSuggestion { Name = "强调蓝", Usage = "重点", Hex = "#2563EB" },
            ],
            Fonts = new CoursewareFontSuggestions { Chinese = "微软雅黑", Western = "Arial" },
            Style = "清晰、克制、现代",
            SafeArea = new CoursewareSafeAreaRatios { LeftRatio = 0.05, TopRatio = 0.05, RightRatio = 0.05, BottomRatio = 0.05 },
            SpacingAndVisualEffects = "保持留白。",
            LayoutPrinciples = "建立网格并保持对齐。",
            CoverPageSlideMl = slideMl,
            ContentPageSlideMl = slideMl,
        };
    }

    private static void AssertThemeMatches(CoursewareTheme expected, CoursewareTheme actual)
    {
        Assert.AreEqual(expected.SchemaVersion, actual.SchemaVersion);
        Assert.AreEqual(expected.Style, actual.Style);
        Assert.AreEqual(expected.CoverPageSlideMl, actual.CoverPageSlideMl);
        Assert.AreEqual(expected.ContentPageSlideMl, actual.ContentPageSlideMl);
        CollectionAssert.AreEqual(expected.ColorSuggestions.ToArray(), actual.ColorSuggestions.ToArray());
    }

    private static ScriptRound TextRound(string text) => new(text, null);
    private static ScriptRound ToolRound(CoursewareTheme theme) => new("提交完成", theme);

    private sealed record ScriptRound(string FinalText, CoursewareTheme? Theme);

    private sealed class ScriptedChatClient
    {
        private readonly Queue<ScriptRound> _rounds;
        private ScriptRound? _activeRound;

        public ScriptedChatClient(IEnumerable<ScriptRound> rounds)
        {
            _rounds = new Queue<ScriptRound>(rounds);
            ChatClient = new FakeChatClient { OnGetStreamingResponseAsync = GetStreamingResponseAsync };
        }

        public FakeChatClient ChatClient { get; }
        public List<string> UserPrompts { get; } = [];
        public List<string> SystemPrompts { get; } = [];
        public List<string[]> ToolNames { get; } = [];

        private IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            CancellationToken cancellationToken)
        {
            var messageList = messages.ToList();
            if (_activeRound is null)
            {
                _activeRound = _rounds.Dequeue();
                UserPrompts.Add(messageList.Last(message => message.Role == ChatRole.User).Text ?? string.Empty);
                SystemPrompts.Add(messageList.FirstOrDefault(message => message.Role == ChatRole.System)?.Text ?? string.Empty);
                ToolNames.Add(options?.Tools?.Select(tool => tool.Name).ToArray() ?? []);
                if (_activeRound.Theme is { } theme)
                {
                    return ToolCallAsync(theme, cancellationToken);
                }
            }

            var finalText = _activeRound.FinalText;
            _activeRound = null;
            return TextAsync(finalText, cancellationToken);
        }

        private static async IAsyncEnumerable<ChatResponseUpdate> ToolCallAsync(
            CoursewareTheme theme,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var arguments = new Dictionary<string, object?>
            {
                ["theme"] = JsonSerializer.SerializeToElement(theme),
            };
            yield return new ChatResponseUpdate(
                ChatRole.Assistant,
                [new FunctionCallContent(Guid.NewGuid().ToString("N"), "submit_courseware_theme_analysis", arguments)]);
            await Task.CompletedTask;
        }

        private static async IAsyncEnumerable<ChatResponseUpdate> TextAsync(
            string text,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]);
            await Task.CompletedTask;
        }
    }

    private sealed class RecordingFactory(
        FakeChatClient chatClient,
        int? contextWindowSize = 100_000,
        int? maxOutputTokens = 8_000) : ICopilotChatManagerFactory
    {
        public CopilotChatManager? Manager { get; private set; }

        public Task<CopilotChatManager> CreateAsync(AgentWorkload workload, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = new FakeLanguageModel(chatClient)
            {
                ModelDefinition = new ModelDefinition
                {
                    Provider = "Fake",
                    ModelName = "ThemeFake",
                    ContextWindowSize = contextWindowSize,
                    MaxOutputTokens = maxOutputTokens,
                },
            };
            Manager = new CopilotChatManager();
            Manager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([model]));
            return Task.FromResult(Manager);
        }
    }

    private sealed class ConcurrentFactory(IEnumerable<CoursewareTheme> themes) : ICopilotChatManagerFactory
    {
        private readonly Queue<CoursewareTheme> _themes = new(themes);
        private readonly object _gate = new();

        public List<CopilotChatManager> CreatedManagers { get; } = [];

        public Task<CopilotChatManager> CreateAsync(AgentWorkload workload, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CoursewareTheme theme;
            lock (_gate)
            {
                theme = _themes.Dequeue();
            }

            var scriptedClient = new ScriptedChatClient([ToolRound(theme)]);
            var manager = new CopilotChatManager();
            manager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider(scriptedClient.ChatClient));
            lock (_gate)
            {
                CreatedManagers.Add(manager);
            }
            return Task.FromResult(manager);
        }
    }

    private sealed class QueueSlideMlValidator(IEnumerable<CoursewareThemeValidationResult> results) : ICoursewareThemeSlideMlValidator
    {
        private readonly Queue<CoursewareThemeValidationResult> _results = new(results);

        public SlideDocumentContext? LastCanvas { get; private set; }
        public IReadOnlySet<string>? LastResourceIds { get; private set; }

        public Task<CoursewareThemeValidationResult> ValidateAsync(
            CoursewareTheme theme,
            SlideDocumentContext documentContext,
            IReadOnlySet<string> availableResourceIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastCanvas = documentContext;
            LastResourceIds = availableResourceIds;
            return Task.FromResult(_results.Count > 0 ? _results.Dequeue() : new CoursewareThemeValidationResult());
        }
    }

    private sealed class StubPromptProvider(string specification) : ISlideMlPromptProvider
    {
        public string BuildCompleteDocumentSpecificationPrompt() => specification;
        public string BuildSystemPrompt() => throw new NotSupportedException();
        public string BuildInitialUserPrompt(string userPrompt) => throw new NotSupportedException();
        public string BuildStreamingSystemPrompt() => throw new NotSupportedException();
        public string BuildStreamingUserPrompt(string userPrompt) => throw new NotSupportedException();
    }

    private sealed class ImmediateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
