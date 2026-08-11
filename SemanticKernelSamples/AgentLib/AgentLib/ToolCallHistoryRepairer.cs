using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib;

/// <summary>
/// 修复工具调用会话历史，使其满足模型聊天协议对工具调用和工具结果的约束。
/// </summary>
internal static class ToolCallHistoryRepairer
{
    /// <summary>
    /// 原地修复工具调用历史，并返回历史与待追加更新中具有结果的工具调用 ID。
    /// </summary>
    /// <remarks>
    /// 修复会删除没有结果的工具调用、没有相邻调用或重复的工具结果，并将有效工具结果拆分为
    /// <see cref="ChatRole.Tool"/> 角色消息。这样可以避免模型服务因工具调用与结果不成对，或工具结果
    /// 被错误放入助手消息而拒绝请求。修复步骤的顺序不可交换：必须先确定配对并删除无效内容，
    /// 最后再规范消息角色。
    /// </remarks>
    /// <param name="chatMessages">需要原地修复的会话历史。</param>
    /// <param name="collectedUpdates">尚未写入历史的流式更新，用于判断跨历史边界的工具调用配对。</param>
    /// <returns>历史与流式更新中具有结果的工具调用 ID。</returns>
    internal static HashSet<string> Repair(List<ChatMessage> chatMessages,
        IReadOnlyList<AgentResponseUpdate> collectedUpdates)
    {
        ArgumentNullException.ThrowIfNull(chatMessages);
        ArgumentNullException.ThrowIfNull(collectedUpdates);

        HashSet<string> completedFunctionCallIds = GetFunctionResultIds(chatMessages, collectedUpdates);
        RemoveIncompleteFunctionCalls(chatMessages, completedFunctionCallIds);
        RemoveOrphanFunctionResults(chatMessages, collectedUpdates);
        NormalizeFunctionResultRoles(chatMessages);
        return completedFunctionCallIds;
    }

    private static void NormalizeFunctionResultRoles(List<ChatMessage> chatMessages)
    {
        for (var messageIndex = 0; messageIndex < chatMessages.Count; messageIndex++)
        {
            ChatMessage message = chatMessages[messageIndex];
            if (message.Role == ChatRole.Tool || !message.Contents.OfType<FunctionResultContent>().Any())
            {
                continue;
            }

            var normalizedMessages = new List<ChatMessage>();
            var currentContents = new List<AIContent>();
            ChatRole currentRole = message.Role;
            foreach (AIContent content in message.Contents)
            {
                ChatRole contentRole = content is FunctionResultContent ? ChatRole.Tool : message.Role;
                if (currentContents.Count > 0 && contentRole != currentRole)
                {
                    normalizedMessages.Add(new ChatMessage(currentRole, currentContents));
                    currentContents = [];
                }

                currentRole = contentRole;
                currentContents.Add(content);
            }

            if (currentContents.Count > 0)
            {
                normalizedMessages.Add(new ChatMessage(currentRole, currentContents));
            }

            chatMessages.RemoveAt(messageIndex);
            chatMessages.InsertRange(messageIndex, normalizedMessages);
            messageIndex += normalizedMessages.Count - 1;
        }
    }

    private static void RemoveIncompleteFunctionCalls(List<ChatMessage> chatMessages,
        HashSet<string> completedFunctionCallIds)
    {
        for (var i = chatMessages.Count - 1; i >= 0; i--)
        {
            ChatMessage message = chatMessages[i];
            if (message.Role != ChatRole.Assistant || !message.Contents.OfType<FunctionCallContent>().Any())
            {
                continue;
            }

            var contents = new List<AIContent>(message.Contents.Count);
            foreach (AIContent content in message.Contents)
            {
                if (content is FunctionCallContent functionCall
                    && !string.IsNullOrWhiteSpace(functionCall.CallId)
                    && !completedFunctionCallIds.Contains(functionCall.CallId))
                {
                    continue;
                }

                contents.Add(content);
            }

            if (contents.Count == 0)
            {
                chatMessages.RemoveAt(i);
            }
            else if (contents.Count != message.Contents.Count)
            {
                chatMessages[i] = new ChatMessage(message.Role, contents);
            }
        }
    }

    private static void RemoveOrphanFunctionResults(List<ChatMessage> chatMessages,
        IReadOnlyList<AgentResponseUpdate> collectedUpdates)
    {
        HashSet<string> validFunctionCallIds = GetAdjacentFunctionCallIds(chatMessages, collectedUpdates);
        var retainedFunctionResultIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < chatMessages.Count; i++)
        {
            ChatMessage message = chatMessages[i];
            if (!message.Contents.Any(content => content is FunctionCallContent or FunctionResultContent))
            {
                continue;
            }

            var contents = new List<AIContent>(message.Contents.Count);
            foreach (AIContent content in message.Contents)
            {
                if (content is FunctionCallContent functionCall
                    && !string.IsNullOrWhiteSpace(functionCall.CallId)
                    && !validFunctionCallIds.Contains(functionCall.CallId))
                {
                    continue;
                }

                if (content is FunctionResultContent functionResult
                    && !string.IsNullOrWhiteSpace(functionResult.CallId)
                    && (!validFunctionCallIds.Contains(functionResult.CallId)
                        || !retainedFunctionResultIds.Add(functionResult.CallId)))
                {
                    continue;
                }

                contents.Add(content);
            }

            if (contents.Count == 0)
            {
                chatMessages.RemoveAt(i);
                i--;
            }
            else if (contents.Count != message.Contents.Count)
            {
                chatMessages[i] = new ChatMessage(message.Role, contents);
            }
        }
    }

    private static HashSet<string> GetAdjacentFunctionCallIds(IEnumerable<ChatMessage> chatMessages,
        IReadOnlyList<AgentResponseUpdate> collectedUpdates)
    {
        var validFunctionCallIds = new HashSet<string>(StringComparer.Ordinal);
        List<AIContent> allContents = [.. chatMessages.SelectMany(message => message.Contents)];
        allContents.AddRange(collectedUpdates.SelectMany(update => update.Contents));
        for (var i = 0; i < allContents.Count; i++)
        {
            if (allContents[i] is not FunctionCallContent functionCall
                || string.IsNullOrWhiteSpace(functionCall.CallId))
            {
                continue;
            }

            var callIds = new HashSet<string>(StringComparer.Ordinal);
            do
            {
                if (allContents[i] is FunctionCallContent currentCall
                    && !string.IsNullOrWhiteSpace(currentCall.CallId))
                {
                    callIds.Add(currentCall.CallId);
                }

                i++;
            }
            while (i < allContents.Count && allContents[i] is FunctionCallContent);

            var resultIds = new HashSet<string>(StringComparer.Ordinal);
            while (i < allContents.Count && allContents[i] is FunctionResultContent currentResult)
            {
                if (!string.IsNullOrWhiteSpace(currentResult.CallId)
                    && callIds.Contains(currentResult.CallId))
                {
                    resultIds.Add(currentResult.CallId);
                }

                i++;
            }

            validFunctionCallIds.UnionWith(resultIds);
            i--;
        }

        return validFunctionCallIds;
    }

    private static HashSet<string> GetFunctionResultIds(IEnumerable<ChatMessage> chatMessages,
        IReadOnlyList<AgentResponseUpdate> collectedUpdates)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (ChatMessage message in chatMessages)
        {
            AddFunctionResultIds(message.Contents, result);
        }

        foreach (AgentResponseUpdate update in collectedUpdates)
        {
            AddFunctionResultIds(update.Contents, result);
        }

        return result;
    }

    private static void AddFunctionResultIds(IEnumerable<AIContent> contents, HashSet<string> result)
    {
        foreach (FunctionResultContent functionResult in contents.OfType<FunctionResultContent>())
        {
            if (!string.IsNullOrWhiteSpace(functionResult.CallId))
            {
                result.Add(functionResult.CallId);
            }
        }
    }
}
