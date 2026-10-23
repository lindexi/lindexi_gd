using Microsoft.Extensions.AI;

namespace AgentLib.Model;

/// <summary>
/// 接收助手执行过程中产生的内容、工具展示和用量信息。
/// </summary>
public interface ICopilotChatOutput
{
    /// <summary>追加文本输出。</summary>
    void AppendText(string text);

    /// <summary>追加推理输出。</summary>
    void AppendReasoning(string text);

    /// <summary>注册审批工具的展示说明。</summary>
    void RegisterApprovalTool(string toolName, string? approvalDescription = null);

    /// <summary>追加工具调用。</summary>
    void AppendFunctionCall(FunctionCallContent functionCallContent);

    /// <summary>追加带展示摘要的工具调用。</summary>
    void AppendFunctionCall(FunctionCallContent functionCallContent, ToolCallPresentation? presentation);

    /// <summary>创建或获取工具调用片段，保留已有详情。</summary>
    CopilotChatToolItem CreateToolItem(FunctionCallContent functionCallContent, ToolCallPresentation? presentation = null);

    /// <summary>追加工具执行结果。</summary>
    void AppendFunctionResult(FunctionResultContent functionResultContent);

    /// <summary>从响应内容中提取并追加用量。</summary>
    void AppendUsageDetails(IEnumerable<AIContent>? contents);

    /// <summary>追加用量，累加总用量并更新当前用量。</summary>
    void AppendUsageDetails(UsageDetails details);

    /// <summary>清除尚未被实际输出替换的占位符，保留已有输出。</summary>
    void ClearPlaceholder();

    /// <summary>创建或获取审批工具展示片段。</summary>
    CopilotChatApprovalToolItem CreateApprovalToolItem(string toolName, string? inputText, string? approvalDescription = null,
        string? callId = null, string? displayName = null);

    /// <summary>创建或获取子智能体展示片段。</summary>
    CopilotChatSubAgentItem CreateSubAgentItem(string toolName, string? inputText, string? callId = null);
}
