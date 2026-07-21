using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
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
        var analysisInput = CreateAnalysisInput("第一页完整内容", "第二页完整内容\nTAIL-MARKER");
        var originalPrompt = analysisInput.Prompt;
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
            () => agent.AnalyzeAsync(analysisInput, 1280, 720));

        Assert.HasCount(2, capturedMessages, exception.ToString());
        var firstRequestText = string.Join("\n", capturedMessages[0].Select(message => message.Text));
        var repairRequestText = capturedMessages[1]
            .Select(message => message.Text)
            .Where(text => text.TrimStart().StartsWith('{'))
            .Single(text => text.Contains("courseware-theme-repair/v1", StringComparison.Ordinal));
        StringAssert.Contains(firstRequestText, originalPrompt);
        using var repairDocument = System.Text.Json.JsonDocument.Parse(repairRequestText);
        Assert.AreEqual("courseware-theme-repair/v1", repairDocument.RootElement.GetProperty("schemaVersion").GetString());
        StringAssert.Contains(repairRequestText, "上一轮没有调用 submit_courseware_theme");
        StringAssert.Contains(repairRequestText, "TAIL-MARKER");
        var embeddedOriginal = repairDocument.RootElement.GetProperty("originalAnalysisEnvelope");
        using var originalDocument = System.Text.Json.JsonDocument.Parse(originalPrompt);
        Assert.IsTrue(System.Text.Json.JsonElement.DeepEquals(originalDocument.RootElement, embeddedOriginal));
        Assert.AreEqual(analysisInput.TotalSlideCount, embeddedOriginal.GetProperty("slides").GetArrayLength());
        Assert.IsFalse(repairRequestText.Contains("<repair-task>", StringComparison.Ordinal));
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

        Assert.AreEqual(2, invocationCount, exception.ToString());
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
        var analysisInput = CreateAnalysisInput($"{new string('文', 5_000)}\nTAIL-MARKER");
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

    [TestMethod(DisplayName = "分析输入应为外部不可构造且不可修改的只读产物")]
    [Timeout(60_000)]
    public void AnalysisInputShouldNotExposePublicConstructionOrMutation()
    {
        Assert.IsEmpty(typeof(CoursewareAnalysisInput).GetConstructors());
        Assert.IsTrue(typeof(CoursewareAnalysisInput)
            .GetProperties()
            .All(property => property.SetMethod is null || !property.SetMethod.IsPublic));
        Assert.IsEmpty(typeof(CoursewareAnalysisSourceSnapshot).GetConstructors());
        Assert.IsTrue(typeof(CoursewareAnalysisSourceSnapshot)
            .GetProperties()
            .All(property => property.SetMethod is null || !property.SetMethod.IsPublic));
    }

    private static CoursewareAnalysisInput CreateAnalysisInput(params string[] slideContents)
    {
        var contents = slideContents.Length == 0 ? ["测试内容"] : slideContents;
        var slides = new CoursewareSlideInput[contents.Length];
        for (var index = 0; index < contents.Length; index++)
        {
            var slideId = $"slide-{index + 1}";
            var markdown = $"## 页面信息\n\n- Id: {slideId}\n- 尺寸: 1280×720\n- 序号(1-base): {index + 1}\n\n---\n\n## 元素细节\n\n{contents[index]}";
            slides[index] = new CoursewareSlideInput
            {
                SlideIndex = index,
                PageNumber = index + 1,
                SlideId = slideId,
                Width = 1280,
                Height = 720,
                MarkdownFile = new System.IO.FileInfo(System.IO.Path.Join(System.IO.Path.GetTempPath(), $"Slide{index:D3}.md")),
                MarkdownText = markdown,
            };
        }

        return new CoursewareAnalysisInputBuilder().Build(new CoursewareInputPackage
        {
            RootDirectory = new System.IO.DirectoryInfo(System.IO.Path.GetTempPath()),
            CoursewareName = "测试课件",
            Slides = slides,
        });
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
