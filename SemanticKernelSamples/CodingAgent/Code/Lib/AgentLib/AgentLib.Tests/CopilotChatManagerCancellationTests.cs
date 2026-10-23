using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tests.Fakes;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

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
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => blockingResponse.CreateAsyncEnumerable(cancellationToken);
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task sendTask = context.ChatManager.SendMessageAsync(contents: [new TextContent("普通聊天取消测试")], cancellationToken: cancellationTokenSource.Token);
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
        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "tool-call-1", toolName, null, cancellationToken,
                CopilotChatManagerTestContext.AssistantText("不会到达的结果"));
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task sendTask = context.ChatManager.SendMessageAsync(
            contents: [new TextContent("工具取消测试")],
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
        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "sub-agent-call-1", "InvokeSubAgent", new Dictionary<string, object?>
            {
                ["prompt"] = "请处理子任务",
                ["systemPrompt"] = null,
                ["subAgentType"] = "Flash"
            }, cancellationToken, CopilotChatManagerTestContext.AssistantText("不会到达的主结果"));

        var flashChatClient = new FakeChatClient();
        var blockingResponse = new BlockingStreamingResponse();
        flashChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => blockingResponse.CreateAsyncEnumerable(cancellationToken);

        var context = CopilotChatManagerTestContext.Create(primaryChatClient, flashChatClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task sendTask = context.ChatManager.SendMessageAsync(
            contents: [new TextContent("子智能体取消测试")],
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

    [TestMethod]
    [Description("调用聊天管理层取消当前聊天时应中断新会话流式处理并追加已取消提示")]
    public async Task CancelCurrentChat_WhenNewSessionChatIsRunning_AppendsCancelledMessage()
    {
        var primaryChatClient = new FakeChatClient();
        var blockingResponse = new BlockingStreamingResponse();
        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => blockingResponse.CreateAsyncEnumerable(cancellationToken);
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        Task sendTask = context.ChatManager.SendMessageInNewSessionAsync(contents: [new TextContent("新会话取消测试")]);
        await blockingResponse.Started;
        context.ChatManager.CancelCurrentChat();
        blockingResponse.Release();
        await sendTask;

        Assert.IsFalse(context.ChatManager.IsChatting);
        Assert.AreEqual("已取消", context.ChatManager.ChatMessages[^1].Content);
        Assert.IsTrue(context.ChatManager.ChatMessages[^1].IsPresetInfo);
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

    private static object? NormalizeResult(object? result)
    {
        if (result is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                _ => jsonElement.ToString()
            };
        }

        return result;
    }
}
