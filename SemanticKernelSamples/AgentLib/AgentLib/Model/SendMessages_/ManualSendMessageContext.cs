using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Model;

/// <summary>
/// <see cref="IManualSendMessageContext"/> 的内部实现。
/// </summary>
internal sealed record ManualSendMessageContext(
    CopilotChatMessage UserChatMessage,
    CopilotChatMessage AssistantChatMessage,
    IChatClient ChatClient,
    ChatClientAgent ChatClientAgent,
    AgentSession AgentSession,
    IReadOnlyList<AITool> DefaultTools) : IManualSendMessageContext
{
    private bool _isFirstUpdate = true;

    /// <inheritdoc />
    public void AppendResponseUpdate(AgentResponseUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (_isFirstUpdate)
        {
            _isFirstUpdate = false;
            if (AssistantChatMessage.Content == "...")
            {
                AssistantChatMessage.ClearMessageItems();
            }
        }

        CopilotChatManager.AppendAssistantResponseUpdate(AssistantChatMessage, update);
    }
}
