using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.ComponentModel;
using System.Runtime.CompilerServices;

#pragma warning disable MAAI001

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerManualSendCancellationTests
{
    private const string SystemMessageText = "系统消息";
    private const string UserMessageText = "用户消息";
    private const string AssistantStreamingText = "助手消息 Streaming 中";
    private const string ContinueUserMessageText = "请继续";

    [TestMethod(DisplayName = "手动发送取消时应补全输入和助手局部输出")]
    public async Task ManualSend_WhenStreamingCancelled_AppendsInputAndPartialAssistantMessages()
    {
        var fakeChatClient = new FakeChatClient();
        var cancellationTriggerText = "第二段";
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, "第一段", cancellationTriggerText, "不会输出");
        var context = CopilotChatManagerTestContext.Create(fakeChatClient);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await RunManualSendLoopAsync(
            context.ChatManager,
            "用户输入",
            cancellationTokenSource,
            update =>
            {
                if (string.Equals(update.Text, cancellationTriggerText, StringComparison.Ordinal))
                {
                    cancellationTokenSource.Cancel();
                }
            }).ConfigureAwait(false);

        Assert.IsTrue(result.WasCancelled, "测试应走内部取消分支。契合业务端 XML 解析错误触发取消的场景。");
        Assert.IsTrue(result.Session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages), "取消后应保留 AgentSession 内存历史。");
        Assert.IsNotNull(messages);
        Assert.HasCount(2, messages, "取消后应补全本轮输入和已收到的助手输出，且不应重复追加。 ");
        Assert.AreEqual(ChatRole.User, messages[0].Role);
        Assert.AreEqual("用户输入", messages[0].Text);
        Assert.AreEqual(ChatRole.Assistant, messages[1].Role);
        Assert.AreEqual("第一段第二段", messages[1].Text);
    }

    [TestMethod(DisplayName = "工具调用后取消再续跑时应补全工具结果和助手局部输出")]
    public async Task ManualSend_WhenCancelledAfterToolCallAndTwoTextUpdates_CompletesHistoryForNextLoop()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        var cancellationTriggerText = "工具后第二段";
        fakeChatClient.OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
        {
            var currentCall = Interlocked.Increment(ref callCount);
            return currentCall switch
            {
                1 => CreateToolCallStreamAsync(options, cancellationToken),
                2 => CreateTextStreamAsync(cancellationToken, "工具后第一段", cancellationTriggerText, "不会输出"),
                _ => CreateTextStreamAsync(cancellationToken, $"续跑响应，历史消息数：{messages.Count()}")
            };
        };

        var context = CopilotChatManagerTestContext.Create(fakeChatClient);
        using var firstLoopCancellationTokenSource = new CancellationTokenSource();
        AITool weatherTool = AIFunctionFactory.Create(GetWeather);

        var firstResult = await RunManualSendLoopAsync(
            context.ChatManager,
            "请调用工具获取天气",
            firstLoopCancellationTokenSource,
            update =>
            {
                if (string.Equals(update.Text, cancellationTriggerText, StringComparison.Ordinal))
                {
                    firstLoopCancellationTokenSource.Cancel();
                }
            },
            [weatherTool]).ConfigureAwait(false);

        Assert.IsTrue(firstResult.WasCancelled, "工具调用后的普通文本输出应触发内部取消分支。");
        Assert.IsTrue(firstResult.Session.TryGetInMemoryChatHistory(out List<ChatMessage>? messagesAfterCancel), "取消后应保留 AgentSession 历史。");
        Assert.IsNotNull(messagesAfterCancel);
        Assert.HasCount(3, messagesAfterCancel, "取消补全后应保留本轮用户、已持久化工具调用和助手局部输出，不应重复追加用户消息或工具调用。");
        Assert.AreEqual(ChatRole.User, messagesAfterCancel[0].Role);
        Assert.AreEqual("请调用工具获取天气", messagesAfterCancel[0].Text);
        Assert.AreEqual(ChatRole.Assistant, messagesAfterCancel[1].Role);
        Assert.IsTrue(messagesAfterCancel[1].Contents.OfType<FunctionCallContent>().Any(), "历史应保留工具调用请求。");
        Assert.AreEqual(ChatRole.Assistant, messagesAfterCancel[2].Role);
        Assert.IsTrue(messagesAfterCancel[2].Contents.OfType<FunctionResultContent>().Any(), "历史应补全工具调用结果。");
        Assert.AreEqual("工具后第一段工具后第二段", messagesAfterCancel[2].Text);

        using var secondLoopCancellationTokenSource = new CancellationTokenSource();
        var secondResult = await RunManualSendLoopAsync(
            context.ChatManager,
            "请根据上一轮中断位置继续",
            secondLoopCancellationTokenSource,
            tools: [weatherTool]).ConfigureAwait(false);

        Assert.IsFalse(secondResult.WasCancelled, "第二轮不应再被取消。");
        Assert.IsTrue(secondResult.Session.TryGetInMemoryChatHistory(out List<ChatMessage>? messagesAfterSecondLoop), "第二轮后应仍可读取历史。");
        Assert.IsNotNull(messagesAfterSecondLoop);
        Assert.HasCount(5, messagesAfterSecondLoop, "第二轮应在已补全历史后继续追加新的用户和助手消息。");
        Assert.AreEqual(ChatRole.User, messagesAfterSecondLoop[3].Role);
        Assert.AreEqual("请根据上一轮中断位置继续", messagesAfterSecondLoop[3].Text);
        Assert.AreEqual(ChatRole.Assistant, messagesAfterSecondLoop[4].Role);
        Assert.AreEqual("续跑响应，历史消息数：4", messagesAfterSecondLoop[4].Text);
    }

    [TestMethod(DisplayName = "系统用户助手流式工具返回后再次流式取消时续跑应保留完整历史且不重复")]
    public async Task RunWithHistoryCompletion_WhenCancelledAfterToolResultAndAssistantStreaming_CompletesHistoryForNextLoop()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        var cancellationTriggerText = "工具后第二段";
        fakeChatClient.OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
        {
            var currentCall = Interlocked.Increment(ref callCount);
            return currentCall switch
            {
                1 => CreateToolCallAfterTextStreamAsync(options, cancellationToken),
                2 => CreateTextStreamAsync(cancellationToken, "工具后第一段", cancellationTriggerText, "不会输出"),
                _ => CreateTextStreamAsync(cancellationToken, $"续跑响应，历史消息数：{messages.Count()}")
            };
        };

        AITool weatherTool = AIFunctionFactory.Create(GetWeather);
        ChatClientAgent agent = CreateAgent(fakeChatClient, [weatherTool]);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        using var cancellationTokenSource = new CancellationTokenSource();

        DirectRunResult firstResult = await RunDirectStreamingAsync(
            agent,
            session,
            CreateSystemAndUserInputMessages(),
            cancellationTokenSource,
            update =>
            {
                if (string.Equals(update.Text, cancellationTriggerText, StringComparison.Ordinal))
                {
                    cancellationTokenSource.Cancel();
                }
            }).ConfigureAwait(false);

        Assert.IsTrue(firstResult.WasCancelled);
        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        AssertSystemUserAndAssistantStreamingPrefix(messages);
        Assert.AreEqual(1, CountMessages(messages, ChatRole.System, SystemMessageText));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, UserMessageText));
        Assert.AreEqual(1, CountAssistantTexts(messages, AssistantStreamingText));
        Assert.AreEqual(1, CountFunctionCalls(messages));
        Assert.AreEqual(1, CountFunctionResults(messages));
        Assert.AreEqual(1, CountAssistantTextsContaining(messages, "工具后第一段工具后第二段"));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, ContinueUserMessageText));
    }

    [TestMethod(DisplayName = "系统用户助手流式工具返回后取消时续跑应保留工具结果且不重复")]
    public async Task RunWithHistoryCompletion_WhenCancelledAfterToolResult_CompletesToolResultForNextLoop()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            var currentCall = Interlocked.Increment(ref callCount);
            return currentCall switch
            {
                1 => CreateToolCallAndResultAfterTextStreamAsync(cancellationToken),
                _ => CreateTextStreamAsync(cancellationToken, $"续跑响应，历史消息数：{messages.Count()}")
            };
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        using var cancellationTokenSource = new CancellationTokenSource();

        DirectRunResult firstResult = await RunDirectStreamingAsync(
            agent,
            session,
            CreateSystemAndUserInputMessages(),
            cancellationTokenSource,
            update =>
            {
                if (update.Contents.OfType<FunctionResultContent>().Any())
                {
                    cancellationTokenSource.Cancel();
                }
            }).ConfigureAwait(false);

        Assert.IsTrue(firstResult.WasCancelled);
        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        AssertSystemUserAndAssistantStreamingPrefix(messages);
        Assert.AreEqual(1, CountMessages(messages, ChatRole.System, SystemMessageText));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, UserMessageText));
        Assert.AreEqual(1, CountAssistantTexts(messages, AssistantStreamingText));
        Assert.AreEqual(1, CountFunctionResults(messages));
        Assert.AreEqual(0, CountAssistantTexts(messages, "工具后第一段"));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, ContinueUserMessageText));
    }

    [TestMethod(DisplayName = "系统用户助手流式工具调用后取消时续跑应去掉无结果工具调用")]
    public async Task RunWithHistoryCompletion_WhenCancelledAfterToolCallWithoutResult_RemovesIncompleteToolCallForNextLoop()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            var currentCall = Interlocked.Increment(ref callCount);
            return currentCall switch
            {
                1 => CreateToolCallOnlyAfterTextStreamAsync(cancellationToken),
                _ => CreateTextStreamAsync(cancellationToken, $"续跑响应，历史消息数：{messages.Count()}")
            };
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        using var cancellationTokenSource = new CancellationTokenSource();

        DirectRunResult firstResult = await RunDirectStreamingAsync(
            agent,
            session,
            CreateSystemAndUserInputMessages(),
            cancellationTokenSource,
            update =>
            {
                if (update.Contents.OfType<FunctionCallContent>().Any())
                {
                    cancellationTokenSource.Cancel();
                }
            }).ConfigureAwait(false);

        Assert.IsTrue(firstResult.WasCancelled);
        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        AssertSystemUserAndAssistantStreamingPrefix(messages);
        Assert.AreEqual(1, CountMessages(messages, ChatRole.System, SystemMessageText));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, UserMessageText));
        Assert.AreEqual(1, CountAssistantTexts(messages, AssistantStreamingText));
        Assert.AreEqual(0, CountFunctionCalls(messages));
        Assert.AreEqual(0, CountFunctionResults(messages));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, ContinueUserMessageText));
    }

    [TestMethod(DisplayName = "历史中存在未配对工具调用时续跑应先移除该工具调用")]
    public async Task RunWithHistoryCompletion_WhenHistoryContainsUnpairedToolCall_RemovesToolCallBeforeNextLoop()
    {
        var fakeChatClient = new FakeChatClient();
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, AssistantStreamingText);
        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "上一轮用户消息"),
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("上一轮助手消息"),
                new FunctionCallContent("unpaired-call-1", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
        ]);

        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, CountFunctionCalls(messages, "unpaired-call-1"));
        Assert.AreEqual(1, CountAssistantTexts(messages, "上一轮助手消息"));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, ContinueUserMessageText));
        Assert.AreEqual(1, CountAssistantTexts(messages, AssistantStreamingText));
    }

    [TestMethod(DisplayName = "历史中存在重复工具结果时续跑应只保留第一条工具结果")]
    public async Task RunWithHistoryCompletion_WhenHistoryContainsDuplicateToolResults_RemovesLaterToolResult()
    {
        var fakeChatClient = new FakeChatClient();
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, AssistantStreamingText);
        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "上一轮用户消息"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("duplicate-call-1", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("duplicate-call-1", "第一次结果"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("duplicate-call-1", "第二次结果"),
            ]),
        ]);

        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        Assert.AreEqual(1, CountFunctionResults(messages, "duplicate-call-1"));
        Assert.IsTrue(messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
            .Any(content => string.Equals(content.Result?.ToString(), "第一次结果", StringComparison.Ordinal)));
        Assert.IsFalse(messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
            .Any(content => string.Equals(content.Result?.ToString(), "第二次结果", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "历史中工具调用后接普通内容时续跑应移除非相邻工具调用")]
    public async Task RunWithHistoryCompletion_WhenFunctionCallIsNotFollowedByFunctionResult_RemovesFunctionCall()
    {
        var fakeChatClient = new FakeChatClient();
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, AssistantStreamingText);
        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "上一轮用户消息"),
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                new FunctionCallContent("not-adjacent-call-1", "ReadFileLines", new Dictionary<string, object?>()),
                new TextContent("普通助手消息"),
                new FunctionResultContent("not-adjacent-call-1", "非相邻工具结果"),
            ]),
            new ChatMessage(ChatRole.Assistant, "后续助手消息"),
        ]);

        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, CountFunctionCalls(messages, "not-adjacent-call-1"));
        Assert.AreEqual(0, CountFunctionResults(messages, "not-adjacent-call-1"));
        Assert.AreEqual(1, CountAssistantTexts(messages, "调用前文本普通助手消息"));
    }

    [TestMethod(DisplayName = "历史中工具调用后接错误工具结果时续跑应同时移除调用和结果")]
    public async Task RunWithHistoryCompletion_WhenFunctionCallIsFollowedByDifferentFunctionResult_RemovesFunctionCallAndResult()
    {
        var fakeChatClient = new FakeChatClient();
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, AssistantStreamingText);
        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "上一轮用户消息"),
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                new FunctionCallContent("expected-call-1", "ReadFileLines", new Dictionary<string, object?>()),
                new FunctionResultContent("actual-call-1", "错误结果"),
            ]),
            new ChatMessage(ChatRole.Assistant, "后续助手消息"),
        ]);

        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, CountFunctionCalls(messages, "expected-call-1"));
        Assert.AreEqual(0, CountFunctionResults(messages, "actual-call-1"));
        Assert.AreEqual(1, CountAssistantTexts(messages, "调用前文本"));
    }

    [TestMethod(DisplayName = "历史中工具结果前没有工具调用时续跑应移除孤立结果")]
    public async Task RunWithHistoryCompletion_WhenFunctionResultHasNoPreviousFunctionCall_RemovesFunctionResult()
    {
        var fakeChatClient = new FakeChatClient();
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, AssistantStreamingText);
        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "上一轮用户消息"),
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("结果前文本"),
                new FunctionResultContent("orphan-result-1", "孤立结果"),
                new TextContent("结果后文本"),
            ]),
            new ChatMessage(ChatRole.Assistant, "后续助手消息"),
        ]);

        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, CountFunctionResults(messages, "orphan-result-1"));
        Assert.AreEqual(1, CountAssistantTexts(messages, "结果前文本结果后文本"));
    }

    [TestMethod(DisplayName = "历史中相邻工具调用和结果分散在两条消息时续跑应保留配对内容")]
    public async Task RunWithHistoryCompletion_WhenFunctionCallAndResultAreAdjacentAcrossMessages_KeepsPair()
    {
        var fakeChatClient = new FakeChatClient();
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, AssistantStreamingText);
        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "上一轮用户消息"),
            new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("调用前文本"),
                new FunctionCallContent("adjacent-call-1", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("adjacent-call-1", "工具结果"),
                new TextContent("结果后文本"),
            ]),
        ]);

        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        Assert.AreEqual(1, CountFunctionCalls(messages, "adjacent-call-1"));
        Assert.AreEqual(1, CountFunctionResults(messages, "adjacent-call-1"));
    }

    [TestMethod(DisplayName = "历史中并行工具调用乱序相邻返回时续跑应保留全部配对内容")]
    public async Task RunWithHistoryCompletion_WhenParallelFunctionCallsReturnOutOfOrderAdjacently_KeepsAllPairs()
    {
        var fakeChatClient = new FakeChatClient();
        fakeChatClient.OnGetStreamingResponseAsync = (_, _, cancellationToken) => CreateTextStreamAsync(cancellationToken, AssistantStreamingText);
        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "上一轮用户消息"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("parallel-call-1", "ReadFileLines", new Dictionary<string, object?>()),
                new FunctionCallContent("parallel-call-2", "ReadFileLines", new Dictionary<string, object?>()),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("parallel-call-2", "第二个结果"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionResultContent("parallel-call-1", "第一个结果"),
                new TextContent("结果后文本"),
            ]),
        ]);

        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        Assert.AreEqual(1, CountFunctionCalls(messages, "parallel-call-1"));
        Assert.AreEqual(1, CountFunctionCalls(messages, "parallel-call-2"));
        Assert.AreEqual(1, CountFunctionResults(messages, "parallel-call-1"));
        Assert.AreEqual(1, CountFunctionResults(messages, "parallel-call-2"));
    }

    [TestMethod(DisplayName = "系统用户助手流式中取消时续跑应保留局部助手消息")]
    public async Task RunWithHistoryCompletion_WhenCancelledDuringAssistantStreaming_CompletesPartialAssistantMessageForNextLoop()
    {
        var fakeChatClient = new FakeChatClient();
        var callCount = 0;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, _, cancellationToken) =>
        {
            var currentCall = Interlocked.Increment(ref callCount);
            return currentCall switch
            {
                1 => CreateTextStreamAsync(cancellationToken, AssistantStreamingText, "不会输出"),
                _ => CreateTextStreamAsync(cancellationToken, $"续跑响应，历史消息数：{messages.Count()}")
            };
        };

        ChatClientAgent agent = CreateAgent(fakeChatClient);
        AgentSession session = await agent.CreateSessionAsync().ConfigureAwait(false);
        using var cancellationTokenSource = new CancellationTokenSource();

        DirectRunResult firstResult = await RunDirectStreamingAsync(
            agent,
            session,
            CreateSystemAndUserInputMessages(),
            cancellationTokenSource,
            update =>
            {
                if (string.Equals(update.Text, AssistantStreamingText, StringComparison.Ordinal))
                {
                    cancellationTokenSource.Cancel();
                }
            }).ConfigureAwait(false);

        Assert.IsTrue(firstResult.WasCancelled);
        await RunSecondLoopAsync(agent, session).ConfigureAwait(false);

        Assert.IsTrue(session.TryGetInMemoryChatHistory(out List<ChatMessage>? messages));
        Assert.IsNotNull(messages);
        AssertSystemUserAndAssistantStreamingPrefix(messages);
        Assert.AreEqual(1, CountMessages(messages, ChatRole.System, SystemMessageText));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, UserMessageText));
        Assert.AreEqual(1, CountAssistantTexts(messages, AssistantStreamingText));
        Assert.AreEqual(1, CountMessages(messages, ChatRole.User, ContinueUserMessageText));
    }

    private static async Task<ManualSendLoopResult> RunManualSendLoopAsync(
        CopilotChatManager chatManager,
        string userMessage,
        CancellationTokenSource loopCancellationTokenSource,
        Action<AgentResponseUpdate>? onUpdate = null,
        IReadOnlyList<AITool>? tools = null)
    {
        IManualSendMessageContext manualContext = await chatManager.CreateManualSendMessageContextAsync().ConfigureAwait(false);
        manualContext.UserChatMessage.AppendText(userMessage);
        await manualContext.AppendMessagesToSessionAsync().ConfigureAwait(false);

        using var _ = manualContext.StartChatting();

        var currentAssistantResponseUpdateList = new List<AgentResponseUpdate>();
        var chatOptions = new ChatOptions();
        if (tools is not null)
        {
            chatOptions.Tools = [.. tools];
        }

        AgentSession session = await manualContext.GetAgentSessionAsync().ConfigureAwait(false);
        ChatClientAgent agent = await manualContext.GetChatClientAgentAsync(
            options => options.ChatOptions = chatOptions).ConfigureAwait(false);
        ChatMessage userChatMessage = manualContext.UserChatMessage.ToChatMessage();
        ChatMessage[] inputMessages = [userChatMessage];

        var wasCancelled = false;
        try
        {
            await foreach (AgentResponseUpdate update in agent.RunWithHistoryCompletionAsync(
                inputMessages, session, loopCancellationTokenSource.Token).ConfigureAwait(false))
            {
                manualContext.AppendResponseUpdate(update);
                currentAssistantResponseUpdateList.Add(update);
                onUpdate?.Invoke(update);
            }
        }
        catch (OperationCanceledException) when (loopCancellationTokenSource.IsCancellationRequested)
        {
            wasCancelled = true;
        }

        return new ManualSendLoopResult(session, currentAssistantResponseUpdateList, wasCancelled);
    }

    private static async Task<DirectRunResult> RunDirectStreamingAsync(
        ChatClientAgent agent,
        AgentSession session,
        IReadOnlyList<ChatMessage> inputMessages,
        CancellationTokenSource loopCancellationTokenSource,
        Action<AgentResponseUpdate>? onUpdate = null)
    {
        var updates = new List<AgentResponseUpdate>();
        var wasCancelled = false;
        try
        {
            await foreach (AgentResponseUpdate update in agent.RunWithHistoryCompletionAsync(
                inputMessages, session, loopCancellationTokenSource.Token).ConfigureAwait(false))
            {
                updates.Add(update);
                onUpdate?.Invoke(update);
                if (loopCancellationTokenSource.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (loopCancellationTokenSource.IsCancellationRequested)
        {
            wasCancelled = true;
        }

        return new DirectRunResult(updates, wasCancelled);
    }

    private static async Task<AgentSession> CreateSessionAsync(FakeChatClient fakeChatClient, IReadOnlyList<AITool>? tools = null)
    {
        var chatOptions = new ChatOptions();
        if (tools is not null)
        {
            chatOptions.Tools = [.. tools];
        }

        ChatClientAgent agent = fakeChatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = chatOptions,
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()),
            RequirePerServiceCallChatHistoryPersistence = true,
        });

        return await agent.CreateSessionAsync().ConfigureAwait(false);
    }

    private static ChatClientAgent CreateAgent(FakeChatClient fakeChatClient, IReadOnlyList<AITool>? tools = null)
    {
        var chatOptions = new ChatOptions();
        if (tools is not null)
        {
            chatOptions.Tools = [.. tools];
        }

        return fakeChatClient.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = chatOptions,
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()),
            RequirePerServiceCallChatHistoryPersistence = true,
        });
    }

    private static ChatMessage[] CreateSystemAndUserInputMessages()
    {
        return
        [
            new ChatMessage(ChatRole.System, SystemMessageText),
            new ChatMessage(ChatRole.User, UserMessageText),
        ];
    }

    private static void AssertSystemUserAndAssistantStreamingPrefix(IReadOnlyList<ChatMessage> messages)
    {
        Assert.IsGreaterThanOrEqualTo(3, messages.Count);
        Assert.AreEqual(ChatRole.System, messages[0].Role);
        Assert.AreEqual(SystemMessageText, messages[0].Text);
        Assert.AreEqual(ChatRole.User, messages[1].Role);
        Assert.AreEqual(UserMessageText, messages[1].Text);
        Assert.AreEqual(ChatRole.Assistant, messages[2].Role);
        Assert.AreEqual(AssistantStreamingText, messages[2].Text);
    }

    private static int CountMessages(IReadOnlyList<ChatMessage> messages, ChatRole role, string text)
    {
        return messages.Count(message => message.Role == role && string.Equals(message.Text, text, StringComparison.Ordinal));
    }

    private static int CountAssistantTexts(IReadOnlyList<ChatMessage> messages, string text)
    {
        return messages.Count(message => message.Role == ChatRole.Assistant && string.Equals(message.Text, text, StringComparison.Ordinal));
    }

    private static int CountAssistantTextsContaining(IReadOnlyList<ChatMessage> messages, string text)
    {
        return messages.Count(message => message.Role == ChatRole.Assistant && message.Text.Contains(text, StringComparison.Ordinal));
    }

    private static int CountFunctionCalls(IReadOnlyList<ChatMessage> messages)
    {
        return messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>().Count();
    }

    private static int CountFunctionResults(IReadOnlyList<ChatMessage> messages)
    {
        return messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Count();
    }

    private static int CountFunctionCalls(IReadOnlyList<ChatMessage> messages, string callId)
    {
        return messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>()
            .Count(content => string.Equals(content.CallId, callId, StringComparison.Ordinal));
    }

    private static int CountFunctionResults(IReadOnlyList<ChatMessage> messages, string callId)
    {
        return messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
            .Count(content => string.Equals(content.CallId, callId, StringComparison.Ordinal));
    }

    private static async Task RunSecondLoopAsync(ChatClientAgent agent, AgentSession session)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await RunDirectStreamingAsync(
            agent,
            session,
            [new ChatMessage(ChatRole.User, ContinueUserMessageText)],
            cancellationTokenSource).ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateTextStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params string[] texts)
    {
        foreach (var text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return CopilotChatManagerTestContext.AssistantText(text);
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolCallAfterTextStreamAsync(
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantText(AssistantStreamingText);
        await Task.Yield();

        await foreach (ChatResponseUpdate update in CreateToolCallStreamAsync(options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolCallOnlyAfterTextStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantText(AssistantStreamingText);
        await Task.Yield();

        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionCall("weather-call-1", nameof(GetWeather));
        await Task.Yield();
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolCallAndResultAfterTextStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (ChatResponseUpdate update in CreateToolCallOnlyAfterTextStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }

        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionResult("weather-call-1", GetWeather());
        await Task.Yield();
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolCallResultAndTextAfterTextStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params string[] texts)
    {
        await foreach (ChatResponseUpdate update in CreateToolCallAndResultAfterTextStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }

        foreach (string text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return CopilotChatManagerTestContext.AssistantText(text);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CancelBeforeTextStreamAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantText("不会输出");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolCallStreamAsync(
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tool = options?.Tools?.OfType<AIFunction>().FirstOrDefault()
                   ?? throw new InvalidOperationException("未找到测试工具。");

        yield return new ChatResponseUpdate(ChatRole.Assistant,
            [new FunctionCallContent("weather-call-1", tool.Name, new Dictionary<string, object?>())]);
        await Task.Yield();
    }

    [System.ComponentModel.Description("获取天气")]
    private static string GetWeather()
    {
        return "天气晴朗";
    }

    private sealed record ManualSendLoopResult(
        AgentSession Session,
        IReadOnlyList<AgentResponseUpdate> Updates,
        bool WasCancelled);

    private sealed record DirectRunResult(
        IReadOnlyList<AgentResponseUpdate> Updates,
        bool WasCancelled);
}
