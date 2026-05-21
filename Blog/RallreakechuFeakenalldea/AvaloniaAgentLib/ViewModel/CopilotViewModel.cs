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
            session.ChatMessages.Add(copilotChatMessage);
        }
    }

    public void OpenSetting()
    {
        SettingOpened?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SettingOpened;
}
