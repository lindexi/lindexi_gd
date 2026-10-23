using AgentLib.ChatRoom.Model;

using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom;

internal interface IChatRoomRoleExecutor : IAsyncDisposable
{
    ChatRoomRoleExecutionKind ExecutionKind { get; }

    Task<ChatRoomRoleExecutionResult> RunAsync(
        ChatRoomRoleExecutionContext context,
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken);

    Task SetWorkspacePathAsync(
        CopilotChatManager chatManager,
        string? workspacePath,
        CancellationToken cancellationToken);
}
