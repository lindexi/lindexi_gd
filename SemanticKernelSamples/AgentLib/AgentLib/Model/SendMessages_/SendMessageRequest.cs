using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Threading;

namespace AgentLib.Model;

/// <summary>
/// 表示一次聊天发送请求。
/// </summary>
public readonly record struct SendMessageRequest
{
    /// <summary>
    /// 从纯文本创建发送请求。
    /// </summary>
    /// <param name="text">用户输入的纯文本内容。</param>
    public SendMessageRequest(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Contents = [new TextContent(text)];
    }

    /// <summary>
    /// 使用多模态内容创建发送请求。
    /// </summary>
    /// <param name="contents">用户输入的多模态内容集合，可包含 <see cref="TextContent"/>、<see cref="DataContent"/> 等。</param>
    public SendMessageRequest(IReadOnlyList<AIContent> contents)
    {
        Contents = contents;
    }

    /// <summary>
    /// 用户输入的多模态内容集合，可包含 <see cref="TextContent"/>、<see cref="DataContent"/> 等。
    /// </summary>
    public IReadOnlyList<AIContent> Contents { get; init; }

    /// <summary>
    /// 是否携带当前 <see cref="AgentSession"/> 继续对话。
    /// </summary>
    public bool WithHistory { get; init; } = true;

    /// <summary>
    /// 是否在发送前切换到新会话。
    /// </summary>
    public bool CreateNewSession { get; init; } = false;

    /// <summary>
    /// 本次额外启用的工具集合。
    /// </summary>
    public IEnumerable<AITool>? Tools { get; init; } = null;

    /// <summary>
    /// 工具调用模式。
    /// </summary>
    public ChatToolMode? ToolMode { get; init; } = null;

    /// <summary>
    /// 可选的系统提示词，会注入到 <see cref="ChatOptions.Instructions"/>。
    /// </summary>
    public string? SystemPrompt { get; init; } = null;

    /// <summary>
    /// 本次发送使用的取消令牌。
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// 可选的对话历史压缩器。为 <see langword="null"/> 时不启用压缩。
    /// </summary>
    public IChatReducer? ChatReducer { get; init; } = null;

    /// <summary>
    /// 是否在每次工具调用完成后触发压缩。仅在 <see cref="ChatReducer"/> 不为 <see langword="null"/> 时有效。
    /// </summary>
    public bool RequirePerServiceCallChatHistoryPersistence { get; init; } = false;

    /// <summary>
    /// 本次请求的 AI 上下文提供者集合。非 <see langword="null"/> 时覆盖 <see cref="CopilotChatManager.AIContextProviders"/>。
    /// 设为空集合可临时禁用上下文提供者。
    /// </summary>
    public IList<AIContextProvider>? AIContextProviders { get; init; } = null;
}