using System.Windows;
using System.Windows.Controls;

using AgentLib.Model;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Selects a data template for Copilot chat message items.
/// </summary>
public sealed class CopilotChatMessageItemTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template used for text items.
    /// </summary>
    public DataTemplate? TextItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used for reasoning items.
    /// </summary>
    public DataTemplate? ReasoningItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used for tool call items.
    /// </summary>
    public DataTemplate? ToolItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used for sub-agent items.
    /// </summary>
    public DataTemplate? SubAgentItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used for image items.
    /// </summary>
    public DataTemplate? ImageItemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used for audio items.
    /// </summary>
    public DataTemplate? AudioItemTemplate { get; set; }

    /// <inheritdoc />
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            CopilotChatTextItem => TextItemTemplate,
            CopilotChatReasoningItem => ReasoningItemTemplate,
            CopilotChatToolItem => ToolItemTemplate,
            CopilotChatSubAgentItem => SubAgentItemTemplate,
            CopilotChatImageItem => ImageItemTemplate,
            CopilotChatAudioItem => AudioItemTemplate,
            _ => base.SelectTemplate(item, container),
        };
    }
}
