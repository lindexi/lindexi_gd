using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom.Tools;

internal interface IChatRoomRoleTool : IAsyncDisposable
{
    IReadOnlyList<AITool> AITools { get; }
}

internal interface IChatRoomWorkspaceAwareTool
{
    Task SetWorkspacePathAsync(string? workspacePath, CancellationToken cancellationToken);
}
