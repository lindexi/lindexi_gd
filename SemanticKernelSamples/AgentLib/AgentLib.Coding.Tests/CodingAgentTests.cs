using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingAgentTests
{
    [TestMethod(DisplayName = "运行应立即返回流式消息并只使用租约工具")]
    [Timeout(10000)]
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
        Assert.AreEqual("前后", context.UserChatMessage.Content);
    }

    [TestMethod(DisplayName = "连续运行应复用同一个 AgentSession")]
    [Timeout(10000)]
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

    [TestMethod(DisplayName = "连续运行后会话历史只应保留一个系统提示词")]
    [Timeout(10000)]
    public async Task RunAsyncShouldKeepOnlyOneSystemPromptInSessionHistory()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) => ImmediateStreamAsync(
                messages,
                [],
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = new CodingAgent(CreateProvider("workspace", []));

        CodingAgentRunResult first = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "第一轮",
            "workspace");
        await first.CompletionTask;
        CodingAgentRunResult second = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "第二轮",
            "workspace");
        await second.CompletionTask;

        AgentSession agentSession = chatManager.SelectedSession.AgentSession!;
        int systemPromptCount = agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages)
            ? messages.Count(message => message.Role == ChatRole.System)
            : 0;
        Assert.AreEqual(1, systemPromptCount);
    }

    [TestMethod(DisplayName = "同一 CodingAgent 允许重叠运行")]
    [Timeout(10000)]
    public async Task RunAsyncShouldAllowOverlappingRuns()
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

        CodingAgentRunResult second = await agent.RunAsync(
            secondContext,
            "第二轮",
            "workspace");

        releaseStream.TrySetResult();
        await first.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2));
        await second.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod(DisplayName = "输入快照失败后应允许再次运行")]
    [Timeout(10000)]
    public async Task RunAsyncWhenInputSnapshotFailsShouldAllowNextRun()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) => ImmediateStreamAsync(
                messages,
                [],
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = new CodingAgent(CreateProvider("workspace", []));

        IManualSendMessageContext failingContext = await chatManager.CreateManualSendMessageContextAsync();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => agent.RunAsync(
            failingContext,
            new ThrowingReadOnlyList(),
            "workspace"));

        CodingAgentRunResult nextRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "后续任务",
            "workspace");
        Assert.AreEqual("完成", await nextRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [TestMethod(DisplayName = "模型初始化失败时应清除助手占位符")]
    [Timeout(10000)]
    public async Task RunAsyncWhenModelInitializationFailsShouldClearPlaceholder()
    {
        await using var agent = new CodingAgent(CreateProvider("workspace", []));
        var context = new FailingManualSendMessageContext();

        CodingAgentRunResult result = await agent.RunAsync(context, "任务", "workspace");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.IsTrue(context.MessagesAppended);
        Assert.IsTrue(string.IsNullOrEmpty(context.AssistantChatMessage.Content));
    }

    [TestMethod(DisplayName = "工作区切换后运行应继续使用旧租约且下一轮使用新工具")]
    [Timeout(10000)]
    public async Task WorkspaceChangeDuringRunShouldKeepOldLeaseAndUseNewToolsNextRun()
    {
        var firstResource = new TrackingAsyncDisposable();
        var secondResource = new TrackingAsyncDisposable();
        var firstStreamStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstStream = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyList<AITool>? firstRunTools = null;
        IReadOnlyList<AITool>? secondRunTools = null;
        int runCount = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => CaptureWorkspaceToolsAsync(
                messages,
                options,
                Interlocked.Increment(ref runCount),
                firstStreamStarted,
                releaseFirstStream,
                tools => firstRunTools = tools,
                tools => secondRunTools = tools,
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var provider = CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(
                path,
                [AIFunctionFactory.Create(() => path, $"tool_{path}")],
                path == "first" ? firstResource : secondResource)));
        await provider.SetWorkspacePathAsync("first", CancellationToken.None);
        var agent = new CodingAgent(provider);
        try
        {
            CodingAgentRunResult firstRun = await agent.RunAsync(
                await chatManager.CreateManualSendMessageContextAsync(),
                "第一轮",
                "first");
            await firstStreamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await provider.SetWorkspacePathAsync("second", CancellationToken.None);

            Assert.AreEqual(0, firstResource.DisposeCount);
            AIFunction oldTool = firstRunTools!.OfType<AIFunction>().Single(tool => tool.Name == "tool_first");
            Assert.AreEqual("first", (await oldTool.InvokeAsync()).ToString());
            releaseFirstStream.TrySetResult();
            Assert.AreEqual("完成", await firstRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
            await firstResource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.AreEqual(1, firstResource.DisposeCount);

            CodingAgentRunResult secondRun = await agent.RunAsync(
                await chatManager.CreateManualSendMessageContextAsync(),
                "第二轮",
                "second");
            Assert.AreEqual("完成", await secondRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
            CollectionAssert.AreEqual(
                new[] { "tool_second" },
                secondRunTools!.Select(tool => tool.Name).ToArray());
        }
        finally
        {
            releaseFirstStream.TrySetResult();
            await agent.DisposeAsync();
        }

        Assert.AreEqual(1, secondResource.DisposeCount);
    }

    [TestMethod(DisplayName = "模型未返回更新时应清除助手占位符并返回空回复")]
    [Timeout(10000)]
    public async Task RunAsyncWhenModelReturnsNoUpdatesShouldClearPlaceholder()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) => EmptyStreamAsync(cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = new CodingAgent(CreateProvider("workspace", []));
        IManualSendMessageContext context = await chatManager.CreateManualSendMessageContextAsync();

        CodingAgentRunResult result = await agent.RunAsync(context, "任务", "workspace");

        Assert.IsNull(await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.IsTrue(string.IsNullOrEmpty(result.AssistantChatMessage.Content));
        Assert.AreSame(context.AssistantChatMessage, result.AssistantChatMessage);
    }

    [TestMethod(DisplayName = "运行取消时应释放租约并允许后续运行")]
    [Timeout(10000)]
    public async Task RunAsyncWhenCanceledShouldReleaseLeaseAndAllowNextRun()
    {
        var streamStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resource = new TrackingAsyncDisposable();
        int runCount = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
                Interlocked.Increment(ref runCount) == 1
                    ? WaitForCancellationAsync(messages, options, streamStarted, cancellationToken)
                    : ImmediateStreamAsync(messages, [], cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var provider = CreateProvider("workspace", [], resource);
        await provider.SetWorkspacePathAsync("workspace", CancellationToken.None);
        await using var agent = new CodingAgent(provider);
        using var cancellationTokenSource = new CancellationTokenSource();
        CodingAgentRunResult canceledRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "取消任务",
            "workspace",
            cancellationTokenSource.Token);
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await provider.SetWorkspacePathAsync("next-workspace", CancellationToken.None);
        Assert.AreEqual(0, resource.DisposeCount);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await canceledRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        await resource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(1, resource.DisposeCount);

        CodingAgentRunResult nextRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "后续任务",
            "next-workspace");
        Assert.AreEqual("完成", await nextRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [TestMethod(DisplayName = "运行异常时应释放租约并允许后续运行")]
    [Timeout(10000)]
    public async Task RunAsyncWhenModelFailsShouldReleaseLeaseAndAllowNextRun()
    {
        var resource = new TrackingAsyncDisposable();
        int runCount = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
                Interlocked.Increment(ref runCount) == 1
                    ? ThrowingStreamAsync(new InvalidOperationException("模型失败"), cancellationToken)
                    : ImmediateStreamAsync(messages, [], cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var provider = CreateProvider("workspace", [], resource);
        await provider.SetWorkspacePathAsync("workspace", CancellationToken.None);
        await using var agent = new CodingAgent(provider);
        CodingAgentRunResult failedRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "失败任务",
            "workspace");
        await provider.SetWorkspacePathAsync("next-workspace", CancellationToken.None);

        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await failedRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.AreEqual("模型失败", exception.Message);
        await resource.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(1, resource.DisposeCount);

        CodingAgentRunResult nextRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "后续任务",
            "next-workspace");
        Assert.AreEqual("完成", await nextRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [TestMethod(DisplayName = "并发释放调用应等待同一个活动运行")]
    [Timeout(10000)]
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

    private static CodingWorkspaceToolProvider CreateProvider(
        string workspacePath,
        IReadOnlyList<AITool> tools,
        IAsyncDisposable? asyncDisposable = null)
    {
        return CreateProvider(
            (path, _, _) => Task.FromResult(new CodingWorkspaceToolSession(
                path,
                tools,
                path == workspacePath ? asyncDisposable : null)));
    }

    private static CodingWorkspaceToolProvider CreateProvider(
        Func<string, string, CancellationToken, Task<CodingWorkspaceToolSession>> createSession) =>
        new(new TestSessionProvider(createSession));

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

    private sealed class TestSessionProvider(
        Func<string, string, CancellationToken, Task<CodingWorkspaceToolSession>> createSession)
        : ICodingWorkspaceToolSessionProvider
    {
        public Task<CodingWorkspaceToolSession> CreateAsync(
            string workspacePath,
            CancellationToken cancellationToken) =>
            createSession(workspacePath, "test-server", cancellationToken);
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

    private static async IAsyncEnumerable<ChatResponseUpdate> WaitForCancellationAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        TaskCompletionSource streamStarted,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _ = messages;
        _ = options;
        streamStarted.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        yield break;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingStreamAsync(
        Exception exception,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw exception;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CaptureWorkspaceToolsAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        int runNumber,
        TaskCompletionSource firstStreamStarted,
        TaskCompletionSource releaseFirstStream,
        Action<IReadOnlyList<AITool>> captureFirstRunTools,
        Action<IReadOnlyList<AITool>> captureSecondRunTools,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _ = messages;
        IReadOnlyList<AITool> tools = options?.Tools?.ToArray() ?? [];
        if (runNumber == 1)
        {
            captureFirstRunTools(tools);
            firstStreamStarted.TrySetResult();
            await releaseFirstStream.Task.WaitAsync(cancellationToken);
        }
        else
        {
            captureSecondRunTools(tools);
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("完成")]);
    }

    private sealed class TrackingAsyncDisposable : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            Disposed.TrySetResult();
            return default;
        }
    }

    private sealed class FailingManualSendMessageContext : IManualSendMessageContext
    {
        public CopilotChatMessage UserChatMessage { get; } = CopilotChatMessage.CreateUser(string.Empty);

        public CopilotChatMessage AssistantChatMessage { get; } = CopilotChatMessage.CreateAssistant(
            CopilotChatMessage.PlaceholderContent,
            isPresetInfo: false);

        public IChatClient ChatClient { get; } = new FakeChatClient();

        public IMainThreadDispatcher? MainThreadDispatcher => null;

        public IReadOnlyList<AITool> DefaultTools => [];

        public bool MessagesAppended { get; private set; }

        public Task<ChatClientAgent> GetChatClientAgentAsync(
            Action<ChatClientAgentOptions>? configure = null,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ChatClientAgent>(new InvalidOperationException("代理初始化失败。"));

        public Task<AgentSession> GetAgentSessionAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<AgentSession>(new InvalidOperationException("不应获取会话。"));

        public void AppendResponseUpdate(AgentResponseUpdate update) =>
            throw new InvalidOperationException("不应追加响应。 ");

        public Task AppendMessagesToSessionAsync()
        {
            MessagesAppended = true;
            return Task.CompletedTask;
        }

        public IDisposable StartChatting() => NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
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

    private static async IAsyncEnumerable<ChatResponseUpdate> EmptyStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        yield break;
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

    private sealed class ThrowingReadOnlyList : IReadOnlyList<AIContent>
    {
        public AIContent this[int index] => throw new InvalidOperationException("无法读取输入内容。");

        public int Count => 1;

        public IEnumerator<AIContent> GetEnumerator() =>
            throw new InvalidOperationException("无法枚举输入内容。");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Join(AppContext.BaseDirectory, nameof(CodingAgentTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
