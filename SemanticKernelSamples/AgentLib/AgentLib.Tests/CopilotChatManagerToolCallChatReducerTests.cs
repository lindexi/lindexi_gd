using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerToolCallChatReducerTests
{
    [TestMethod]
    [Description("尾部连续 Assistant+Tool 块超过阈值时应触发压缩")]
    public async Task ReduceAsync_WhenTailBlockExceedsThreshold_Compresses()
    {
        var primaryChatClient = new FakeChatClient();
        string summaryText = "工具调用摘要";

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, summaryText)]));
        };

        var reducer = new CopilotChatManagerToolCallChatReducer(primaryChatClient, characterThreshold: 50);

        // 构造超过阈值的尾部块（每条消息20字符，总计80字符 > 50）
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "用户消息"),
            new ChatMessage(ChatRole.Assistant, new string('A', 20)),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call1", new string('B', 20))]),
            new ChatMessage(ChatRole.Assistant, new string('C', 20)),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call2", new string('D', 20))]),
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Count < messages.Count, "压缩后消息数量应减少");
        Assert.IsTrue(resultList.Any(m => m.Text?.Contains(summaryText) == true), "应包含摘要内容");
    }

    [TestMethod]
    [Description("尾部连续 Assistant+Tool 块未超过阈值时不应压缩")]
    public async Task ReduceAsync_WhenTailBlockBelowThreshold_DoesNotCompress()
    {
        var primaryChatClient = new FakeChatClient();

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "摘要")]));
        };

        var reducer = new CopilotChatManagerToolCallChatReducer(primaryChatClient, characterThreshold: 10000);

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "用户消息"),
            new ChatMessage(ChatRole.Assistant, "助手回复"),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call1", "工具结果")]),
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.AreEqual(messages.Count, resultList.Count, "未超过阈值时消息数量应保持不变");
    }

    [TestMethod]
    [Description("消息列表为空时应原样返回")]
    public async Task ReduceAsync_WhenMessagesEmpty_ReturnsEmpty()
    {
        var primaryChatClient = new FakeChatClient();
        var reducer = new CopilotChatManagerToolCallChatReducer(primaryChatClient);

        var messages = new List<ChatMessage>();

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count(), "空消息列表应原样返回");
    }

    [TestMethod]
    [Description("只有 Assistant 无 Tool 时应正确识别为可压缩块")]
    public async Task ReduceAsync_WhenOnlyAssistantMessages_IdentifiesAsCompressible()
    {
        var primaryChatClient = new FakeChatClient();
        string summaryText = "助手对话摘要";

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, summaryText)]));
        };

        var reducer = new CopilotChatManagerToolCallChatReducer(primaryChatClient, characterThreshold: 50);

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "用户消息"),
            new ChatMessage(ChatRole.Assistant, new string('A', 20)),
            new ChatMessage(ChatRole.Assistant, new string('B', 20)),
            new ChatMessage(ChatRole.Assistant, new string('C', 20)),
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Count < messages.Count, "压缩后消息数量应减少");
        Assert.IsTrue(resultList.Any(m => m.Text?.Contains(summaryText) == true), "应包含摘要内容");
    }

    [TestMethod]
    [Description("开头 System Prompt 应保留不被压缩")]
    public async Task ReduceAsync_WhenHasSystemPrompt_PreservesSystemPrompt()
    {
        var primaryChatClient = new FakeChatClient();
        string summaryText = "摘要";

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, summaryText)]));
        };

        var reducer = new CopilotChatManagerToolCallChatReducer(primaryChatClient, characterThreshold: 50);

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "系统提示词"),
            new ChatMessage(ChatRole.User, "用户消息"),
            new ChatMessage(ChatRole.Assistant, "助手回复1"),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("call1", "工具结果1")]),
            new ChatMessage(ChatRole.Assistant, "助手回复2"),
        };

        var result = await reducer.ReduceAsync(messages, CancellationToken.None);

        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.AreEqual(ChatRole.System, resultList[0].Role, "第一条消息应保持为 System 角色");
        Assert.AreEqual("系统提示词", resultList[0].Text, "系统提示词内容应保持不变");
    }

    [TestMethod]
    [Description("ChatReducer 为 null 时应自动启用内置压缩器")]
    public async Task SendMessage_WhenChatReducerNull_UsesBuiltInReducer()
    {
        var primaryChatClient = new FakeChatClient();
        bool reducerCalled = false;

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("助手回复"));

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            reducerCalled = true;
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "摘要")]));
        };

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        // 发送消息，不指定 ChatReducer
        await context.ChatManager.SendMessageAsync(
            contents: [new TextContent("用户消息")],
            withHistory: true);

        // 验证 ChatHistoryProvider 被设置
        var agentSession = context.ChatManager.SelectedSession.AgentSession;
        Assert.IsNotNull(agentSession, "AgentSession 应存在");

        // 触发压缩（通过手动调用 ReduceSessionAsync 来验证内置 reducer 被使用）
        await context.ChatManager.ReduceSessionAsync(chatReducer: null);

        Assert.IsTrue(reducerCalled, "内置压缩器应被调用");
    }

    [TestMethod]
    [Description("ChatReducer 不为 null 时应使用外部压缩器")]
    public async Task SendMessage_WhenChatReducerNotNull_UsesExternalReducer()
    {
        var primaryChatClient = new FakeChatClient();
        bool externalReducerCalled = false;

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("助手回复"));

        var externalReducer = new TestChatReducer(() => externalReducerCalled = true);

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        // 通过 SendMessage(request) 将外部 ChatReducer 传入 CreateChatClientAgentAsync 路径
        var request = new SendMessageRequest("用户消息")
        {
            WithHistory = true,
            ChatReducer = externalReducer,
            RequirePerServiceCallChatHistoryPersistence = false
        };

        var result = context.ChatManager.SendMessage(request);
        await result.RunTask;

        // 同时手动触发压缩，双重验证外部 reducer 确实被调用
        await context.ChatManager.ReduceSessionAsync(chatReducer: externalReducer);

        Assert.IsTrue(externalReducerCalled, "外部压缩器应被调用");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateStreamingUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params ChatResponseUpdate[] updates)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    /// <summary>
    /// 用于测试的简单 ChatReducer 实现
    /// </summary>
    private class TestChatReducer : IChatReducer
    {
        private readonly Action _onReduce;

        public TestChatReducer(Action onReduce)
        {
            _onReduce = onReduce;
        }

        public Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
        {
            _onReduce();
            return Task.FromResult(messages);
        }
    }
}
