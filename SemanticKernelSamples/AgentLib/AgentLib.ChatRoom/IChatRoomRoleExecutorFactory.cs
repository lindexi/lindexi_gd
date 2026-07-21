using AgentLib.ChatRoom.Model;

namespace AgentLib.ChatRoom;

internal interface IChatRoomRoleExecutorFactory
{
    ChatRoomRoleExecutionKind ExecutionKind { get; }

    IChatRoomRoleExecutor Create(ChatRoomRoleExecutorCreationContext context);
}

internal sealed record ChatRoomRoleExecutorCreationContext(ChatRoomRoleDefinition Definition);
