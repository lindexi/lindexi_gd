using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AgentLib.Model;

internal sealed class CopilotChatContext
{
    public CopilotChatContext(ObservableCollection<CopilotChatMessage> historyMessages, ICopilotChatOutput currentContent)
    {
        ArgumentNullException.ThrowIfNull(historyMessages);
        ArgumentNullException.ThrowIfNull(currentContent);

        HistoryMessages = new ReadOnlyObservableCollection<CopilotChatMessage>(historyMessages);
        CurrentContent = currentContent;
    }

    public IReadOnlyList<CopilotChatMessage> HistoryMessages { get; }

    public ICopilotChatOutput CurrentContent { get; }

    public CopilotChatContext CreateSubAgentContext(ICopilotChatOutput subAgentItem)
    {
        ArgumentNullException.ThrowIfNull(subAgentItem);
        return new CopilotChatContext(new ObservableCollection<CopilotChatMessage>(HistoryMessages), subAgentItem);
    }
}
