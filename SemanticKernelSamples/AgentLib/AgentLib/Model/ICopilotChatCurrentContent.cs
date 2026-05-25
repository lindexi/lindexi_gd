using Microsoft.Extensions.AI;

namespace AgentLib.Model;

internal interface ICopilotChatCurrentContent
{
    void AppendText(string text);

    void AppendReasoning(string text);

    void AppendFunctionCall(FunctionCallContent functionCallContent);

    void AppendFunctionResult(FunctionResultContent functionResultContent);

    CopilotChatSubAgentItem CreateSubAgentItem(string toolName, string? inputText, string? callId = null);
}
