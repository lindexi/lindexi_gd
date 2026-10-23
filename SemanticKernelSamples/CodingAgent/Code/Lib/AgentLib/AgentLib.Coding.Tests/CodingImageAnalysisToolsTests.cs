using System.Runtime.CompilerServices;

using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Coding.Images;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingImageAnalysisToolsTests
{
    [TestMethod]
    public async Task AnalyzeImageAsyncShouldUseOnlySubmissionToolAndReturnSubmittedText()
    {
        string imagePath = await CreateImageFileAsync();
        IReadOnlyList<ChatMessage>? capturedMessages = null;
        IReadOnlyList<AITool>? capturedTools = null;
        var invocation = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => RespondAsync(
                ++invocation,
                messages,
                options,
                value => capturedMessages ??= value,
                value => capturedTools ??= value,
                cancellationToken),
        };
        var tools = new CodingImageAnalysisTools(CreateChatManager(client));

        string result = await tools.AnalyzeImageAsync([imagePath], "提取图片内容");

        Assert.AreEqual("分析完成", result);
        CollectionAssert.AreEqual(
            new[] { CodingImageAnalysisTools.SubmitImageAnalysisResultToolName },
            capturedTools!.Select(tool => tool.Name).ToArray());
        Assert.IsNotNull(capturedMessages);
        Assert.HasCount(2, capturedMessages);
        Assert.AreEqual(ChatRole.System, capturedMessages[0].Role);
        Assert.AreEqual(ChatRole.User, capturedMessages[1].Role);
        Assert.IsInstanceOfType<TextContent>(capturedMessages[1].Contents[0]);
        Assert.IsInstanceOfType<DataContent>(capturedMessages[1].Contents[1]);
    }

    [TestMethod]
    public async Task AnalyzeImageAsyncWhenInstructionIsEmptyShouldReturnFailureReason()
    {
        var tools = new CodingImageAnalysisTools(new CopilotChatManager());

        string result = await tools.AnalyzeImageAsync(["image.png"], " ");

        Assert.AreEqual("图片分析失败：图片分析要求不能为空。", result);
    }

    [TestMethod]
    public async Task AnalyzeImageAsyncWhenFileDoesNotExistShouldReturnFailureReason()
    {
        string imagePath = Path.Join(Path.GetTempPath(), $"missing-image-{Guid.NewGuid():N}.png");
        var tools = new CodingImageAnalysisTools(new CopilotChatManager());

        string result = await tools.AnalyzeImageAsync([imagePath], "描述图片");

        StringAssert.Contains(result, nameof(FileNotFoundException));
        StringAssert.Contains(result, imagePath);
    }

    [TestMethod]
    public async Task AnalyzeImageAsyncWhenFileIsNotImageShouldReturnFailureReason()
    {
        string filePath = Path.Join(Path.GetTempPath(), $"coding-image-analysis-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(filePath, "not an image");
        var tools = new CodingImageAnalysisTools(new CopilotChatManager());

        string result = await tools.AnalyzeImageAsync([filePath], "描述图片");

        Assert.AreEqual($"图片分析失败：文件“{filePath}”不是受支持的图片，请提供 PNG、JPG 等常见图片格式。", result);
    }

    [TestMethod]
    public async Task AnalyzeImageAsyncWhenAgentRunFailsShouldReturnFailureReason()
    {
        string imagePath = await CreateImageFileAsync();
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
                ThrowingStreamAsync(cancellationToken),
        };
        var tools = new CodingImageAnalysisTools(CreateChatManager(client));

        string result = await tools.AnalyzeImageAsync([imagePath], "描述图片");

        StringAssert.Contains(result, nameof(InvalidOperationException));
        StringAssert.Contains(result, "模型不可用。");
    }

    [TestMethod]
    public async Task AnalyzeImageAsyncShouldRetryOnceWhenSubmissionToolIsNotCalled()
    {
        string imagePath = await CreateImageFileAsync();
        var invocation = 0;
        IReadOnlyList<ChatMessage>? secondRunMessages = null;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) => TextOnlyAsync(
                ++invocation,
                messages,
                value => secondRunMessages = value,
                cancellationToken),
        };
        var tools = new CodingImageAnalysisTools(CreateChatManager(client));

        string result = await tools.AnalyzeImageAsync([imagePath], "描述图片");

        Assert.AreEqual(CodingImageAnalysisTools.MissingSubmissionResult, result);
        Assert.AreEqual(2, invocation);
        Assert.IsNotNull(secondRunMessages);
        Assert.HasCount(3, secondRunMessages);
        StringAssert.Contains(secondRunMessages[2].Text, CodingImageAnalysisTools.SubmitImageAnalysisResultToolName);
    }

    private static CopilotChatManager CreateChatManager(FakeChatClient client)
    {
        var manager = new CopilotChatManager();
        var model = new FakeLanguageModel(client)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = "fake",
                ModelId = "image-model",
                ModelName = "Image Model",
            },
        };
        manager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([model]));
        return manager;
    }

    private static async Task<string> CreateImageFileAsync()
    {
        string path = Path.Join(Path.GetTempPath(), $"coding-image-analysis-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path,
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        ]);
        return path;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> RespondAsync(
        int invocation,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        Action<IReadOnlyList<ChatMessage>> captureMessages,
        Action<IReadOnlyList<AITool>> captureTools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        captureMessages(messages.ToArray());
        captureTools(options?.Tools?.ToArray() ?? []);
        if (invocation == 1)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant,
            [
                new FunctionCallContent(
                    "submit-image-analysis",
                    CodingImageAnalysisTools.SubmitImageAnalysisResultToolName,
                    new Dictionary<string, object?> { ["text"] = "分析完成" }),
            ]);
        }
        else
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("已提交")]);
        }

        await Task.Yield();
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw new InvalidOperationException("模型不可用。");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> TextOnlyAsync(
        int invocation,
        IEnumerable<ChatMessage> messages,
        Action<IReadOnlyList<ChatMessage>> captureSecondRunMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (invocation == 2)
        {
            captureSecondRunMessages(messages.ToArray());
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("普通文本")]);
        await Task.Yield();
    }
}
