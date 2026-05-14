using Avalonia.Controls;
using Avalonia.Controls.Templates;

using AvaloniaAgentLib.Model;

namespace AvaloniaAgentLib.View;

public sealed class CopilotChatMessageItemTemplateSelector : IDataTemplate
{
    public IDataTemplate? TextItemTemplate { get; set; }

    public IDataTemplate? ReasoningItemTemplate { get; set; }

    public IDataTemplate? ToolItemTemplate { get; set; }

    public Control? Build(object? param)
    {
        IDataTemplate? template = param switch
        {
            CopilotChatTextItem => TextItemTemplate,
            CopilotChatReasoningItem => ReasoningItemTemplate,
            CopilotChatToolItem => ToolItemTemplate,
            _ => null
        };

        return template?.Build(param);
    }

    public bool Match(object? data)
    {
        return data is ICopilotChatMessageItem;
    }
}
