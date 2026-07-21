using AgentLib.ChatRoom.Model;
using AgentLib.Coding;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;

using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

namespace AgentLib.ChatRoom.Tests;

[TestClass]
public sealed class ChatRoomRoleExecutorTests
{
    [TestMethod(DisplayName = "Standard 执行器应通过标准发送传递系统提示词和本轮工具")]
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

    [TestMethod(DisplayName = "Coding 执行器不应向 CodingAgent 传入聊天室提示词和本轮工具")]
    [Timeout(15000, CooperativeCancellation = true)]
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

    private static string CreateTestDirectory()
    {
        string path = Path.Join(AppContext.BaseDirectory, nameof(ChatRoomRoleExecutorTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
