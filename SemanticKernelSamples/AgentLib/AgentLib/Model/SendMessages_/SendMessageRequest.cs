using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Model;

/// <summary>
/// 表示一次聊天发送请求。
/// </summary>
/// <param name="InputText">用户输入内容。</param>
/// <param name="WithHistory">是否携带当前 <see cref="AgentSession"/> 继续对话。</param>
/// <param name="CreateNewSession">是否在发送前切换到新会话。</param>
/// <param name="Tools">本次额外启用的工具集合。</param>
/// <param name="ToolMode">工具调用模式。</param>
/// <param name="SystemPrompt">可选的系统提示词，会注入到 <see cref="ChatOptions.Instructions"/>。</param>
/// <param name="CancellationToken">本次发送使用的取消令牌。</param>
public readonly record struct SendMessageRequest
(
    string? InputText,
    bool WithHistory = true,
    bool CreateNewSession = false,
    IEnumerable<AITool>? Tools = null,
    ChatToolMode? ToolMode = null,
    string? SystemPrompt = null,
    CancellationToken CancellationToken = default)
{
}