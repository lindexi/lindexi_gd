using AgentLib.ChatRoom.Model;

namespace AgentLib.ChatRoom;

/// <summary>
/// 根据可持久化定义创建聊天室角色。
/// </summary>
public interface IChatRoomRoleFactory
{
    /// <summary>
    /// 创建聊天室角色。
    /// </summary>
    /// <param name="definition">角色定义。</param>
    /// <returns>尚未初始化的聊天室角色。</returns>
    ChatRoomRole CreateRole(ChatRoomRoleDefinition definition);
}
