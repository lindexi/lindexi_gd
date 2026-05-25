using Microsoft.Extensions.AI;

namespace AgentLib.Model;

internal interface ISubAgentProgressContainer
{
    CopilotChatSubAgentItem AppendSubAgentCall(string toolName, string? inputText, string? callId = null);

    void AppendSubAgentText(string callId, string text);

    void AppendSubAgentReasoning(string callId, string text);

    void AppendSubAgentFunctionCall(string callId, FunctionCallContent functionCallContent);

    void AppendSubAgentFunctionResult(string callId, FunctionResultContent functionResultContent);

    void AppendSubAgentOutput(string callId, string? outputText);
}