using AgentLib.Tests.Fakes;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

namespace AgentLib.Tests;

[TestClass]
public class MainThreadDispatcherTests
{
    [TestMethod]
    [Description("当 MainThreadDispatcher 为 null 时，AddMessage 直接在当前线程执行")]
    public async Task SendMessage_WhenDispatcherIsNull_AddsMessageOnCurrentThread()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);
        Assert.IsNull(context.ChatManager.MainThreadDispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        await result.RunTask;

        Assert.IsGreaterThanOrEqualTo(2, context.ChatManager.ChatMessages.Count);
    }

    [TestMethod]
    [Description("当设置了 MainThreadDispatcher 时，InvokeAsync 被调用来调度 AddMessage")]
    public async Task SendMessage_WhenDispatcherIsSet_InvokeAsyncIsCalled()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        // 验证 dispatcher 已经传播到 SelectedSession
        Assert.IsNotNull(context.ChatManager.SelectedSession.MainThreadDispatcher,
            "设置 CopilotChatManager.MainThreadDispatcher 后，SelectedSession 应持有 dispatcher");

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        await result.RunTask;

        // 用户消息和助手消息各触发一次 InvokeAsync
        Assert.IsGreaterThanOrEqualTo(1, dispatcher.InvokeCount,
            "设置了 MainThreadDispatcher 后，应该通过 InvokeAsync 调度 AddMessage");
    }

    [TestMethod]
    [Description("当设置了 MainThreadDispatcher 时，消息仍然被正确添加到会话中")]
    public async Task SendMessage_WhenDispatcherIsSet_MessagesAreAdded()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        await result.RunTask;

        Assert.IsGreaterThanOrEqualTo(2, context.ChatManager.ChatMessages.Count,
            "即使通过调度器，消息也应该被正确添加到会话中");
    }

    [TestMethod]
    [Description("InvokeAsync 回调执行时，线程 ID 与捕获的主线程一致，Thread 实例相同")]
    public async Task SendMessage_WhenDispatcherIsSet_CallbackExecutesOnCapturedMainThread()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        await result.RunTask;

        Assert.IsGreaterThan(0, dispatcher.InvokeHistory.Count,
            "应该有 InvokeAsync 调用记录");

        foreach (var record in dispatcher.InvokeHistory)
        {
            Assert.AreEqual(dispatcher.MainThreadId, record.CallbackThreadId,
                "回调执行时的线程 ID 应与捕获的主线程 ID 一致");
            Assert.AreSame(dispatcher.MainThread, record.CallbackThread,
                "回调执行时的 Thread 实例应与捕获的主线程实例相同");
        }
    }

    [TestMethod]
    [Description("InvokeAsync 回调执行期间，SynchronizationContext.Current 是自定义的单线程上下文")]
    public async Task SendMessage_WhenDispatcherIsSet_SynchronizationContextIsSet()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("回复"));

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        await result.RunTask;

        Assert.IsGreaterThan(0, dispatcher.SynchronizationContextSnapshots.Count,
            "应该有 SynchronizationContext 快照记录");

        foreach (var snapshot in dispatcher.SynchronizationContextSnapshots)
        {
            Assert.IsTrue(snapshot.IsCustomContext,
                "回调执行期间 SynchronizationContext.Current 应该是自定义的单线程上下文");
            Assert.AreEqual(nameof(SingleThreadSynchronizationContext), snapshot.ContextType,
                "SynchronizationContext 类型应该是 SingleThreadSynchronizationContext");
        }
    }

    [TestMethod]
    [Description("大量流式更新后，所有消息添加操作均通过调度器在主线程上执行")]
    public async Task SendMessage_WithManyStreamingUpdates_ThreadRemainsOnMainThread()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();

        const int updateCount = 100;
        var updates = new ChatResponseUpdate[updateCount];
        for (int i = 0; i < updateCount; i++)
        {
            updates[i] = CopilotChatManagerTestContext.AssistantText($"更新{i}");
        }

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken, updates);

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("你好"));

        await result.RunTask;

        Assert.IsGreaterThan(0, dispatcher.InvokeHistory.Count,
            "应该有 InvokeAsync 调用记录");

        foreach (var record in dispatcher.InvokeHistory)
        {
            Assert.AreEqual(dispatcher.MainThreadId, record.CallbackThreadId,
                "每次回调执行时的线程 ID 应与捕获的主线程 ID 一致");
            Assert.AreSame(dispatcher.MainThread, record.CallbackThread,
                "每次回调执行时的 Thread 实例应与捕获的主线程实例相同");
        }

        foreach (var snapshot in dispatcher.SynchronizationContextSnapshots)
        {
            Assert.IsTrue(snapshot.IsCustomContext,
                "每次回调执行期间 SynchronizationContext.Current 应该是自定义的单线程上下文");
        }
    }

    [TestMethod]
    [Description("单轮工具调用后，所有消息添加操作均通过调度器在主线程上执行")]
    public async Task SendMessage_WithSingleToolCall_ThreadRemainsOnMainThread()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();
        string toolName = "TestTool";

        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "call-1", toolName, null, cancellationToken,
                CopilotChatManagerTestContext.AssistantText("工具调用完成"));

        var testTool = AIFunctionFactory.Create(() => "工具结果", toolName, "测试工具");
        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("调用工具") { Tools = [testTool] });

        await result.RunTask;

        Assert.IsGreaterThan(0, dispatcher.InvokeHistory.Count,
            "应该有 InvokeAsync 调用记录");

        foreach (var record in dispatcher.InvokeHistory)
        {
            Assert.AreEqual(dispatcher.MainThreadId, record.CallbackThreadId,
                "工具调用后回调执行时的线程 ID 应与捕获的主线程 ID 一致");
            Assert.AreSame(dispatcher.MainThread, record.CallbackThread,
                "工具调用后回调执行时的 Thread 实例应与捕获的主线程实例相同");
        }
    }

    [TestMethod]
    [Description("多轮工具调用后，所有消息添加操作均通过调度器在主线程上执行")]
    public async Task SendMessage_WithMultipleToolCalls_ThreadRemainsOnMainThread()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();

        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateMultiToolInvocationAsyncEnumerable(options, cancellationToken);

        var tool1 = AIFunctionFactory.Create(() => "结果1", "Tool1", "工具1");
        var tool2 = AIFunctionFactory.Create(() => "结果2", "Tool2", "工具2");
        var tool3 = AIFunctionFactory.Create(() => "结果3", "Tool3", "工具3");

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("多轮工具调用") { Tools = [tool1, tool2, tool3] });

        await result.RunTask;

        Assert.IsGreaterThan(0, dispatcher.InvokeHistory.Count,
            "应该有 InvokeAsync 调用记录");

        foreach (var record in dispatcher.InvokeHistory)
        {
            Assert.AreEqual(dispatcher.MainThreadId, record.CallbackThreadId,
                "多轮工具调用后回调执行时的线程 ID 应与捕获的主线程 ID 一致");
            Assert.AreSame(dispatcher.MainThread, record.CallbackThread,
                "多轮工具调用后回调执行时的 Thread 实例应与捕获的主线程实例相同");
        }

        foreach (var snapshot in dispatcher.SynchronizationContextSnapshots)
        {
            Assert.IsTrue(snapshot.IsCustomContext,
                "多轮工具调用后每次回调执行期间 SynchronizationContext.Current 应该是自定义的单线程上下文");
        }
    }

    [TestMethod]
    [Description("工具调用完成后，后续助手文本消息仍在主线程上添加，未被错误调度回后台")]
    public async Task SendMessage_AfterToolExecution_DoesNotDispatchBackToBackground()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();
        string toolName = "VerifyTool";

        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "call-1", toolName, null, cancellationToken,
                CopilotChatManagerTestContext.AssistantText("工具调用后的文本"));

        var testTool = AIFunctionFactory.Create(() => "验证结果", toolName, "验证工具");
        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        int invokeCountBeforeTool = dispatcher.InvokeCount;

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("验证线程") { Tools = [testTool] });

        await result.RunTask;

        int invokeCountAfterTool = dispatcher.InvokeCount;

        Assert.IsGreaterThan(invokeCountBeforeTool, invokeCountAfterTool,
            "工具调用后应该继续有 InvokeAsync 调用（用于添加后续消息）");

        Assert.IsGreaterThanOrEqualTo(2, dispatcher.InvokeHistory.Count,
            "应该有多次 InvokeAsync 调用记录");

        foreach (var record in dispatcher.InvokeHistory)
        {
            Assert.AreEqual(dispatcher.MainThreadId, record.CallbackThreadId,
                "所有回调（包括工具调用后的）执行时的线程 ID 应与捕获的主线程 ID 一致");
        }
    }

    [TestMethod]
    [Description("严格模式下，CheckAccess 在非主线程上调用时抛出异常")]
    public void CheckAccess_WhenOnWrongThreadWithStrictMode_ThrowsException()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false, strictMode: true);

        InvalidOperationException? exception = null;
        try
        {
            dispatcher.CheckAccess();
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        Assert.IsNotNull(exception,
            "严格模式下，CheckAccess 在非主线程调用时应抛出 InvalidOperationException");
        Assert.IsTrue(exception.Message.Contains("非主线程"),
            "异常消息应包含'非主线程'");
        Assert.IsTrue(exception.Message.Contains(dispatcher.MainThreadId.ToString()),
            "异常消息应包含期望的主线程 ID");
    }

    [TestMethod]
    [Description("已在主线程时，CheckAccess 返回 true 且不抛出异常")]
    public void CheckAccess_WhenAlreadyOnMainThread_ReturnsTrue()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: true, strictMode: true);

        bool accessResult = dispatcher.CheckAccess();

        Assert.IsTrue(accessResult,
            "已在主线程时，CheckAccess 应返回 true");
    }

    [TestMethod]
    [Description("流式过程中发生异常时，异常消息仍在主线程上添加")]
    public async Task SendMessage_WhenExceptionOccurs_ErrorMessageAddedOnMainThread()
    {
        var dispatcher = new TestMainThreadDispatcher(isMainThread: false);
        var primaryChatClient = new FakeChatClient();

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesWithExceptionAsync(cancellationToken);

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, mainThreadDispatcher: dispatcher);

        SendMessageResult result = context.ChatManager.SendMessage(
            new SendMessageRequest("触发异常"));

        await result.RunTask;

        Assert.IsGreaterThan(0, dispatcher.InvokeHistory.Count,
            "即使发生异常，也应该有 InvokeAsync 调用记录");

        foreach (var record in dispatcher.InvokeHistory)
        {
            Assert.IsTrue(record.CheckAccessDuringCallback,
                "异常路径下回调执行期间 CheckAccess 应返回 true");
        }

        Assert.IsGreaterThanOrEqualTo(2, context.ChatManager.ChatMessages.Count,
            "发生异常后，用户消息和异常消息应被添加到会话中");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params ChatResponseUpdate[] updates)
    {
        await Task.CompletedTask;
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingUpdatesWithExceptionAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return CopilotChatManagerTestContext.AssistantText("部分回复");
        await Task.Yield();
        throw new InvalidOperationException("模拟流式处理中的异常");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolInvocationAsyncEnumerable(
        ChatOptions? options,
        string callId,
        string toolName,
        IDictionary<string, object?>? arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params ChatResponseUpdate[] trailingUpdates)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionCall(callId, toolName, arguments);

        AITool tool = options?.Tools?.FirstOrDefault(candidate => string.Equals(candidate.Name, toolName, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException($"未找到名为 {toolName} 的工具。");

        if (tool is not AIFunction function)
        {
            throw new InvalidOperationException($"工具 {toolName} 不是可调用函数。");
        }

        object? result = await function.InvokeAsync(new AIFunctionArguments(arguments?.ToDictionary(pair => pair.Key, pair => pair.Value)), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionResult(callId, NormalizeResult(result));

        foreach (ChatResponseUpdate update in trailingUpdates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateMultiToolInvocationAsyncEnumerable(
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string[] toolNames = ["Tool1", "Tool2", "Tool3"];

        for (int i = 0; i < toolNames.Length; i++)
        {
            string callId = $"call-{i + 1}";
            string toolName = toolNames[i];

            cancellationToken.ThrowIfCancellationRequested();
            yield return CopilotChatManagerTestContext.AssistantFunctionCall(callId, toolName, null);

            AITool tool = options?.Tools?.FirstOrDefault(candidate => string.Equals(candidate.Name, toolName, StringComparison.Ordinal))
                          ?? throw new InvalidOperationException($"未找到名为 {toolName} 的工具。");

            if (tool is not AIFunction function)
            {
                throw new InvalidOperationException($"工具 {toolName} 不是可调用函数。");
            }

            object? result = await function.InvokeAsync(new AIFunctionArguments(), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            yield return CopilotChatManagerTestContext.AssistantFunctionResult(callId, NormalizeResult(result));

            await Task.Yield();
        }

        yield return CopilotChatManagerTestContext.AssistantText("多轮工具调用完成");
    }

    private static object? NormalizeResult(object? result)
    {
        if (result is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
                _ => jsonElement.ToString()
            };
        }

        return result;
    }
}