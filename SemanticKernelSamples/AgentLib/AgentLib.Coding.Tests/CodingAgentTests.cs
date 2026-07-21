using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingAgentTests
{
    [TestMethod(DisplayName = "运行应立即返回流式消息并只使用租约工具")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task RunAsyncShouldReturnStreamingMessageAndUseOnlyLeaseTools()
    {
        string defaultWorkspace = CreateTestDirectory();
        var streamStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseStream = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyList<ChatMessage>? capturedMessages = null;
        ChatOptions? capturedOptions = null;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => StreamAsync(
                messages,
                options,
                streamStarted,
                releaseStream,
                value => capturedMessages = value,
                value => capturedOptions = value,
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        chatManager.WorkspacePath = defaultWorkspace;
        IManualSendMessageContext context = await chatManager.CreateManualSendMessageContextAsync();
        Assert.IsNotEmpty(context.DefaultTools);
        AITool codingTool = AIFunctionFactory.Create(() => "coding", "coding_only");
        await using var agent = new CodingAgent(CreateProvider("coding-workspace", [codingTool]));
        IReadOnlyList<AIContent> contents =
        [
            new TextContent("前"),
            new DataContent(new byte[] { 1, 2, 3 }, "image/png"),
            new TextContent("后"),
        ];

        CodingAgentRunResult result = await agent.RunAsync(
            context,
            contents,
            "coding-workspace",
            CancellationToken.None);
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreSame(context.AssistantChatMessage, result.AssistantChatMessage);
        Assert.IsFalse(result.CompletionTask.IsCompleted);
        CollectionAssert.AreEqual(
            new[] { "coding_only" },
            capturedOptions!.Tools!.Select(tool => tool.Name).ToArray());
        IReadOnlyList<ChatMessage> runMessages = capturedMessages!;
        Assert.HasCount(2, runMessages);
        Assert.AreEqual(ChatRole.System, runMessages[0].Role);
        StringAssert.Contains(runMessages[0].Text, "自动化编程代理");
        Assert.AreEqual(ChatRole.User, runMessages[1].Role);
        ChatMessage userMessage = runMessages[1];
        Assert.IsInstanceOfType<TextContent>(userMessage.Contents[0]);
        Assert.IsInstanceOfType<DataContent>(userMessage.Contents[1]);
        Assert.IsInstanceOfType<TextContent>(userMessage.Contents[2]);

        releaseStream.TrySetResult();
        Assert.AreEqual("完成", await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.AreEqual("完成", result.AssistantChatMessage.Content);
        Assert.HasCount(3, chatManager.SelectedSession.ChatMessages);
        Assert.IsTrue(chatManager.SelectedSession.ChatMessages[0].IsPresetInfo);
        Assert.AreSame(context.UserChatMessage, chatManager.SelectedSession.ChatMessages[1]);
        Assert.AreSame(context.AssistantChatMessage, chatManager.SelectedSession.ChatMessages[2]);
    }

    [TestMethod(DisplayName = "连续运行应复用同一个 AgentSession")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task RunAsyncShouldReuseExistingAgentSession()
    {
        var observedMessageCounts = new List<int>();
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) => ImmediateStreamAsync(
                messages,
                observedMessageCounts,
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = new CodingAgent(CreateProvider("workspace", []));
        IManualSendMessageContext firstContext = await chatManager.CreateManualSendMessageContextAsync();

        CodingAgentRunResult first = await agent.RunAsync(firstContext, "第一轮", "workspace");
        await first.CompletionTask;
        object? firstSession = chatManager.SelectedSession.AgentSession;
        Assert.IsNotNull(firstSession);

        IManualSendMessageContext secondContext = await chatManager.CreateManualSendMessageContextAsync();
        CodingAgentRunResult second = await agent.RunAsync(secondContext, "第二轮", "workspace");
        await second.CompletionTask;

        Assert.AreSame(firstSession, chatManager.SelectedSession.AgentSession);
        Assert.HasCount(2, observedMessageCounts);
        Assert.IsGreaterThan(observedMessageCounts[0], observedMessageCounts[1]);
    }

    [TestMethod(DisplayName = "同一 CodingAgent 不允许重叠运行")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task RunAsyncShouldRejectOverlappingRun()
    {
        var streamStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseStream = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => StreamAsync(
                messages,
                options,
                streamStarted,
                releaseStream,
                _ => { },
                _ => { },
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = new CodingAgent(CreateProvider("workspace", []));
        CodingAgentRunResult first = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "第一轮",
            "workspace");
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        IManualSendMessageContext secondContext = await chatManager.CreateManualSendMessageContextAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => agent.RunAsync(
            secondContext,
            "第二轮",
            "workspace"));

        releaseStream.TrySetResult();
        await first.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod(DisplayName = "并发释放调用应等待同一个活动运行")]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task DisposeAsyncShouldReturnSamePendingLifetime()
    {
        var streamStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseStream = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, _) => StreamIgnoringCancellationAsync(
                messages,
                options,
                streamStarted,
                releaseStream),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var agent = new CodingAgent(CreateProvider("workspace", []));
        CodingAgentRunResult run = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "任务",
            "workspace");
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Task firstDispose = agent.DisposeAsync().AsTask();
        Task secondDispose = agent.DisposeAsync().AsTask();

        Assert.IsFalse(firstDispose.IsCompleted);
        Assert.IsFalse(secondDispose.IsCompleted);
        releaseStream.TrySetResult();
        await Task.WhenAll(firstDispose, secondDispose).WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await run.CompletionTask);
    }

    private static CodingWorkspaceToolProvider CreateProvider(string workspacePath, IReadOnlyList<AITool> tools)
    {
        return new CodingWorkspaceToolProvider(
            "test-server",
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(path, tools)));
    }

    private static CopilotChatManager CreateChatManager(FakeChatClient client)
    {
        var chatManager = new CopilotChatManager();
        var model = new FakeLanguageModel(client)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = "fake",
                ModelId = "fake",
                ModelName = "Fake",
            },
        };
        chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([model]));
        return chatManager;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        TaskCompletionSource streamStarted,
        TaskCompletionSource releaseStream,
        Action<IReadOnlyList<ChatMessage>> captureMessages,
        Action<ChatOptions?> captureOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        captureMessages(messages.ToArray());
        captureOptions(options);
        streamStarted.TrySetResult();
        await releaseStream.Task.WaitAsync(cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("完成")]);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ImmediateStreamAsync(
        IEnumerable<ChatMessage> messages,
        ICollection<int> observedMessageCounts,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        observedMessageCounts.Add(messages.Count());
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("完成")]);
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamIgnoringCancellationAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        TaskCompletionSource streamStarted,
        TaskCompletionSource releaseStream)
    {
        _ = messages;
        _ = options;
        streamStarted.TrySetResult();
        await releaseStream.Task;
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("完成")]);
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Join(AppContext.BaseDirectory, nameof(CodingAgentTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
