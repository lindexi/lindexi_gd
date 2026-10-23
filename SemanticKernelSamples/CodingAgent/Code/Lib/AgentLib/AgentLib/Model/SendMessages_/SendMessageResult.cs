using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.Model;

/// <summary>
/// 表示一次聊天发送过程的即时结果与后续异步阶段。
/// </summary>
/// <param name="UserChatMessage">已创建的用户消息对象。</param>
/// <param name="AssistantChatMessage">已创建的助手消息对象。</param>
/// <param name="ToolList">本次实际启用的工具列表。</param>
/// <param name="CreateChatClientAgentTask">用于创建聊天客户端、代理与运行会话的任务。</param>
/// <param name="RunTask">用于等待整个发送流程结束的任务。</param>
public record SendMessageResult
(
    CopilotChatMessage UserChatMessage,
    CopilotChatMessage AssistantChatMessage,
    IReadOnlyList<AITool> ToolList,
    Task<ChatClientAgentCreatedResult> CreateChatClientAgentTask,
    Task<SendMessageRunState> RunTask)
{
}