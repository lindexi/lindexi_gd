using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AgentLib.Model;

internal sealed class CopilotChatContext
{
    public CopilotChatContext(ObservableCollection<CopilotChatMessage> historyMessages, ICopilotChatCurrentContent currentContent)
    {
        ArgumentNullException.ThrowIfNull(historyMessages);
        ArgumentNullException.ThrowIfNull(currentContent);

        HistoryMessages = new ReadOnlyObservableCollection<CopilotChatMessage>(historyMessages);
        CurrentContent = currentContent;
    }

    public IReadOnlyList<CopilotChatMessage> HistoryMessages { get; }

    public ICopilotChatCurrentContent CurrentContent { get; }

    public CopilotChatContext CreateSubAgentContext(CopilotChatSubAgentItem subAgentItem)
    {
        ArgumentNullException.ThrowIfNull(subAgentItem);
        return new CopilotChatContext(new ObservableCollection<CopilotChatMessage>(HistoryMessages), subAgentItem);
    }
}
