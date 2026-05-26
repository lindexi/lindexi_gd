using AgentLib.Tests.Fakes;
using AgentLib.Tools;
using Microsoft.Extensions.AI;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerCancellationTests
{
    [TestMethod]
    [Description("普通聊天流式处理中取消时应追加已取消提示并结束聊天状态")]
    public async Task SendMessageAsync_WhenStreamingChatIsCancelled_AppendsCancelledMessage()
    {
        var primaryChatClient = new FakeChatClient();
        var blockingResponse = new BlockingStreamingResponse();
        primaryChatClient.EnqueueStreamingResponse((_, _, cancellationToken) => blockingResponse.CreateAsyncEnumerable(cancellationToken));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task sendTask = context.ChatManager.SendMessageAsync("普通聊天取消测试", cancellationToken: cancellationTokenSource.Token);
        await blockingResponse.Started;
        cancellationTokenSource.Cancel();
        blockingResponse.Release();
        await sendTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.AreEqual("已取消", context.ChatManager.ChatMessages[^1].Content);
        Assert.IsTrue(context.ChatManager.ChatMessages[^1].IsPresetInfo);
    }

    [TestMethod]
    [Description("调用工具时取消应中断工具执行并追加已取消提示")]
    public async Task SendMessageAsync_WhenToolInvocationIsCancelled_AppendsCancelledMessage()
    {
        var primaryChatClient = new FakeChatClient();
        var blockingTool = new BlockingTool();
        string toolName = "BlockingTool";
        primaryChatClient.EnqueueStreamingResponseWithToolInvocation(
            callId: "tool-call-1",
            toolName: toolName,
            arguments: null,
            CopilotChatManagerTestContext.AssistantText("不会到达的结果"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task sendTask = context.ChatManager.SendMessageAsync(
            inputText: "工具取消测试",
            withHistory: true,
            createNewSession: false,
            tools: [blockingTool.CreateTool(toolName, "阻塞工具")],
            toolMode: ChatToolMode.RequireAny,
            cancellationToken: cancellationTokenSource.Token);
        await blockingTool.Started;
        cancellationTokenSource.Cancel();
        await sendTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.AreEqual("已取消", context.ChatManager.ChatMessages[^1].Content);
        Assert.IsTrue(context.ChatManager.ChatMessages[^1].IsPresetInfo);
    }

    [TestMethod]
    [Description("调用子智能体时取消应中断子智能体流式处理并追加已取消提示")]
    public async Task SendMessageAsync_WhenSubAgentInvocationIsCancelled_AppendsCancelledMessage()
    {
        var primaryChatClient = new FakeChatClient();
        primaryChatClient.EnqueueStreamingResponseWithToolInvocation(
            callId: "sub-agent-call-1",
            toolName: "InvokeSubAgent",
            arguments: new Dictionary<string, object?>
            {
                ["prompt"] = "请处理子任务",
                ["systemPrompt"] = null,
                ["subAgentType"] = "Flash"
            },
            CopilotChatManagerTestContext.AssistantText("不会到达的主结果"));

        var flashChatClient = new FakeChatClient();
        var blockingResponse = new BlockingStreamingResponse();
        flashChatClient.EnqueueStreamingResponse((_, _, cancellationToken) => blockingResponse.CreateAsyncEnumerable(cancellationToken));

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, flashChatClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task sendTask = context.ChatManager.SendMessageAsync(
            inputText: "子智能体取消测试",
            withHistory: true,
            createNewSession: false,
            tools: [],
            toolMode: ChatToolMode.RequireAny,
            cancellationToken: cancellationTokenSource.Token);
        await blockingResponse.Started;
        cancellationTokenSource.Cancel();
        blockingResponse.Release();
        await sendTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.AreEqual("已取消", context.ChatManager.ChatMessages[^1].Content);
        Assert.IsTrue(context.ChatManager.ChatMessages[^1].IsPresetInfo);
    }
}
