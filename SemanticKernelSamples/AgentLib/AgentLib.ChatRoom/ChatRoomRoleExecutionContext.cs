using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom;

internal sealed record ChatRoomRoleExecutionContext(
    CopilotChatManager ChatManager,
    string? SystemPrompt,
    IReadOnlyList<AITool> AdditionalTools);
