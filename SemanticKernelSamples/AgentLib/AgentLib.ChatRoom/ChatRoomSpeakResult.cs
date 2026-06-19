using AgentLib.Model;

using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 角色发言的结果。包含底层 <see cref="CopilotChatMessage"/>（用于 UI 流式绑定）
/// 和一个表示最终文本内容的异步任务。
/// </summary>
/// <param name="AssistantChatMessage">
/// 底层助手消息对象。其 <see cref="CopilotChatMessage.Content"/> 在流式生成过程中持续更新，
/// UI 可直接绑定此对象感知实时变化。
/// </param>
/// <param name="FinalContentTask">
/// 在发言完全结束后完成的任务，结果为角色的公开回复文本。
/// 如果角色未产生有效回复或被取消，结果为 <see langword="null"/>。
/// </param>
public sealed record ChatRoomSpeakResult(
    CopilotChatMessage AssistantChatMessage,
    Task<string?> FinalContentTask)
{
}
