using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CopilotCoursewareThemeAgentTests
{
    [TestMethod(DisplayName = "主题修订请求应重新携带完整原始课件输入")]
    [Timeout(60_000)]
    public async Task AnalyzeAsyncShouldIncludeCompleteOriginalInputInRepairRequest()
    {
        const string originalPrompt = "<slides>\n第一页完整内容\n第二页完整内容\nTAIL-MARKER\n</slides>";
        var capturedMessages = new List<IReadOnlyList<ChatMessage>>();
        var fakeChatClient = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
            {
                capturedMessages.Add(messages.ToArray());
                return StreamTextAsync("正在形成主题方向。", cancellationToken);
            },
        };
        var agent = new CopilotCoursewareThemeAgent(
            new FakeThemeChatManagerFactory(fakeChatClient, CreateModelDefinition()),
            new CoursewareThemeValidator());

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => agent.AnalyzeAsync(CreateAnalysisInput(originalPrompt), 1280, 720));

        Assert.HasCount(2, capturedMessages);
        var firstRequestText = string.Join("\n", capturedMessages[0].Select(message => message.Text));
        var repairRequestText = string.Join("\n", capturedMessages[1].Select(message => message.Text));
        StringAssert.Contains(firstRequestText, originalPrompt);
        StringAssert.Contains(repairRequestText, "<repair-task>");
        StringAssert.Contains(repairRequestText, "上一轮没有调用 submit_courseware_theme");
        StringAssert.Contains(repairRequestText, originalPrompt);
        StringAssert.Contains(repairRequestText, "TAIL-MARKER");
        StringAssert.Contains(exception.Message, "未调用 submit_courseware_theme");
    }

    [TestMethod(DisplayName = "模型缺少上下文容量配置时仍应发送完整请求")]
    [Timeout(60_000)]
    public async Task AnalyzeAsyncShouldSendWhenContextCapacityIsMissing()
    {
        var invocationCount = 0;
        var events = new List<CoursewareAnalysisEvent>();
        var fakeChatClient = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            {
                invocationCount++;
                return StreamTextAsync("正在分析完整课件。", cancellationToken);
            },
        };
        var modelDefinition = CreateModelDefinition() with
        {
            ContextWindowSize = null,
            MaxOutputTokens = null,
        };
        var agent = new CopilotCoursewareThemeAgent(
            new FakeThemeChatManagerFactory(fakeChatClient, modelDefinition),
            new CoursewareThemeValidator());

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => agent.AnalyzeAsync(
                CreateAnalysisInput("完整课件输入"),
                1280,
                720,
                new SynchronousProgress<CoursewareAnalysisEvent>(events.Add)));

        Assert.AreEqual(2, invocationCount);
        Assert.IsTrue(events.Any(item => item.Message.Contains("已跳过本地预算预检", StringComparison.Ordinal)));
        StringAssert.Contains(exception.Message, "未调用 submit_courseware_theme");
    }

    [TestMethod(DisplayName = "完整课件超过模型预算时应在发送前失败且不得截断")]
    [Timeout(60_000)]
    public async Task AnalyzeAsyncShouldFailBeforeSendingWhenCompleteInputExceedsBudget()
    {
        var invocationCount = 0;
        var fakeChatClient = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            {
                invocationCount++;
                return StreamTextAsync("不应发送", cancellationToken);
            },
        };
        var modelDefinition = CreateModelDefinition() with
        {
            ContextWindowSize = 4_000,
            MaxOutputTokens = 1_000,
        };
        var originalPrompt = $"<slides>\n{new string('文', 5_000)}\nTAIL-MARKER\n</slides>";
        var analysisInput = CreateAnalysisInput(originalPrompt);
        var agent = new CopilotCoursewareThemeAgent(
            new FakeThemeChatManagerFactory(fakeChatClient, modelDefinition),
            new CoursewareThemeValidator());

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => agent.AnalyzeAsync(analysisInput, 1280, 720));

        Assert.AreEqual(0, invocationCount);
        Assert.IsFalse(analysisInput.WasTruncated);
        StringAssert.Contains(analysisInput.Prompt, "TAIL-MARKER");
        StringAssert.Contains(exception.Message, "超出模型");
        StringAssert.Contains(exception.Message, "不会静默截断或丢弃页面");
    }

    private static CoursewareAnalysisInput CreateAnalysisInput(string prompt)
    {
        return new CoursewareAnalysisInput
        {
            Prompt = prompt,
            TotalSlideCount = 2,
            AnalyzedSlideCount = 2,
            CharacterCount = prompt.Length,
            EstimatedTokenCount = prompt.Length,
            WasTruncated = false,
            SectionCharacterCounts = new CoursewareAnalysisInputSectionCharacterCounts
            {
                Task = 0,
                CoursewareOverview = 0,
                ResourceCatalog = 0,
                Slides = prompt.Length,
                OutputRequirements = 0,
            },
            InputFingerprint = new string('A', 64),
        };
    }

    private static ModelDefinition CreateModelDefinition()
    {
        return new ModelDefinition
        {
            Provider = "test",
            ModelName = "test-theme-model",
            ContextWindowSize = 100_000,
            MaxOutputTokens = 8_000,
            Capabilities = new LlmModelCapabilities
            {
                ToolCall = true,
                Input = new LlmModalityCapability { Text = true },
            },
        };
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamTextAsync(
        string text,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate(ChatRole.Assistant, text);
        await Task.CompletedTask;
    }

    private sealed class FakeThemeChatManagerFactory(
        FakeChatClient fakeChatClient,
        ModelDefinition modelDefinition) : ICopilotChatManagerFactory
    {
        public Task<CopilotChatManager> CreateAsync(
            AgentWorkload workload,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fakeModel = new FakeLanguageModel(fakeChatClient)
            {
                ModelDefinition = modelDefinition,
            };
            var chatManager = new CopilotChatManager();
            chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(
                new FakeLanguageModelProvider([fakeModel]));
            return Task.FromResult(chatManager);
        }
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value)
        {
            handler(value);
        }
    }
}
