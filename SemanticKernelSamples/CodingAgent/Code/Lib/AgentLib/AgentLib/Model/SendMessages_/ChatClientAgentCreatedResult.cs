using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Model;

/// <summary>
/// 表示聊天客户端与代理创建完成后的结果。
/// </summary>
/// <param name="ChatClient">底层聊天客户端。</param>
/// <param name="ChatClientAgent">由聊天客户端创建的代理对象。</param>
/// <param name="AgentSession">本次运行使用的代理会话；当不携带历史时为 <see langword="null"/>。</param>
public record ChatClientAgentCreatedResult(IChatClient ChatClient, ChatClientAgent ChatClientAgent, AgentSession? AgentSession);