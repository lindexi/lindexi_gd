using AgentLib.Coding;

using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom.Tools;

internal sealed class CodingWorkspaceRoleToolAdapter : IChatRoomRoleTool, IChatRoomWorkspaceAwareTool
{
    private readonly CodingWorkspaceToolProvider _toolProvider;

    internal CodingWorkspaceRoleToolAdapter(CodingWorkspaceToolProvider toolProvider)
    {
        ArgumentNullException.ThrowIfNull(toolProvider);
        _toolProvider = toolProvider;
    }

    public IReadOnlyList<AITool> AITools => _toolProvider.AITools;

    public Task SetWorkspacePathAsync(string? workspacePath, CancellationToken cancellationToken)
    {
        return _toolProvider.SetWorkspacePathAsync(workspacePath, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _toolProvider.DisposeAsync();
    }
}
