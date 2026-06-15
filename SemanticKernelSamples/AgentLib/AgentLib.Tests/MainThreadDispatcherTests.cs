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
}