using Avalonia.Controls;
using Avalonia.Controls.Templates;

using AvaloniaAgentLib.Model;

using Microsoft.Extensions.AI;

namespace AvaloniaAgentLib.View;

public sealed class CopilotChatMessageTemplateSelector : IDataTemplate
{
    public IDataTemplate? UserTemplate { get; set; }

    public IDataTemplate? AssistantTemplate { get; set; }

    public IDataTemplate? SystemTemplate { get; set; }

    public Control? Build(object? param)
    {
        if (param is not CopilotChatMessage chatMessage)
        {
            return null;
        }

        IDataTemplate? template;
        if (chatMessage.Role == ChatRole.User)
        {
            template = UserTemplate;
        }
        else if (chatMessage.Role == ChatRole.System)
        {
            template = SystemTemplate;
        }
        else
        {
            template = AssistantTemplate;
        }

        return template?.Build(param);
    }

    public bool Match(object? data)
    {
        return data is CopilotChatMessage;
    }
}
