using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AgentLib;

/// <summary>
/// 运行流式 Agent 调用的辅助方法。
/// 在迭代器退出时（正常完成 / 取消 / break），自动补全 <see cref="AgentSession"/> 中的输入消息和助手响应历史。
/// </summary>
public static class AgentSessionStreamingHelper
{
    private const int MaxRetryCount = 3;

    /// <summary>
    /// 运行流式 Agent 调用。
    /// 每个 <see cref="AgentResponseUpdate"/> 逐一 yield 给调用方。
    /// 调用方可在 <c>await foreach</c> 循环中自由 <c>break</c> 或触发取消；
    /// 无论以何种方式退出循环，<paramref name="session"/> 的历史都会在迭代器内部自动补全。
    /// </summary>
    /// <param name="agent">已配置好的 <see cref="ChatClientAgent"/>。</param>
    /// <param name="inputMessages">本轮要发送的输入消息。</param>
    /// <param name="session">代理会话，其历史将在退出时自动补全。</param>
    /// <param name="cancellationToken">取消令牌。取消时，已收集的助手更新会被补全进会话历史。</param>
    /// <returns>流式响应更新序列。</returns>
    public static async IAsyncEnumerable<AgentResponseUpdate> RunWithHistoryCompletionAsync(
        this ChatClientAgent agent,
        IReadOnlyList<ChatMessage> inputMessages,
        AgentSession session,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(inputMessages);
        ArgumentNullException.ThrowIfNull(session);

        SanitizeSessionHistory(session);

        var collectedUpdates = new List<AgentResponseUpdate>();
        try
        {
            IReadOnlyList<ChatMessage> currentInputMessages = inputMessages;
            var retryCount = 0;
            while (true)
            {
                IAsyncEnumerator<AgentResponseUpdate> enumerator = agent.RunStreamingAsync(
                    currentInputMessages, session, cancellationToken: cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
                Exception? streamingException = null;
                try
                {
                    while (true)
                    {
                        MoveNextResult moveNextResult = await TryMoveNextAsync(enumerator).ConfigureAwait(false);
                        if (moveNextResult.Exception is not null)
                        {
                            streamingException = moveNextResult.Exception;
                            break;
                        }

                        if (!moveNextResult.HasNext)
                        {
                            break;
                        }

                        // 有过正常的情况，重置重试次数
                        retryCount = 0;

                        AgentResponseUpdate update = enumerator.Current;
                        collectedUpdates.Add(update);
                        yield return update;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }

                if (streamingException is null)
                {
                    // 无异常的情况， 直接退出循环
                    break;
                }

                if (IsRetryableServerError(streamingException, out var delayTime))
                {
                    // 明确的服务器错误，做等待，然后重试
                    await Task.Delay(delayTime, cancellationToken);
                    // 不计入重试
                    retryCount--;
                }
                else if (!IsRetryableException(streamingException) || retryCount >= MaxRetryCount)
                {
                    ExceptionDispatchInfo.Capture(streamingException).Throw();
                    yield break; // 理论上不会进入此分支，只是为了做明确的打断
                }

                cancellationToken.ThrowIfCancellationRequested();
                CompleteRunHistory(session, inputMessages, collectedUpdates);
                collectedUpdates.Clear();
                currentInputMessages = [];
                retryCount++;
            }
        }
        finally
        {
            CompleteRunHistory(session, inputMessages, collectedUpdates);
        }

        static async Task<MoveNextResult> TryMoveNextAsync(IAsyncEnumerator<AgentResponseUpdate> enumerator)
        {
            // 由于 C# 不允许在包含 yield return 的 try 中直接 catch，只用一个内部流式转发方法封装异常
            try
            {
                bool hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                return new MoveNextResult(hasNext, null);
            }
            catch (Exception exception)
            {
                return new MoveNextResult(false, exception);
            }
        }

        static bool IsRetryableException(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                var innerExceptions = aggregateException.InnerExceptions;
                if (innerExceptions.Count>0)
                {
                    if (innerExceptions[0] is TaskCanceledException)
                    {
                        return true;
                    }
                }
            }

            // 400 可能由不完整的工具调用历史引起，清理历史后允许进入有限重试。
            return exception is HttpRequestException { StatusCode: HttpStatusCode.BadRequest }
                || exception is System.ClientModel.ClientResultException { Status: (int) HttpStatusCode.BadRequest }
                || exception is HttpRequestException
                or IOException
                or TimeoutException;
        }

        static bool IsRetryableServerError(Exception exception, out TimeSpan delayTime)
        {
            delayTime = TimeSpan.FromSeconds(0.5);

            // 服务器级错误，不累计错误，但是要做等待的重试
            if (exception is System.ClientModel.ClientResultException clientResultException)
            {
                if (clientResultException.Message.Contains("HTTP 500 (server_error: internal_server_error)", StringComparison.Ordinal))
                {
                    return true;
                }

                if (clientResultException.Message.Contains("HTTP 503 (server_error: internal_server_error)", StringComparison.Ordinal))
                {
                    // 未登录异常，等待时间稍微久一点
                    delayTime = TimeSpan.FromSeconds(3);

                    return true;
                }
            }

            return false;
        }
    }

    private readonly record struct MoveNextResult(bool HasNext, Exception? Exception);

    private static void CompleteRunHistory(
        AgentSession session,
        IReadOnlyList<ChatMessage> inputMessages,
        IReadOnlyList<AgentResponseUpdate> collectedUpdates)
    {
        if (!session.TryGetInMemoryChatHistory(out List<ChatMessage>? chatMessageList))
        {
            return;
        }

        HashSet<string> completedFunctionCallIds = ToolCallHistoryRepairer.Repair(chatMessageList, collectedUpdates);

        if (!ContainsMessageSequence(chatMessageList, inputMessages))
        {
            chatMessageList.AddRange(inputMessages);
        }

        List<ChatMessage> updateChatMessageList = CollectAssistantContents(chatMessageList, collectedUpdates, completedFunctionCallIds);
        if (updateChatMessageList.Count == 0 || EndsWithAssistantContents(chatMessageList, updateChatMessageList))
        {
            session.SetInMemoryChatHistory(chatMessageList);
            return;
        }

        chatMessageList.AddRange(updateChatMessageList);
        session.SetInMemoryChatHistory(chatMessageList);
    }

    private static void SanitizeSessionHistory(AgentSession session)
    {
        if (!session.TryGetInMemoryChatHistory(out List<ChatMessage>? chatMessageList))
        {
            return;
        }

        ToolCallHistoryRepairer.Repair(chatMessageList, []);
        session.SetInMemoryChatHistory(chatMessageList);
    }

    private static List<ChatMessage> CollectAssistantContents(
        IReadOnlyList<ChatMessage> chatMessageList,
        IReadOnlyList<AgentResponseUpdate> collectedUpdates,
        HashSet<string> completedFunctionCallIds)
    {
        HashSet<string> existingFunctionCallIds = GetExistingFunctionCallIds(chatMessageList);
        HashSet<string> existingFunctionResultIds = GetExistingFunctionResultIds(chatMessageList);

        var updateChatMessageList = new List<ChatMessage>();

        var assistantContents = new List<AIContent>(collectedUpdates.Sum(update => update.Contents.Count));
        foreach (AgentResponseUpdate agentResponseUpdate in collectedUpdates)
        {
            foreach (AIContent content in agentResponseUpdate.Contents)
            {
                if (content is FunctionCallContent functionCallContent
                    && !string.IsNullOrWhiteSpace(functionCallContent.CallId)
                    && (existingFunctionCallIds.Contains(functionCallContent.CallId)
                        || !completedFunctionCallIds.Contains(functionCallContent.CallId)))
                {
                    continue;
                }

                if (content is FunctionResultContent functionResultContent
                    && !string.IsNullOrWhiteSpace(functionResultContent.CallId)
                    && existingFunctionResultIds.Contains(functionResultContent.CallId))
                {
                    continue;
                }

                if (content is FunctionResultContent)
                {
                    if (assistantContents.Count > 0)
                    {
                        updateChatMessageList.Add(new ChatMessage(ChatRole.Assistant, assistantContents));
                        assistantContents = new List<AIContent>();
                    }

                    updateChatMessageList.Add(new ChatMessage(ChatRole.Tool, [content]));
                }
                else
                {
                    assistantContents.Add(content);
                }
            }
        }

        if (assistantContents.Count > 0)
        {
            updateChatMessageList.Add(new ChatMessage(ChatRole.Assistant, assistantContents));
        }

        return updateChatMessageList;
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

    private static HashSet<string> GetExistingFunctionResultIds(IEnumerable<ChatMessage> chatMessageList)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (FunctionResultContent functionResult in chatMessageList
                     .SelectMany(message => message.Contents)
                     .OfType<FunctionResultContent>())
        {
            if (!string.IsNullOrWhiteSpace(functionResult.CallId))
            {
                result.Add(functionResult.CallId);
            }
        }

        return result;
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

    private static bool EndsWithAssistantContents(IReadOnlyList<ChatMessage> messageList, IReadOnlyList<ChatMessage> updateChatMessageList)
    {
        if (messageList.Count == 0 || messageList[^1].Role != ChatRole.Assistant)
        {
            return false;
        }

        if (updateChatMessageList.Count != 1)
        {
            return false;
        }

        var firstUpdateChatMessage = updateChatMessageList[0];
        if (firstUpdateChatMessage.Role != ChatRole.Assistant)
        {
            // 如果追加的是工具，那就不需要检查是否以助手内容结尾
            return false;
        }

        var assistantContents = firstUpdateChatMessage.Contents;

        var lastText = messageList[^1].Text;

        //if (string.IsNullOrEmpty(lastText))
        //{
        //    return false;
        //}

        return string.Equals(lastText, GetText(assistantContents), StringComparison.Ordinal);
    }

    private static string GetText(IEnumerable<AIContent> contents)
    {
        return string.Concat(contents.OfType<TextContent>().Select(content => content.Text));
    }
}
