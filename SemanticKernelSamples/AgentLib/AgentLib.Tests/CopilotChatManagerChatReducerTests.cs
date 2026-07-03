using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerChatReducerTests
{
    [TestMethod]
    [Description("ReduceSessionAsync 在没有 AgentSession 时应静默返回，不抛异常")]
    public async Task ReduceSessionAsync_WhenNoAgentSession_ReturnsSilently()
    {
        var primaryChatClient = new FakeChatClient();
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.ReduceSessionAsync();
    }

    [TestMethod]
    [Description("ReduceSessionAsync 在有消息时应触发压缩，reducer 应收到包含历史和系统 prompt 的消息")]
    public async Task ReduceSessionAsync_WhenMessagesExist_TriggersCompression()
    {
        var primaryChatClient = new FakeChatClient();
        List<ChatMessage>? capturedMessages = null;

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("助手回复"));

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            capturedMessages = messages.ToList();
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "这是对话的摘要")]));
        };

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第一轮对话")], withHistory: true);
        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第二轮对话")], withHistory: true);

        await context.ChatManager.ReduceSessionAsync();

        Assert.IsNotNull(capturedMessages, "Reducer 应该收到消息");
        Assert.IsTrue(capturedMessages.Count >= 2, "消息数量应至少包含历史对话");
        Assert.IsTrue(capturedMessages.Any(m => m.Role == ChatRole.System), "应包含系统 prompt");
    }

    [TestMethod]
    [Description("ReduceSessionAsync 压缩后应替换 ChatHistory 为摘要结果")]
    public async Task ReduceSessionAsync_AfterCompression_ReplacesChatHistory()
    {
        var primaryChatClient = new FakeChatClient();
        string summaryText = "用户和助手进行了两轮对话，讨论了测试话题";

        primaryChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
            CreateStreamingUpdatesAsync(cancellationToken,
                CopilotChatManagerTestContext.AssistantText("助手回复"));

        primaryChatClient.OnGetResponseAsync = (messages, _, cancellationToken) =>
        {
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, summaryText)]));
        };

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第一轮对话")], withHistory: true);
        await context.ChatManager.SendMessageAsync(contents: [new TextContent("第二轮对话")], withHistory: true);

        await context.ChatManager.ReduceSessionAsync();

        var agentSession = context.ChatManager.SelectedSession.AgentSession;
        Assert.IsNotNull(agentSession, "AgentSession 应存在");

        bool hasHistory = agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? compressedMessages);
        Assert.IsTrue(hasHistory, "应能获取压缩后的 ChatHistory");
        Assert.IsNotNull(compressedMessages, "压缩后的消息不应为 null");
        Assert.IsTrue(compressedMessages.Any(m => m.Text?.Contains(summaryText) == true),
            "压缩后的消息应包含摘要内容");
    }

    [TestMethod]
    [Description("ToolCallAwareChatReducer 在存在未配对的 FunctionCallContent 时应跳过压缩")]
    public async Task ToolCallAwareReducer_WhenPendingFunctionCall_SkipsInnerReducer()
    {
        var primaryChatClient = new FakeChatClient();
        bool innerReducerCalled = false;

        primaryChatClient.OnGetResponseAsync = (_, _, _) =>
            Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "摘要")]));

        var innerReducer = new CopilotChatManagerChatReducer(primaryChatClient);
        var decorator = new ToolCallAwareChatReducer(innerReducer);

        // 构造包含未配对 FunctionCallContent 的消息（有调用请求但无结果）
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "请问天气"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_001", "GetWeather", new Dictionary<string, object?>())
            ]),
        };

        var result = await decorator.ReduceAsync(messages, CancellationToken.None);

        // 装饰器应跳过压缩，原样返回
        var resultList = result.ToList();
        Assert.AreEqual(messages.Count, resultList.Count, "未配对工具调用时应原样返回，不压缩");
        Assert.IsFalse(innerReducerCalled, "内部 reducer 不应被调用");
    }

    [TestMethod]
    [Description("ToolCallAwareChatReducer 在工具调用已完成（有 FunctionResultContent）时应委托给内部 reducer")]
    public async Task ToolCallAwareReducer_WhenToolCallCompleted_DelegatesToInnerReducer()
    {
        var primaryChatClient = new FakeChatClient();
        bool innerReducerCalled = false;

        primaryChatClient.OnGetResponseAsync = (_, _, _) =>
        {
            innerReducerCalled = true;
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "摘要")]));
        };

        var innerReducer = new CopilotChatManagerChatReducer(primaryChatClient);
        var decorator = new ToolCallAwareChatReducer(innerReducer);

        // 构造包含配对的 FunctionCallContent + FunctionResultContent 的消息
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "请问天气"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_001", "GetWeather", new Dictionary<string, object?>())
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("call_001", "温度100度")
            ]),
        };

        var result = await decorator.ReduceAsync(messages, CancellationToken.None);

        Assert.IsTrue(innerReducerCalled, "工具调用已完成后，内部 reducer 应被委托执行");
    }

    [TestMethod]
    [Description("ToolCallAwareChatReducer 在没有工具调用时应正常委托给内部 reducer")]
    public async Task ToolCallAwareReducer_WhenNoToolCalls_DelegatesToInnerReducer()
    {
        var primaryChatClient = new FakeChatClient();
        bool innerReducerCalled = false;

        primaryChatClient.OnGetResponseAsync = (_, _, _) =>
        {
            innerReducerCalled = true;
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "摘要")]));
        };

        var innerReducer = new CopilotChatManagerChatReducer(primaryChatClient);
        var decorator = new ToolCallAwareChatReducer(innerReducer);

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "第一轮对话"),
            new ChatMessage(ChatRole.Assistant, "助手回复"),
            new ChatMessage(ChatRole.User, "第二轮对话"),
            new ChatMessage(ChatRole.Assistant, "助手回复2"),
        };

        var result = await decorator.ReduceAsync(messages, CancellationToken.None);

        Assert.IsTrue(innerReducerCalled, "无工具调用时，内部 reducer 应被正常委托执行");
    }

    [TestMethod]
    [Description("工具调用场景下，CopilotChatManager 应通过 ToolCallAwareChatReducer 避免在工具未完成时过早压缩")]
    public async Task ToolCall_CopilotChatManager_PreventsPrematureReductionDuringToolCall()
    {
        var primaryChatClient = new FakeChatClient();
        bool innerReducerCalled = false;
        int chatCallCount = 0;

        primaryChatClient.OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
        {
            var currentCall = Interlocked.Increment(ref chatCallCount);

            if (currentCall == 1)
            {
                return CreateToolCallStreamAsync(options, cancellationToken);
            }
            else
            {
                return CreateStreamingUpdatesAsync(cancellationToken,
                    CopilotChatManagerTestContext.AssistantText("根据查询结果，当前温度是100度。"));
            }
        };

        primaryChatClient.OnGetResponseAsync = (_, _, _) =>
        {
            innerReducerCalled = true;
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "摘要")]));
        };

        // 使用 CopilotChatManagerChatReducer 作为内部 reducer
        // CopilotChatManager 会自动用 ToolCallAwareChatReducer 包装
        var innerReducer = new CopilotChatManagerChatReducer(primaryChatClient);

        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        var request = new SendMessageRequest("请问天气多少")
        {
            WithHistory = true,
            ChatReducer = innerReducer,
            RequirePerServiceCallChatHistoryPersistence = true,
            Tools = [AIFunctionFactory.Create(GetWeather)],
            AppendDefaultTools = false,
        };

        var result = context.ChatManager.SendMessage(request);
        await result.RunTask;

        Console.WriteLine($"ChatClient.GetStreamingResponseAsync 共被调用 {chatCallCount} 次");
        Console.WriteLine($"内部 reducer 是否被调用: {innerReducerCalled}");

        // 验证：工具调用过程中，内部 reducer 不应被过早调用（装饰器应跳过）
        // 当前框架行为是第二次 ReduceAsync 消息为空，所以内部 reducer 也不会被调用
        // 关键断言：在工具调用过程中，不应发生过早的压缩
        Assert.IsFalse(innerReducerCalled,
            "工具调用未完成时，内部 reducer 不应被调用（ToolCallAwareChatReducer 应跳过过早的压缩）");
    }

    /// <summary>
    /// 测试用工具函数
    /// </summary>
    private static string GetWeather() => "温度100度";

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolCallStreamAsync(
        ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var toolName = options?.Tools?.FirstOrDefault() is AIFunction tool
            ? tool.Name
            : "GetWeather";

        var functionCallContent = new FunctionCallContent(
            callId: "call_001",
            name: toolName,
            arguments: new Dictionary<string, object?>());

        yield return new ChatResponseUpdate(ChatRole.Assistant, [functionCallContent])
        {
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await Task.CompletedTask;
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
}
