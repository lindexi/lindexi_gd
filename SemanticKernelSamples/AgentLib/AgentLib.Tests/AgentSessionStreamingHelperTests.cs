using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Net;
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

    [TestMethod(DisplayName = "HTTP 400 后应移除未配对工具调用并重试")]
    [Timeout(10000)]
    public async Task RunWithHistoryCompletion_WhenBadRequestOccurs_RemovesUnpairedToolCallAndRetries()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        ChatMessage[]? retryMessages = null;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                return CreateFailingFunctionCallStreamAsync(
                    cancellationToken,
                    new HttpRequestException("HTTP 400", null, HttpStatusCode.BadRequest));
            }

            retryMessages = [.. messages];
            return CreateTextStreamAsync(cancellationToken, CompletedAssistantText);
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

        await CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session)).ConfigureAwait(false);

        Assert.AreEqual(2, callCount);
        Assert.IsNotNull(retryMessages);
        Assert.IsFalse(retryMessages.SelectMany(message => message.Contents).OfType<FunctionCallContent>().Any());
    }

    [TestMethod(DisplayName = "工具结果应通过 Tool 角色发送")]
    [Timeout(10000)]
    public async Task RunWithHistoryCompletion_WhenFunctionResultUsesAssistantRole_NormalizesRoleToTool()
    {
        var fakeChatClient = new FakeChatClient();
        ChatMessage[]? receivedMessages = null;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            receivedMessages = [.. messages];
            return CreateTextStreamAsync(cancellationToken, CompletedAssistantText);
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call-1", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("call-1", "文件内容"),
            ]),
        ]);

        await CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session)).ConfigureAwait(false);

        Assert.IsNotNull(receivedMessages);
        ChatMessage resultMessage = receivedMessages.Single(message => message.Contents.OfType<FunctionResultContent>().Any());
        Assert.AreEqual(ChatRole.Tool, resultMessage.Role);
    }

    [TestMethod(DisplayName = "工具历史修复应删除未配对调用")]
    public async Task RunWithHistoryCompletion_WhenToolCallHasNoResult_RemovesCall()
    {
        ChatMessage[] messages = await RunWithHistoryAsync(
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                new FunctionCallContent("unpaired-call", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
        ]).ConfigureAwait(false);

        Assert.IsFalse(messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>().Any());
    }

    [TestMethod(DisplayName = "工具历史修复应删除孤立结果")]
    public async Task RunWithHistoryCompletion_WhenToolResultHasNoCall_RemovesResult()
    {
        ChatMessage[] messages = await RunWithHistoryAsync(
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("结果前文本"),
                new FunctionResultContent("orphan-result", "孤立结果"),
                new TextContent("结果后文本"),
            ]),
        ]).ConfigureAwait(false);

        Assert.IsFalse(messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any());
    }

    [TestMethod(DisplayName = "工具历史修复应删除重复结果")]
    public async Task RunWithHistoryCompletion_WhenToolResultIsDuplicated_KeepsFirstResult()
    {
        ChatMessage[] messages = await RunWithHistoryAsync(
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("duplicate-call", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("duplicate-call", "第一次结果")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("duplicate-call", "第二次结果")]),
        ]).ConfigureAwait(false);

        FunctionResultContent result = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Single();
        Assert.AreEqual("第一次结果", result.Result);
    }

    [TestMethod(DisplayName = "工具历史修复应保留有效配对")]
    public async Task RunWithHistoryCompletion_WhenToolCallAndResultArePaired_KeepsPair()
    {
        ChatMessage[] messages = await RunWithHistoryAsync(
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("paired-call", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
            new ChatMessage(ChatRole.Assistant, [new FunctionResultContent("paired-call", "工具结果")]),
        ]).ConfigureAwait(false);

        Assert.AreEqual(
            "paired-call",
            messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>().Single().CallId);
    }

    [TestMethod(DisplayName = "工具历史修复应保留并行乱序返回的有效配对")]
    public async Task RunWithHistoryCompletion_WhenParallelToolResultsAreOutOfOrder_KeepsAllPairs()
    {
        ChatMessage[] messages = await RunWithHistoryAsync(
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("parallel-call-1", "ReadFileLines", new Dictionary<string, object?>()),
                new FunctionCallContent("parallel-call-2", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("parallel-call-2", "第二个结果"),
                new FunctionResultContent("parallel-call-1", "第一个结果"),
            ]),
        ]).ConfigureAwait(false);

        Assert.HasCount(2, messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>());
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
            return CreateFailingStreamAsync(cancellationToken, expectedExceptions[currentCall - 1]);
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

        HttpRequestException actualException = await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
            CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session))).ConfigureAwait(false);

        Assert.AreEqual(4, callCount);
        Assert.AreSame(expectedExceptions[^1], actualException);
    }

    [TestMethod(DisplayName = "暂时性异常之间收到正常输出后应重新计算重试次数")]
    [Timeout(10000)]
    public async Task RunWithHistoryCompletion_WhenUpdateIsReceived_ResetsRetryCount()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) =>
        {
            int currentCall = Interlocked.Increment(ref callCount);
            return currentCall switch
            {
                <= 3 => CreateFailingStreamAsync(cancellationToken, new HttpRequestException($"第 {currentCall} 次失败")),
                4 => CreateFailingTextStreamAsync(cancellationToken, PartialAssistantText, new HttpRequestException("正常输出后的失败")),
                <= 6 => CreateFailingStreamAsync(cancellationToken, new HttpRequestException($"重新计数后第 {currentCall - 4} 次失败")),
                _ => CreateTextStreamAsync(cancellationToken, CompletedAssistantText),
            };
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);

        List<AgentResponseUpdate> updates = await CollectUpdatesAsync(
            agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session)).ConfigureAwait(false);

        Assert.AreEqual(7, callCount);
        Assert.AreEqual($"{PartialAssistantText}{CompletedAssistantText}", string.Concat(updates.Select(update => update.Text)));
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

    private static async Task<ChatMessage[]> RunWithHistoryAsync(IReadOnlyList<ChatMessage> history)
    {
        var fakeChatClient = new FakeChatClient();
        ChatMessage[]? receivedMessages = null;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            receivedMessages = [.. messages];
            return CreateTextStreamAsync(cancellationToken, CompletedAssistantText);
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory([.. history]);

        await CollectUpdatesAsync(agent.RunWithHistoryCompletionAsync(CreateInputMessages(), session)).ConfigureAwait(false);

        Assert.IsNotNull(receivedMessages);
        return receivedMessages;
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

    private static ChatClientAgent CreateAgent(FakeChatClient fakeChatClient)
    {
        return fakeChatClient.AsAIAgent(new ChatClientAgentOptions
        {
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

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateFailingStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Exception exception)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        throw exception;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateFailingFunctionCallStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Exception exception)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionCall(
            "unpaired-call-1",
            "ReadFileLines",
            new Dictionary<string, object?>());
        await Task.Yield();
        throw exception;
    }
}
