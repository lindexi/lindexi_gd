using AgentLib.Model;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 保存一次编程对话的模型会话与输出目标，不包含聊天宿主状态。
/// </summary>
public sealed class CodingRunContext
{
    /// <summary>创建编程执行上下文。</summary>
    public CodingRunContext(IChatClient chatClient, CopilotChatMessage assistantChatMessage,
        AgentSession? agentSession = null, IMainThreadDispatcher? mainThreadDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(assistantChatMessage);
        ChatClient = chatClient;
        AssistantChatMessage = assistantChatMessage;
        AgentSession = agentSession;
        MainThreadDispatcher = mainThreadDispatcher;
    }

    /// <summary>本次执行使用的模型客户端。</summary>
    public IChatClient ChatClient { get; }

    /// <summary>本次对话的模型历史，首次执行时由执行器创建。</summary>
    public AgentSession? AgentSession { get; internal set; }

    /// <summary>承接流式输出的消息。</summary>
    public CopilotChatMessage AssistantChatMessage { get; }

    /// <summary>输出绑定 UI 时使用的线程调度器。</summary>
    public IMainThreadDispatcher? MainThreadDispatcher { get; }
}
