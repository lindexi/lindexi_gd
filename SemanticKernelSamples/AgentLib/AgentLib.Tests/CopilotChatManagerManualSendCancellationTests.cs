using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AgentLib.Tests;

[TestClass]
public class CopilotChatManagerManualSendCancellationTests
{
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
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(
                inputMessages, session, cancellationToken: loopCancellationTokenSource.Token).ConfigureAwait(false))
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

        if (loopCancellationTokenSource.IsCancellationRequested && session.TryGetInMemoryChatHistory(out List<ChatMessage>? chatMessageList))
        {
            AppendCancelledManualSendMessages(chatMessageList, inputMessages, currentAssistantResponseUpdateList);
            session.SetInMemoryChatHistory(chatMessageList);
        }

        return new ManualSendLoopResult(session, currentAssistantResponseUpdateList, wasCancelled);
    }

    private static void AppendCancelledManualSendMessages(
        List<ChatMessage> chatMessageList,
        IReadOnlyList<ChatMessage> inputMessages,
        IReadOnlyList<AgentResponseUpdate> currentAssistantResponseUpdateList)
    {
        bool shouldAppendInputMessages = !ContainsMessageSequence(chatMessageList, inputMessages);
        if (shouldAppendInputMessages)
        {
            chatMessageList.AddRange(inputMessages);
        }

        var assistantContents = new List<AIContent>();
        HashSet<string> existingFunctionCallIds = GetExistingFunctionCallIds(chatMessageList);
        foreach (AgentResponseUpdate agentResponseUpdate in currentAssistantResponseUpdateList)
        {
            foreach (AIContent content in agentResponseUpdate.Contents)
            {
                if (content is FunctionCallContent functionCallContent
                    && existingFunctionCallIds.Contains(functionCallContent.CallId))
                {
                    continue;
                }

                assistantContents.Add(content);
            }
        }

        if (assistantContents.Count == 0 || EndsWithAssistantContents(chatMessageList, assistantContents))
        {
            return;
        }

        chatMessageList.Add(new ChatMessage(ChatRole.Assistant, assistantContents));
    }

    private static HashSet<string> GetExistingFunctionCallIds(IEnumerable<ChatMessage> chatMessageList)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (ChatMessage chatMessage in chatMessageList)
        {
            foreach (FunctionCallContent functionCallContent in chatMessage.Contents.OfType<FunctionCallContent>())
            {
                if (!string.IsNullOrWhiteSpace(functionCallContent.CallId))
                {
                    result.Add(functionCallContent.CallId);
                }
            }
        }

        return result;
    }

    private static bool EndsWithMessages(IReadOnlyList<ChatMessage> messageList, IReadOnlyList<ChatMessage> expectedTail)
    {
        if (messageList.Count < expectedTail.Count)
        {
            return false;
        }

        for (var i = 0; i < expectedTail.Count; i++)
        {
            ChatMessage actual = messageList[messageList.Count - expectedTail.Count + i];
            ChatMessage expected = expectedTail[i];
            if (actual.Role != expected.Role || !string.Equals(actual.Text, expected.Text, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsMessageSequence(IReadOnlyList<ChatMessage> messageList, IReadOnlyList<ChatMessage> expectedSequence)
    {
        if (expectedSequence.Count == 0)
        {
            return true;
        }

        if (messageList.Count < expectedSequence.Count)
        {
            return false;
        }

        for (var startIndex = 0; startIndex <= messageList.Count - expectedSequence.Count; startIndex++)
        {
            var matched = true;
            for (var i = 0; i < expectedSequence.Count; i++)
            {
                ChatMessage actual = messageList[startIndex + i];
                ChatMessage expected = expectedSequence[i];
                if (actual.Role != expected.Role || !string.Equals(actual.Text, expected.Text, StringComparison.Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return true;
            }
        }

        return false;
    }

    private static bool EndsWithAssistantContents(IReadOnlyList<ChatMessage> messageList, IReadOnlyList<AIContent> assistantContents)
    {
        if (messageList.Count == 0 || messageList[^1].Role != ChatRole.Assistant)
        {
            return false;
        }

        return string.Equals(messageList[^1].Text, GetText(assistantContents), StringComparison.Ordinal);
    }

    private static string GetText(IEnumerable<AIContent> contents)
    {
        return string.Concat(contents.OfType<TextContent>().Select(content => content.Text));
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
}
