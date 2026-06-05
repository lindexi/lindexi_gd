namespace AgentLib.Model;

/// <summary>
/// 表示聊天消息中的一个可观测片段。
/// </summary>
public interface ICopilotChatMessageItem
{
    /// <summary>
    /// 创建当前片段的深拷贝。
    /// </summary>
    /// <returns>深拷贝后的新片段实例。</returns>
    ICopilotChatMessageItem Clone();
}