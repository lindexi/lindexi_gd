using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;
using AgentLib.Coding;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomRoleExecutorTests
{
    [TestMethod(DisplayName = "Standard 执行器应通过标准发送传递系统提示词和本轮工具")]
    [Timeout(10000)]
    public async Task StandardExecutorShouldPassSystemPromptAndAdditionalTools()
    {
        IReadOnlyList<ChatMessage>? capturedMessages = null;
        ChatOptions? capturedOptions = null;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => CaptureStreamAsync(
                messages,
                options,
                value => capturedMessages = value,
                value => capturedOptions = value,
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        AITool additionalTool = AIFunctionFactory.Create(() => "host", "host_tool");
        var executor = new StandardChatRoomRoleExecutor();

        ChatRoomRoleExecutionResult result = await executor.RunAsync(
            new ChatRoomRoleExecutionContext(chatManager, "聊天室宿主指令", [additionalTool]),
            [new TextContent("任务")],
            CancellationToken.None);
        ChatRoomRoleExecutionCompletion completion = await result.CompletionTask;

        Assert.AreEqual("完成", completion.Content);
        Assert.IsFalse(completion.WasCanceled);
        Assert.AreSame(chatManager.SelectedSession.ChatMessages[2], result.AssistantChatMessage);
        Assert.IsTrue(capturedMessages!.Any(message =>
            message.Role == ChatRole.System && message.Text == "聊天室宿主指令"));
        Assert.IsTrue(capturedOptions!.Tools!.Any(tool => tool.Name == "host_tool"));
    }

    [TestMethod(DisplayName = "Standard 工作区工具应等待审批后再切换路径")]
    [Timeout(10000)]
    public async Task StandardWorkspaceToolShouldWaitForApprovalBeforeChangingPath()
    {
        string workspacePath = CreateTestDirectory();
        await using var workspaceManager = new ChatRoomManager();
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, options, cancellationToken) => InvokeToolAsync(
                options,
                "set-workspace-call",
                "set_workspace_path",
                new Dictionary<string, object?> { ["path"] = workspacePath },
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var executor = new StandardChatRoomRoleExecutor();

        ChatRoomRoleExecutionResult result = await executor.RunAsync(
            new ChatRoomRoleExecutionContext(
                chatManager,
                null,
                WorkspacePathTools.CreateSetWorkspacePathTool(workspaceManager)),
            [new TextContent("设置工作区")],
            CancellationToken.None);
        CopilotChatApprovalToolItem approvalItem = await WaitForApprovalItemAsync(
            result.AssistantChatMessage,
            "set_workspace_path");

        Assert.IsFalse(result.CompletionTask.IsCompleted);
        Assert.IsNull(workspaceManager.WorkspacePath);
        chatManager.ApproveToolExecution(approvalItem);
        ChatRoomRoleExecutionCompletion completion = await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsFalse(completion.WasCanceled);
        Assert.AreEqual("完成", completion.Content);
        Assert.AreEqual(Path.GetFullPath(workspacePath), workspaceManager.WorkspacePath);
        Assert.AreEqual(CopilotToolApprovalState.Approved, approvalItem.ApprovalState);
    }

    [TestMethod(DisplayName = "Standard 工作区工具被拒绝时不应切换路径")]
    [Timeout(10000)]
    public async Task StandardWorkspaceToolShouldNotChangePathWhenRejected()
    {
        string workspacePath = CreateTestDirectory();
        await using var workspaceManager = new ChatRoomManager();
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, options, cancellationToken) => InvokeToolAsync(
                options,
                "set-workspace-call",
                "set_workspace_path",
                new Dictionary<string, object?> { ["path"] = workspacePath },
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var executor = new StandardChatRoomRoleExecutor();
        ChatRoomRoleExecutionResult result = await executor.RunAsync(
            new ChatRoomRoleExecutionContext(
                chatManager,
                null,
                WorkspacePathTools.CreateSetWorkspacePathTool(workspaceManager)),
            [new TextContent("设置工作区")],
            CancellationToken.None);
        CopilotChatApprovalToolItem approvalItem = await WaitForApprovalItemAsync(
            result.AssistantChatMessage,
            "set_workspace_path");

        chatManager.RejectToolExecution(approvalItem, "拒绝测试路径");
        ChatRoomRoleExecutionCompletion completion = await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsFalse(completion.WasCanceled);
        Assert.AreEqual("完成", completion.Content);
        Assert.IsNull(workspaceManager.WorkspacePath);
        Assert.AreEqual(CopilotToolApprovalState.Rejected, approvalItem.ApprovalState);
    }

    [TestMethod(DisplayName = "Standard 审批等待取消时不应执行工作区工具")]
    [Timeout(10000)]
    public async Task StandardWorkspaceToolShouldNotChangePathWhenApprovalIsCanceled()
    {
        string workspacePath = CreateTestDirectory();
        await using var workspaceManager = new ChatRoomManager();
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, options, cancellationToken) => InvokeToolAsync(
                options,
                "set-workspace-call",
                "set_workspace_path",
                new Dictionary<string, object?> { ["path"] = workspacePath },
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        var executor = new StandardChatRoomRoleExecutor();
        using var cancellationTokenSource = new CancellationTokenSource();
        ChatRoomRoleExecutionResult result = await executor.RunAsync(
            new ChatRoomRoleExecutionContext(
                chatManager,
                null,
                WorkspacePathTools.CreateSetWorkspacePathTool(workspaceManager)),
            [new TextContent("设置工作区")],
            cancellationTokenSource.Token);
        _ = await WaitForApprovalItemAsync(result.AssistantChatMessage, "set_workspace_path");

        cancellationTokenSource.Cancel();
        ChatRoomRoleExecutionCompletion completion = await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(completion.WasCanceled);
        Assert.IsNull(workspaceManager.WorkspacePath);
    }

    [TestMethod(DisplayName = "Coding 执行器不应向 CodingAgent 传入聊天室提示词和本轮工具")]
    [Timeout(15000)]
    public async Task CodingExecutorShouldIgnoreChatRoomPromptAndTools()
    {
        IReadOnlyList<ChatMessage>? capturedMessages = null;
        ChatOptions? capturedOptions = null;
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => CaptureStreamAsync(
                messages,
                options,
                value => capturedMessages = value,
                value => capturedOptions = value,
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        string workspacePath = CreateTestDirectory();
        AITool hostTool = AIFunctionFactory.Create(() => "host", "host_tool");
        await using var executor = new CodingChatRoomRoleExecutor(
            new CodingAgent($"missing-roslyn-language-server-{Guid.NewGuid():N}"));
        await executor.SetWorkspacePathAsync(chatManager, workspacePath, CancellationToken.None);

        ChatRoomRoleExecutionResult result = await executor.RunAsync(
            new ChatRoomRoleExecutionContext(chatManager, "聊天室宿主指令", [hostTool]),
            [new TextContent("任务")],
            CancellationToken.None);
        ChatRoomRoleExecutionCompletion completion = await result.CompletionTask;

        Assert.AreEqual("完成", completion.Content);
        IReadOnlyList<ChatMessage> runMessages = capturedMessages!;
        IList<AITool> runTools = capturedOptions!.Tools!;
        Assert.IsFalse(runMessages.Any(message => message.Text.Contains("聊天室宿主指令", StringComparison.Ordinal)));
        Assert.IsTrue(runMessages.Any(message =>
            message.Role == ChatRole.System && message.Text.Contains("自动化编程代理", StringComparison.Ordinal)));
        Assert.IsFalse(runTools.Any(tool => tool.Name == "host_tool"));
        Assert.IsTrue(runTools.Any(tool => tool.Name == "get_projects_in_solution"));
    }

    [TestMethod(DisplayName = "Coding 执行器不应把内部 OperationCanceledException 误判为外部取消")]
    [Timeout(10000)]
    public async Task CodingExecutorShouldPropagateUnrelatedOperationCanceledException()
    {
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (_, _, _) => ThrowOperationCanceledAsync(),
        };
        CopilotChatManager chatManager = CreateChatManager(client);
        await using var executor = new CodingChatRoomRoleExecutor(new CodingAgent());

        ChatRoomRoleExecutionResult result = await executor.RunAsync(
            new ChatRoomRoleExecutionContext(chatManager, null, []),
            [new TextContent("任务")],
            CancellationToken.None);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await result.CompletionTask);
    }

    [TestMethod(DisplayName = "Coding 发言启动不应等待可见消息调度完成")]
    [Timeout(10000)]
    public async Task CodingExecutorRunShouldReleaseLifecycleLockBeforeMessageDispatchCompletes()
    {
        var dispatcher = new BlockingMainThreadDispatcher();
        var client = new FakeChatClient
        {
            OnGetStreamingResponseAsync = (messages, options, cancellationToken) => CaptureStreamAsync(
                messages,
                options,
                _ => { },
                _ => { },
                cancellationToken),
        };
        CopilotChatManager chatManager = CreateChatManager(client, dispatcher);
        await using var executor = new CodingChatRoomRoleExecutor(new CodingAgent());

        ChatRoomRoleExecutionResult result = await executor.RunAsync(
            new ChatRoomRoleExecutionContext(chatManager, null, []),
            [new TextContent("任务")],
            CancellationToken.None);
        await dispatcher.InvocationStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await executor
            .SetWorkspacePathAsync(chatManager, null, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(1));

        dispatcher.ReleaseInvocations.TrySetResult();
        ChatRoomRoleExecutionCompletion completion = await result.CompletionTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual("完成", completion.Content);
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

    private sealed class BlockingMainThreadDispatcher : IMainThreadDispatcher
    {
        public TaskCompletionSource InvocationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseInvocations { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool CheckAccess() => false;

        public async Task InvokeAsync(Func<Task> action)
        {
            InvocationStarted.TrySetResult();
            await ReleaseInvocations.Task;
            await action();
        }

        public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            InvocationStarted.TrySetResult();
            await ReleaseInvocations.Task;
            return await action();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CaptureStreamAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        Action<IReadOnlyList<ChatMessage>> captureMessages,
        Action<ChatOptions?> captureOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        captureMessages(messages.ToArray());
        captureOptions(options);
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("完成")]);
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowOperationCanceledAsync()
    {
        await Task.Yield();
        throw new OperationCanceledException("内部超时");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async Task<CopilotChatApprovalToolItem> WaitForApprovalItemAsync(
        CopilotChatMessage message,
        string toolName)
    {
        for (int i = 0; i < 100; i++)
        {
            CopilotChatApprovalToolItem? approvalItem = message.MessageItems
                .OfType<CopilotChatApprovalToolItem>()
                .FirstOrDefault(item => string.Equals(item.ToolName, toolName, StringComparison.Ordinal));
            if (approvalItem is not null)
            {
                return approvalItem;
            }

            await Task.Delay(20);
        }

        throw new AssertFailedException($"未在限定时间内找到 {toolName} 的审批片段。");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> InvokeToolAsync(
        ChatOptions? options,
        string callId,
        string toolName,
        IDictionary<string, object?> arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate(
            ChatRole.Assistant,
            [new FunctionCallContent(callId, toolName, arguments)]);
        AIFunction function = options?.Tools?
            .OfType<AIFunction>()
            .Single(tool => string.Equals(tool.Name, toolName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"未找到工具 {toolName}。");
        object? result = await function.InvokeAsync(
            new AIFunctionArguments(arguments.ToDictionary(pair => pair.Key, pair => pair.Value)),
            cancellationToken).ConfigureAwait(false);
        yield return new ChatResponseUpdate(
            ChatRole.Assistant,
            [new FunctionResultContent(callId, NormalizeResult(result))]);
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("完成")]);
    }

    private static object? NormalizeResult(object? result) => result is JsonElement jsonElement
        ? jsonElement.ValueKind == JsonValueKind.String ? jsonElement.GetString() : jsonElement.ToString()
        : result;

    private static string CreateTestDirectory()
    {
        string path = Path.Join(AppContext.BaseDirectory, nameof(ChatRoomRoleExecutorTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
