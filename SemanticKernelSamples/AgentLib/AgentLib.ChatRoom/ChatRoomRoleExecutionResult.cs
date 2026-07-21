using AgentLib.Model;

namespace AgentLib.ChatRoom;

internal sealed record ChatRoomRoleExecutionResult(
    CopilotChatMessage AssistantChatMessage,
    Task<ChatRoomRoleExecutionCompletion> CompletionTask);

internal sealed record ChatRoomRoleExecutionCompletion(
    string? Content,
    bool WasCanceled);
