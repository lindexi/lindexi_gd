using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Runtime.CompilerServices;

#pragma warning disable MAAI001

namespace AgentLib.Tests;

[TestClass]
public class AgentSessionStreamingHelperTests
{
    private const string SystemMessageText = "系统消息";
    private const string UserMessageText = "用户消息";
    private const string PartialAssistantText = "局部助手响应";
    private const string CompletedAssistantText = "重试完成响应";

    [TestMethod(DisplayName = "暂时性异常后应基于已收集历史直接重试")]
    [DataRow("HttpRequestException")]
    [DataRow("IOException")]
    [DataRow("TimeoutException")]
    [Timeout(10000)]
    public async Task RunWithHistoryCompletion_WhenTransientExceptionOccurs_RetriesWithCollectedHistory(string exceptionType)
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        var receivedRequests = new List<(ChatRole Role, string Text)[]>();
        fakeChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            receivedRequests.Add([.. messages.Select(message => (message.Role, message.Text))]);
            return Interlocked.Increment(ref callCount) == 1
                ? CreateFailingTextStreamAsync(cancellationToken, PartialAssistantText, CreateException(exceptionType))
                : CreateTextStreamAsync(cancellationToken, CompletedAssistantText);
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

        List<AgentResponseUpdate> updates = await CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(
            CreateInputMessages(), session)).ConfigureAwait(false);

        Assert.AreEqual(2, callCount);
        Assert.AreEqual($"{PartialAssistantText}{CompletedAssistantText}", string.Concat(updates.Select(update => update.Text)));
        Assert.HasCount(2, receivedRequests);
        Assert.AreEqual(1, receivedRequests[1].Count(message => message.Role == ChatRole.User));
        Assert.AreEqual(UserMessageText, receivedRequests[1].Single(message => message.Role == ChatRole.User).Text);
        Assert.AreEqual(PartialAssistantText, receivedRequests[1].Last(message => message.Role == ChatRole.Assistant).Text);
    }

    [TestMethod(DisplayName = "暂时性异常重试耗尽后应抛出最后一次异常")]
    [Timeout(10000)]
    public async Task RunWithHistoryCompletion_WhenRetriesAreExhausted_ThrowsLastException()
    {
        var fakeChatClient = new FakeChatClient();
        HttpRequestException[] expectedExceptions =
        [
            new("第一次失败"),
            new("第二次失败"),
            new("第三次失败"),
            new("第四次失败"),
        ];
        var callCount = 0;
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
        {
            int currentCall = Interlocked.Increment(ref callCount);
            return CreateFailingTextStreamAsync(cancellationToken, $"第 {currentCall} 次局部响应", expectedExceptions[currentCall - 1]);
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

        HttpRequestException actualException = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session))).ConfigureAwait(false);

        Assert.AreEqual(4, callCount);
        Assert.AreSame(expectedExceptions[^1], actualException);
    }

    [TestMethod(DisplayName = "非暂时性异常应保持原样传播且不重试")]
    [Timeout(10000)]
    public async Task RunWithHistoryCompletion_WhenExceptionIsNotTransient_DoesNotRetry()
    {
        var fakeChatClient = new FakeChatClient();
        var expectedException = new InvalidOperationException("不可重试异常");
        var callCount = 0;
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
        {
            Interlocked.Increment(ref callCount);
            return CreateFailingTextStreamAsync(cancellationToken, PartialAssistantText, expectedException);
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

        InvalidOperationException actualException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session))).ConfigureAwait(false);

        Assert.AreEqual(1, callCount);
        Assert.AreSame(expectedException, actualException);
    }

    [TestMethod(DisplayName = "工具调用已持久化分段响应时不应再次追加整轮助手内容")]
    [Timeout(10000)]
    public async Task RunWithHistoryCompletion_WhenToolCallHistoryIsPersisted_DoesNotAppendAggregatedDuplicate()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        AITool tool = AIFunctionFactory.Create(GetWeather);
        fakeChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            Interlocked.Increment(ref callCount) == 1
                ? CreateToolCallStreamAsync(options, cancellationToken)
                : CreateTextAndReasoningStreamAsync(cancellationToken);
        ChatClientAgent agent = CreateAgent(fakeChatClient, [tool]);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

        await CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session)).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.AreEqual(1, messages.SelectMany(message => message.Contents).OfType<TextReasoningContent>()
            .Count(content => content.Text == "最终思考"));
        Assert.AreEqual(1, messages.SelectMany(message => message.Contents).OfType<TextContent>()
            .Count(content => content.Text == "最终正文"));
    }

    private static Exception CreateException(string exceptionType)
    {
        return exceptionType switch
        {
            "HttpRequestException" => new HttpRequestException("HTTP 请求失败"),
            "IOException" => new IOException("I/O 失败"),
            "TimeoutException" => new TimeoutException("请求超时"),
            _ => throw new ArgumentOutOfRangeException(nameof(exceptionType), exceptionType, "未知异常类型。"),
        };
    }

    private static ChatClientAgent CreateAgent(FakeChatClient fakeChatClient, IReadOnlyList<AITool>? tools = null)
    {
        return fakeChatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Tools = tools is null ? null : [.. tools],
            },
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()),
            RequirePerServiceCallChatHistoryPersistence = true,
        });
    }

    private static ChatMessage[] CreateInputMessages()
    {
        return
        [
            new ChatMessage(ChatRole.System, SystemMessageText),
            new ChatMessage(ChatRole.User, UserMessageText),
        ];
    }

    private static async Task<List<AgentResponseUpdate>> CollectUpdatesAsync(
        IAsyncEnumerable<AgentResponseUpdate> updates)
    {
        var result = new List<AgentResponseUpdate>();
        await foreach (AgentResponseUpdate update in updates.ConfigureAwait(false))
        {
            result.Add(update);
        }

        return result;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateTextStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params string[] texts)
    {
        foreach (string text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return CopilotChatManagerTestContext.AssistantText(text);
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateFailingTextStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        string text,
        Exception exception)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantText(text);
        await Task.Yield();
        throw exception;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolCallStreamAsync(
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AIFunction tool = options?.Tools?.OfType<AIFunction>().Single()
            ?? throw new InvalidOperationException("未找到测试工具。");
        yield return CopilotChatManagerTestContext.AssistantReasoning("调用工具前思考");
        yield return CopilotChatManagerTestContext.AssistantFunctionCall(
            "weather-call-1",
            tool.Name,
            new Dictionary<string, object?>());
        await Task.Yield();
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateTextAndReasoningStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantReasoning("最终思考");
        yield return CopilotChatManagerTestContext.AssistantText("最终正文");
        await Task.Yield();
    }

    [System.ComponentModel.Description("获取天气")]
    private static string GetWeather() => "天气晴朗";
}
