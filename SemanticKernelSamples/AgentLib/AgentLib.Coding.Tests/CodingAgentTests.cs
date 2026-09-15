using AgentLib.Coding.Sandboxes;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Reducers;
using AgentLib.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Reflection;
using System.Runtime.CompilerServices;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingAgentTests
{
    [TestMethod(DisplayName = "运行应立即返回流式消息并使用工作区缓存工具")]
    [Timeout(10000)]
    public async Task RunAsyncShouldReturnStreamingMessageAndUseWorkspaceTools()
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
        await using var agent = CreateAgent(CreateProvider("coding-workspace", [codingTool]));
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
            cancellationToken: CancellationToken.None);
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreSame(context.AssistantChatMessage, result.AssistantChatMessage);
        Assert.IsFalse(result.CompletionTask.IsCompleted);
        string[] toolNames = capturedOptions!.Tools!.Select(tool => tool.Name).ToArray();
        CollectionAssert.Contains(toolNames, "coding_only");
        IReadOnlyList<ChatMessage> runMessages = capturedMessages!;
        Assert.HasCount(4, runMessages);
        Assert.AreEqual(ChatRole.System, runMessages[0].Role);
        StringAssert.Contains(runMessages[0].Text, "When asked for your name, you must respond with \"GitHub Copilot\".");
        Assert.IsTrue(runMessages.Take(3).All(message => message.Role == ChatRole.System));
        Assert.AreEqual(ChatRole.User, runMessages[3].Role);
        ChatMessage userMessage = runMessages[3];
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
        Assert.HasCount(3, context.UserChatMessage.MessageItems);
        Assert.IsInstanceOfType<CopilotChatTextItem>(context.UserChatMessage.MessageItems[0]);
        CopilotChatImageItem imageItem = Assert.IsInstanceOfType<CopilotChatImageItem>(context.UserChatMessage.MessageItems[1]);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, imageItem.Data.ToArray());
        Assert.AreEqual("image/png", imageItem.MimeType);
        Assert.IsInstanceOfType<CopilotChatTextItem>(context.UserChatMessage.MessageItems[2]);
    }

    [TestMethod(DisplayName = "运行期间注入消息应由实际运行的 Agent 继续处理")]
    [Timeout(10000)]
    public async Task InjectMessageAsyncShouldContinueTheActiveAgentRun()
    {
        var firstCallStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCallStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyList<ChatMessage>? secondCallMessages = null;
        int callCount = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
            {
                int currentCall = Interlocked.Increment(ref callCount);
                return currentCall == 1
                    ? WaitThenRespondAsync(firstCallStarted, releaseFirstCall, "首轮完成", cancellationToken)
                    : CaptureAndRespondAsync(messages, secondCallStarted, value => secondCallMessages = value, "插话已处理", cancellationToken);
            },
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = CreateAgent(CreateProvider("workspace", []));
        IManualSendMessageContext context = await chatManager.CreateManualSendMessageContextAsync();

        CodingAgentRunResult result = await agent.RunAsync(context, "开始任务", "workspace");
        await firstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await result.InjectMessageAsync([new TextContent("人类插话")]);
        releaseFirstCall.TrySetResult();

        await secondCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(secondCallMessages!
            .Any(message => message.Role == ChatRole.User && message.Text == "人类插话"));
        Assert.AreEqual("首轮完成插话已处理", await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [TestMethod(DisplayName = "交错流式片段应按到达顺序提交")]
    [Timeout(10000)]
    public async Task RunAsyncShouldAppendInterleavedResponseUpdatesInOrder()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
                InterleavedStreamAsync(cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = CreateAgent(CreateProvider("workspace", []));
        IManualSendMessageContext context = await chatManager.CreateManualSendMessageContextAsync();

        CodingAgentRunResult result = await agent.RunAsync(context, "检查顺序", "workspace");
        Assert.AreEqual("正文一正文二", await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));

        Assert.HasCount(4, result.AssistantChatMessage.MessageItems);
        Assert.AreEqual("思考一", Assert.IsInstanceOfType<CopilotChatReasoningItem>(result.AssistantChatMessage.MessageItems[0]).Text);
        Assert.AreEqual("正文一", Assert.IsInstanceOfType<CopilotChatTextItem>(result.AssistantChatMessage.MessageItems[1]).Text);
        Assert.AreEqual("思考二", Assert.IsInstanceOfType<CopilotChatReasoningItem>(result.AssistantChatMessage.MessageItems[2]).Text);
        Assert.AreEqual("正文二", Assert.IsInstanceOfType<CopilotChatTextItem>(result.AssistantChatMessage.MessageItems[3]).Text);
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
        await using var agent = CreateAgent(CreateProvider("workspace", []));
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
        await using var agent = CreateAgent(CreateProvider("workspace", []));

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
        Assert.AreEqual(3, systemPromptCount);
    }

    [TestMethod(DisplayName = "指定 Copilot 指令文件时应追加到代码系统提示词")]
    [Timeout(10000)]
    public async Task RunAsyncShouldAppendCopilotInstructionsToCodePrompt()
    {
        string instructionsPath = Path.Join(CreateTestDirectory(), "copilot-instructions.md");
        const string instructions = "CUSTOM_COPILOT_INSTRUCTIONS";
        await File.WriteAllTextAsync(instructionsPath, instructions);
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) => ImmediateStreamAsync(
                messages,
                [],
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = CreateAgent(CreateProvider("workspace", []), instructionsPath);

        CodingAgentRunResult result = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "测试自定义指令",
            "workspace");
        await result.CompletionTask;

        AgentSession agentSession = chatManager.SelectedSession.AgentSession!;
        bool containsInstructions = agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages)
                                    && messages.Any(message =>
                                        message.Role == ChatRole.System
                                        && message.Text.Contains(instructions, StringComparison.Ordinal));
        Assert.IsTrue(containsInstructions);
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
        CopilotChatManager firstChatManager = CreateChatManager(client);
        CopilotChatManager secondChatManager = CreateChatManager(client);
        await using var agent = CreateAgent(CreateProvider("workspace", []));
        CodingAgentRunResult first = await agent.RunAsync(
            await firstChatManager.CreateManualSendMessageContextAsync(),
            "第一轮",
            "workspace");
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        IManualSendMessageContext secondContext = await secondChatManager.CreateManualSendMessageContextAsync();

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
        await using var agent = CreateAgent(CreateProvider("workspace", []));

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

    [TestMethod(DisplayName = "模型初始化失败时 RunAsync 应直接抛出异常")]
    [Timeout(10000)]
    public async Task RunAsyncWhenModelInitializationFailsShouldThrowDirectly()
    {
        await using var agent = CreateAgent(CreateProvider("workspace", []));
        var context = new FailingManualSendMessageContext();

        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            agent.RunAsync(context, "任务", "workspace"));

        Assert.AreEqual("代理初始化失败。", exception.Message);
        Assert.IsFalse(context.MessagesAppended);
        Assert.AreEqual(CopilotChatMessage.PlaceholderContent, context.AssistantChatMessage.Content);
    }

    [TestMethod(DisplayName = "每轮运行应固定工作区上下文并在下一轮更换缓存")]
    [Timeout(10000)]
    public async Task WorkspacePathChangeShouldApplyToNextRun()
    {
        string firstPath = CreateTestDirectory();
        string secondPath = CreateTestDirectory();
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
        await using var agent = new CodingAgent(new CodingAgentOptions
        {
            LanguageServerCommand = $"missing-roslyn-{Guid.NewGuid():N}",
            AdditionalToolSources = [new WorkspaceNamedToolSource()],
        });

        CodingAgentRunResult firstRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "第一轮",
            firstPath);
        await firstStreamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        CollectionAssert.Contains(
            firstRunTools!.Select(tool => tool.Name).ToArray(),
            $"tool_{Path.GetFileName(firstPath)}");

        releaseFirstStream.TrySetResult();
        Assert.AreEqual("完成", await firstRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));

        CodingAgentRunResult secondRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "第二轮",
            secondPath);
        Assert.AreEqual("完成", await secondRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        CollectionAssert.Contains(
            secondRunTools!.Select(tool => tool.Name).ToArray(),
            $"tool_{Path.GetFileName(secondPath)}");
    }

    [TestMethod(DisplayName = "更新沙盒配置后下一轮应重新装配附加工具")]
    [Timeout(10000)]
    public async Task SandboxConfigurationUpdateShouldApplyToNextRun()
    {
        string workspacePath = CreateTestDirectory();
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyList<AITool>? firstRunTools = null;
        IReadOnlyList<AITool>? secondRunTools = null;
        int runCount = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => CaptureWorkspaceToolsAsync(
                messages,
                options,
                Interlocked.Increment(ref runCount),
                firstRunStarted,
                releaseFirstRun,
                tools => firstRunTools = tools,
                tools => secondRunTools = tools,
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var sandboxToolSource = new WindowsSandboxToolSource("WinRemoteShell.exe", "127.0.0.1:12399");
        await using var agent = new CodingAgent(new CodingAgentOptions
        {
            LanguageServerCommand = $"missing-roslyn-{Guid.NewGuid():N}",
            AdditionalToolSources = [sandboxToolSource],
        });

        CodingAgentRunResult firstRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "第一轮",
            workspacePath);
        await firstRunStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        sandboxToolSource.UpdateConfiguration(false, string.Empty, string.Empty);
        Assert.IsTrue(firstRunTools!.Any(tool => tool.Name == "execute_in_windows_sandbox"));
        releaseFirstRun.TrySetResult();
        Assert.AreEqual("完成", await firstRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        CodingAgentRunResult secondRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "第二轮",
            workspacePath);
        Assert.AreEqual("完成", await secondRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));

        Assert.IsFalse(secondRunTools!.Any(tool => tool.Name == "execute_in_windows_sandbox"));
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
        await using var agent = CreateAgent(CreateProvider("workspace", []));
        IManualSendMessageContext context = await chatManager.CreateManualSendMessageContextAsync();

        CodingAgentRunResult result = await agent.RunAsync(context, "任务", "workspace");

        Assert.IsNull(await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.IsTrue(string.IsNullOrEmpty(result.AssistantChatMessage.Content));
        Assert.AreSame(context.AssistantChatMessage, result.AssistantChatMessage);
    }

    [TestMethod(DisplayName = "运行取消后应允许后续运行")]
    [Timeout(10000)]
    public async Task RunAsyncWhenCanceledShouldAllowNextRun()
    {
        string workspacePath = CreateTestDirectory();
        var streamStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int runCount = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
                Interlocked.Increment(ref runCount) == 1
                    ? WaitForCancellationAsync(messages, options, streamStarted, cancellationToken)
                    : ImmediateStreamAsync(messages, [], cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = CreateAgent(CreateProvider(workspacePath, []));
        using var cancellationTokenSource = new CancellationTokenSource();
        CodingAgentRunResult canceledRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "取消任务",
            workspacePath,
            cancellationToken: cancellationTokenSource.Token);
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await canceledRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        CodingAgentRunResult nextRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "后续任务",
            workspacePath);

        Assert.AreEqual("完成", await nextRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [TestMethod(DisplayName = "运行异常后应允许后续运行")]
    [Timeout(10000)]
    public async Task RunAsyncWhenModelFailsShouldAllowNextRun()
    {
        string workspacePath = CreateTestDirectory();
        int runCount = 0;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
                Interlocked.Increment(ref runCount) == 1
                    ? ThrowingStreamAsync(new InvalidOperationException("模型失败"), cancellationToken)
                    : ImmediateStreamAsync(messages, [], cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var agent = CreateAgent(CreateProvider(workspacePath, []));
        CodingAgentRunResult failedRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "失败任务",
            workspacePath);

        InvalidOperationException exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await failedRun.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2)));
        CodingAgentRunResult nextRun = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "后续任务",
            workspacePath);

        Assert.AreEqual("模型失败", exception.Message);
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
        var agent = CreateAgent(CreateProvider("workspace", []));
        CodingAgentRunResult run = await agent.RunAsync(
            await chatManager.CreateManualSendMessageContextAsync(),
            "任务",
            "workspace");
        await streamStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Task firstDispose = agent.DisposeAsync().AsTask();
        Task secondDispose = agent.DisposeAsync().AsTask();

        releaseStream.TrySetResult();
        await Task.WhenAll(firstDispose, secondDispose).WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await run.CompletionTask);
    }

    [TestMethod]
    public async Task CompressionToolCallObserverShouldAppendAssistantSummary()
    {
        var client = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) => Task.FromResult(new ChatResponse(
            [
                new ChatMessage(ChatRole.System, "系统消息"),
                new ChatMessage(ChatRole.Assistant, "摘要一"),
                new ChatMessage(ChatRole.Assistant, "摘要二"),
            ])),
        };
        var reducer = new CopilotChatManagerToolCallChatReducer(client)
        {
            ConditionalCompressionTokenCountThreshold = 1,
            ForcedCompressionTokenCountThreshold = 1,
        };
        var assistantMessage = CopilotChatMessage.CreateAssistant(
            CopilotChatMessage.PlaceholderContent,
            isPresetInfo: false);
        _ = new CompressionToolCallObserver(assistantMessage, null, reducer);

        await reducer.ReduceAsync(
            [new ChatMessage(ChatRole.Assistant, "需要压缩")],
            CancellationToken.None);

        CopilotChatToolItem toolItem = assistantMessage.MessageItems.OfType<CopilotChatToolItem>().Single();
        Assert.AreEqual("摘要一" + Environment.NewLine + "摘要二", toolItem.OutputText);
    }

    [TestMethod]
    public async Task CompressionToolCallObserverShouldAppendExceptionText()
    {
        var expectedException = new InvalidOperationException("压缩失败");
        var client = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) => Task.FromException<ChatResponse>(expectedException),
        };
        var reducer = new CopilotChatManagerToolCallChatReducer(client)
        {
            ConditionalCompressionTokenCountThreshold = 1,
            ForcedCompressionTokenCountThreshold = 1,
        };
        var assistantMessage = CopilotChatMessage.CreateAssistant(
            CopilotChatMessage.PlaceholderContent,
            isPresetInfo: false);
        _ = new CompressionToolCallObserver(assistantMessage, null, reducer);

        await reducer.ReduceAsync(
            [new ChatMessage(ChatRole.Assistant, "需要压缩")],
            CancellationToken.None);

        CopilotChatToolItem toolItem = assistantMessage.MessageItems.OfType<CopilotChatToolItem>().Single();
        Assert.AreEqual(expectedException.ToString(), toolItem.OutputText);
    }

    private static CodingAgent CreateAgent(
        CodingAgentOptions options,
        string? copilotInstructionsPath = null) =>
        new(options with { CopilotInstructionsPath = copilotInstructionsPath });

    private static CodingAgentOptions CreateProvider(
        string workspacePath,
        IReadOnlyList<AITool> tools)
    {
        Directory.CreateDirectory(Path.GetFullPath(workspacePath));
        return new CodingAgentOptions
        {
            LanguageServerCommand = $"missing-roslyn-{Guid.NewGuid():N}",
            AdditionalToolSources = tools.Count == 0 ? [] : [new FixedToolSource(tools)],
        };
    }

    private static CopilotChatManager CreateChatManager(
        FakeChatClient client,
        IMainThreadDispatcher? mainThreadDispatcher = null)
    {
        var chatManager = new CopilotChatManager
        {
            MainThreadDispatcher = mainThreadDispatcher,
        };
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

    private static async IAsyncEnumerable<ChatResponseUpdate> InterleavedStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatResponseUpdate[] updates =
        [
            new(ChatRole.Assistant, [new TextReasoningContent("思考一")]),
            new(ChatRole.Assistant, [new TextContent("正文一")]),
            new(ChatRole.Assistant, [new TextReasoningContent("思考二")]),
            new(ChatRole.Assistant, [new TextContent("正文二")]),
        ];

        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    private sealed class FixedToolSource(IReadOnlyList<AITool> tools) : ICodingWorkspaceToolSource
    {
        public IReadOnlyList<AITool> CreateTools(string workspacePath)
        {
            _ = workspacePath;
            return tools;
        }
    }

    private sealed class WorkspaceNamedToolSource : ICodingWorkspaceToolSource
    {
        public IReadOnlyList<AITool> CreateTools(string workspacePath) =>
        [
            AIFunctionFactory.Create(
                () => workspacePath,
                $"tool_{Path.GetFileName(workspacePath)}"),
        ];
    }

    private static TaskCompletionSource CompletedTaskSource()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        completion.TrySetResult();
        return completion;
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

    private static async IAsyncEnumerable<ChatResponseUpdate> WaitThenRespondAsync(
        TaskCompletionSource started,
        TaskCompletionSource release,
        string response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        started.TrySetResult();
        await release.Task.WaitAsync(cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(response)]);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CaptureAndRespondAsync(
        IEnumerable<ChatMessage> messages,
        TaskCompletionSource started,
        Action<IReadOnlyList<ChatMessage>> capture,
        string response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        capture(messages.ToArray());
        started.TrySetResult();
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(response)]);
        await Task.CompletedTask;
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
