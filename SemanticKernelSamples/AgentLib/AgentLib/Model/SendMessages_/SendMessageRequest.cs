using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Threading;

namespace AgentLib.Model;

/// <summary>
/// 表示一次聊天发送请求。
/// </summary>
/// <param name="Contents">用户输入的多模态内容集合，可包含 <see cref="TextContent"/>、<see cref="DataContent"/> 等。</param>
/// <param name="WithHistory">是否携带当前 <see cref="AgentSession"/> 继续对话。</param>
/// <param name="CreateNewSession">是否在发送前切换到新会话。</param>
/// <param name="Tools">本次额外启用的工具集合。</param>
/// <param name="ToolMode">工具调用模式。</param>
/// <param name="SystemPrompt">可选的系统提示词，会注入到 <see cref="ChatOptions.Instructions"/>。</param>
/// <param name="CancellationToken">本次发送使用的取消令牌。</param>
/// <param name="ChatReducer">可选的对话历史压缩器。为 <see langword="null"/> 时不启用压缩。</param>
/// <param name="RequirePerServiceCallChatHistoryPersistence">是否在每次工具调用完成后触发压缩。仅在 <paramref name="ChatReducer"/> 不为 <see langword="null"/> 时有效。</param>
public readonly record struct SendMessageRequest
(
    IReadOnlyList<AIContent> Contents,
    bool WithHistory = true,
    bool CreateNewSession = false,
    IEnumerable<AITool>? Tools = null,
    ChatToolMode? ToolMode = null,
    string? SystemPrompt = null,
    CancellationToken CancellationToken = default,
    IChatReducer? ChatReducer = null,
    bool RequirePerServiceCallChatHistoryPersistence = false)
{
    /// <summary>
    /// 从纯文本创建发送请求。
    /// </summary>
    public static SendMessageRequest FromText(string text, bool withHistory = true, bool createNewSession = false,
        IEnumerable<AITool>? tools = null, ChatToolMode? toolMode = null, string? systemPrompt = null,
        CancellationToken cancellationToken = default,
        IChatReducer? chatReducer = null,
        bool requirePerServiceCallChatHistoryPersistence = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new SendMessageRequest([new TextContent(text)], withHistory, createNewSession, tools, toolMode, systemPrompt, cancellationToken, chatReducer, requirePerServiceCallChatHistoryPersistence);
    }
}