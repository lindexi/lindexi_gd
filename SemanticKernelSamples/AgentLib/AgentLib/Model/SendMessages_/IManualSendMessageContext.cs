using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Model;

/// <summary>
/// 手动发送消息的上下文，提供裸 <see cref="IChatClient"/>、<see cref="ChatClientAgent"/>、<see cref="AgentSession"/>、
/// 默认工具列表，以及空壳 <see cref="CopilotChatMessage"/> 供调用方自行填充和流式追加。
/// 调用方完全自行控制 AgentFramework 的调用流程。
/// </summary>
public interface IManualSendMessageContext
{
    /// <summary>
    /// 用户消息对象。创建时为空内容，由调用方自行填充。
    /// </summary>
    CopilotChatMessage UserChatMessage { get; }

    /// <summary>
    /// 助手消息对象。创建时包含 "..." 占位符，流式响应开始后由 <see cref="AppendResponseUpdate"/> 自动清理。
    /// </summary>
    CopilotChatMessage AssistantChatMessage { get; }

    /// <summary>
    /// 底层聊天客户端。调用方可直接使用 <c>CompleteAsync</c> 等原始 API。
    /// </summary>
    IChatClient ChatClient { get; }

    /// <summary>
    /// 由聊天客户端创建的代理对象，已装配默认 <see cref="IChatReducer"/> 和 <see cref="AIContextProvider"/>。
    /// </summary>
    ChatClientAgent ChatClientAgent { get; }

    /// <summary>
    /// 代理会话，始终不为 <see langword="null"/>。调用方自行决定是否传入 <see cref="ChatClientAgent.RunStreamingAsync"/>。
    /// </summary>
    AgentSession AgentSession { get; }

    /// <summary>
    /// 默认工具列表（工作区工具 + 子代理工具），未经过 <c>HumanApprovalTool</c> 包装。
    /// 调用方可按需将其加入 <see cref="ChatOptions.Tools"/>。
    /// </summary>
    IReadOnlyList<AITool> DefaultTools { get; }

    /// <summary>
    /// 将 <see cref="AgentResponseUpdate"/> 中的内容追加到 <see cref="AssistantChatMessage"/>。
    /// 首次调用时，若助手消息内容仍为 "..." 占位符，则自动清除。
    /// </summary>
    /// <param name="update">来自 <see cref="ChatClientAgent.RunStreamingAsync"/> 的响应更新。</param>
    void AppendResponseUpdate(AgentResponseUpdate update);
}
