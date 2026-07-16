using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Core;

namespace AgentLib.ChatRoom;

/// <summary>
/// 创建聊天室角色。
/// </summary>
public sealed class ChatRoomRoleFactory : IChatRoomRoleFactory
{
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private readonly CodingAssistantRoleFactory _codingAssistantRoleFactory;

    /// <summary>
    /// 创建通用角色工厂。
    /// </summary>
    /// <param name="mainThreadDispatcher">可选的主线程调度器。</param>
    public ChatRoomRoleFactory(
        IMainThreadDispatcher? mainThreadDispatcher = null,
        string roslynLanguageServerCommand = "roslyn-language-server")
    {
        _mainThreadDispatcher = mainThreadDispatcher;
        _codingAssistantRoleFactory = new CodingAssistantRoleFactory(
            mainThreadDispatcher,
            roslynLanguageServerCommand);
    }

    /// <inheritdoc />
    public ChatRoomRole CreateRole(ChatRoomRoleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Kind == ChatRoomRoleKind.CodingAssistant)
        {
            return _codingAssistantRoleFactory.CreateRole(definition);
        }

        return new ChatRoomRole(definition)
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };
    }
}
