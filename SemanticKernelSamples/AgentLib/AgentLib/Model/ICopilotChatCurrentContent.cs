using Microsoft.Extensions.AI;

namespace AgentLib.Model;

internal interface ICopilotChatCurrentContent
{
    void AppendText(string text);

    void AppendReasoning(string text);

    void RegisterApprovalTool(string toolName, string? approvalDescription = null);

    void AppendFunctionCall(FunctionCallContent functionCallContent);

    void AppendFunctionResult(FunctionResultContent functionResultContent);

    CopilotChatApprovalToolItem CreateApprovalToolItem(string toolName, string? inputText, string? approvalDescription = null,
        string? callId = null);

    CopilotChatSubAgentItem CreateSubAgentItem(string toolName, string? inputText, string? callId = null);
}
