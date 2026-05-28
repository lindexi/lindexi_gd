using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AvaloniaAgentLib.View;

public sealed class CopilotChatMessageItemTemplateSelector : IDataTemplate
{
    public IDataTemplate? TextItemTemplate { get; set; }

    public IDataTemplate? ReasoningItemTemplate { get; set; }

    public IDataTemplate? ApprovalToolItemTemplate { get; set; }

    public IDataTemplate? ToolItemTemplate { get; set; }

    public IDataTemplate? SubAgentItemTemplate { get; set; }

    public Control? Build(object? param)
    {
        IDataTemplate? template = param switch
        {
            CopilotChatTextItem => TextItemTemplate,
            CopilotChatReasoningItem => ReasoningItemTemplate,
            CopilotChatApprovalToolItem => ApprovalToolItemTemplate,
            CopilotChatToolItem => ToolItemTemplate,
            CopilotChatSubAgentItem => SubAgentItemTemplate,
            _ => null
        };

        return template?.Build(param);
    }

    public bool Match(object? data)
    {
        return data is ICopilotChatMessageItem;
    }
}
