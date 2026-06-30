using AgentLib;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;

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
    /// 底层聊天客户端。
    /// </summary>
    IChatClient ChatClient { get; }

    /// <summary>
    /// 主线程调度器。用于将操作调度回 UI 主线程执行。为 <see langword="null"  /> 时不做线程调度。
    /// </summary>
    IMainThreadDispatcher? MainThreadDispatcher { get; }

    /// <summary>
    /// 由聊天客户端创建的代理对象，已装配默认 <see cref="IChatReducer"/> 和 <see cref="AIContextProvider"/>。
    /// 在调用 <see cref="IChatClient.AsAIAgent"/> 之前，会执行 <paramref name="configure"/> 委托，
    /// 允许业务端对 <see cref="ChatClientAgentOptions"/> 进行进一步配置。
    /// 延迟创建，首次调用时异步初始化。
    /// </summary>
    /// <param name="configure">在创建代理之前对 <see cref="ChatClientAgentOptions"/> 进行进一步配置的委托。为 <see langword="null"/> 时不做额外配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>聊天客户端代理。</returns>
    Task<ChatClientAgent> GetChatClientAgentAsync(Action<ChatClientAgentOptions>? configure = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 代理会话。延迟创建，首次调用时异步初始化。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>代理会话。</returns>
    Task<AgentSession> GetAgentSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 默认工具列表（工作区工具 + 子代理工具），未经过 <c>HumanApprovalTool</c> 包装。
    /// 调用方可按需将其加入 <see cref="ChatOptions.Tools"/>。
    /// </summary>
    IReadOnlyList<AITool> DefaultTools { get; }

    /// <summary>
    /// 将 <see cref="AgentResponseUpdate"/> 中的内容追加到 <see cref="AssistantChatMessage"/>。
    /// 首次调用时，若助手消息内容仍为 "..." 占位符，则自动清除。
    /// </summary>
    /// <param name="update">响应更新。</param>
    void AppendResponseUpdate(AgentResponseUpdate update);

    /// <summary>
    /// 将 <see cref="UserChatMessage"/> 和 <see cref="AssistantChatMessage"/> 追加到当前选中会话的可见消息列表中。
    /// </summary>
    Task AppendMessagesToSessionAsync();

    /// <summary>
    /// 标记进入聊天状态（<c>IsChatting = true</c>），返回一个可释放对象。
    /// 调用方使用 <c>using</c> 语句包裹流式调用过程，<see cref="IDisposable.Dispose"/> 时自动恢复 <c>IsChatting = false</c>。
    /// 等效于调用 <see cref="CopilotChatManager.StartChatting"/>。
    /// </summary>
    /// <returns>可释放对象，dispose 时恢复 <c>IsChatting = false</c>。</returns>
    IDisposable StartChatting();
}
