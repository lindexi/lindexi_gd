using AgentLib;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace AvaloniaAgentLib.ViewModel;

public class CopilotViewModel : CopilotChatManager
{
    public CopilotViewModel()
    {
    }

    /// <summary>
    /// 额外的默认工具集合。在每次发送消息时，这些工具会与 <see cref="SendMessageRequest.Tools"/> 合并后传递给 <see cref="CopilotChatManager.ResolveTools"/>。
    /// 调用方可在创建 ViewModel 后向此集合添加工具。
    /// </summary>
    public List<AITool> AdditionalDefaultTools { get; } = [];

    /// <summary>
    /// 发送消息并开始聊天。会自动合并 <see cref="AdditionalDefaultTools"/> 到工具列表中。
    /// </summary>
    public new async Task SendMessageAsync(IReadOnlyList<AIContent> contents, bool withHistory = true, bool createNewSession = false, IReadOnlyList<AITool>? tools = null,
        ChatToolMode? toolMode = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AITool> mergedTools = MergeAdditionalTools(tools);
        await base.SendMessageAsync(contents, withHistory, createNewSession, mergedTools, toolMode, cancellationToken);
    }

    /// <summary>
    /// 开启新的会话并发送消息。会自动合并 <see cref="AdditionalDefaultTools"/> 到工具列表中。
    /// </summary>
    public new Task SendMessageInNewSessionAsync(IReadOnlyList<AIContent> contents, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(contents, withHistory: true, createNewSession: true, tools: null, cancellationToken: cancellationToken);
    }

    private IReadOnlyList<AITool> MergeAdditionalTools(IReadOnlyList<AITool>? tools)
    {
        if (AdditionalDefaultTools.Count == 0)
        {
            return tools ?? [];
        }

        if (tools is null || tools.Count == 0)
        {
            return AdditionalDefaultTools;
        }

        var merged = new List<AITool>(tools.Count + AdditionalDefaultTools.Count);
        merged.AddRange(tools);
        merged.AddRange(AdditionalDefaultTools);
        return merged;
    }

    protected override void OnSessionCreated(CopilotChatSession session)
    {
        if (Design.IsDesignMode)
        {
            var copilotChatMessage = new CopilotChatMessage(ChatRole.Assistant, "测试测试测试");
            copilotChatMessage.MessageItems.Add(new CopilotChatReasoningItem("这是思考内容，这是思考内容"));
            copilotChatMessage.MessageItems.Add(new CopilotChatToolItem("asdasdasd", "ToolName", "输入的文本内容", "工具输出内容"));
            copilotChatMessage.MessageItems.Add(new CopilotChatApprovalToolItem("approval-demo", "DeleteFile", "{\n  \"filePath\": \"temp/demo.txt\"\n}", "该操作会删除文件，请确认是否继续。"));
            var subAgentItem = new CopilotChatSubAgentItem("sub-agent-demo", "调用子智能体", "请总结当前文件结构", "项目由主应用、AgentLib 和 Avalonia 界面组成。");
            subAgentItem.MessageItems.Add(new CopilotChatReasoningItem("先查看目录，再总结重点。"));
            subAgentItem.MessageItems.Add(new CopilotChatTextItem("正在读取项目结构..."));
            subAgentItem.MessageItems.Add(new CopilotChatToolItem("call-1", "ReadFile", "README.md", "已读取 README.md"));
            var nestedSubAgentItem = new CopilotChatSubAgentItem("nested-agent", "调用子智能体", "继续分析 Docs 目录", "已定位 Docs/Knowledge");
            nestedSubAgentItem.MessageItems.Add(new CopilotChatTextItem("已定位 Docs/Knowledge"));
            subAgentItem.MessageItems.Add(nestedSubAgentItem);
            copilotChatMessage.MessageItems.Add(subAgentItem);
            session.ChatMessages.Add(copilotChatMessage);
        }
    }

    public void OpenSetting()
    {
        SettingOpened?.Invoke(this, EventArgs.Empty);
    }

    public void ApproveTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        ApproveToolExecution(approvalToolItem);
    }

    public void RejectTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        RejectToolExecution(approvalToolItem);
    }

    public event EventHandler? SettingOpened;
}
