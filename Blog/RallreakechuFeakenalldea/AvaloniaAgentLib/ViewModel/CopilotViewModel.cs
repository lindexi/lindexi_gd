using AgentLib;
using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace AvaloniaAgentLib.ViewModel;

public class CopilotViewModel : CopilotChatManager
{
    public CopilotViewModel()
    {
    }

    protected override void OnSessionCreated(CopilotChatSession session)
    {
        if (Design.IsDesignMode)
        {
            var copilotChatMessage = new CopilotChatMessage(ChatRole.Assistant, "测试测试测试");
            copilotChatMessage.MessageItems.Add(new CopilotChatReasoningItem("这是思考内容，这是思考内容"));
            copilotChatMessage.MessageItems.Add(new CopilotChatToolItem("asdasdasd", "ToolName", "输入的文本内容", "工具输出内容"));
            var subAgentItem = new CopilotChatSubAgentItem("sub-agent-demo", "调用子代理", "请总结当前文件结构", "项目由主应用、AgentLib 和 Avalonia 界面组成。");
            subAgentItem.MessageItems.Add(new CopilotChatReasoningItem("先查看目录，再总结重点。"));
            subAgentItem.MessageItems.Add(new CopilotChatTextItem("正在读取项目结构..."));
            subAgentItem.MessageItems.Add(new CopilotChatToolItem("call-1", "ReadFile", "README.md", "已读取 README.md"));
            var nestedSubAgentItem = new CopilotChatSubAgentItem("nested-agent", "调用子代理", "继续分析 Docs 目录", "已定位 Docs/Knowledge");
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

    public event EventHandler? SettingOpened;
}
