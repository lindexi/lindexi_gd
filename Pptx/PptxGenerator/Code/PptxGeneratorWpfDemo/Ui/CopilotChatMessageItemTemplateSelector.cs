using AgentLib.Model;

using System;
using System.Windows;
using System.Windows.Controls;

namespace PptxGenerator;

/// <summary>
/// 按 <see cref="ICopilotChatMessageItem"/> 具体子类型分发对应的 <see cref="DataTemplate"/>。
/// 模板在 XAML 中定义并通过属性注入。
/// </summary>
public sealed class CopilotChatMessageItemTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// 用于渲染 <see cref="CopilotChatTextItem"/> 的模板。
    /// </summary>
    public DataTemplate? TextItemTemplate { get; set; }

    /// <summary>
    /// 用于渲染 <see cref="CopilotChatReasoningItem"/> 的模板。
    /// </summary>
    public DataTemplate? ReasoningItemTemplate { get; set; }

    /// <summary>
    /// 用于渲染 <see cref="CopilotChatToolItem"/> 的模板。
    /// </summary>
    public DataTemplate? ToolItemTemplate { get; set; }

    /// <summary>
    /// 用于渲染 <see cref="CopilotChatApprovalToolItem"/> 的模板（当前版本不支持，将抛出 <see cref="NotSupportedException"/>）。
    /// </summary>
    public DataTemplate? ApprovalToolItemTemplate { get; set; }

    /// <summary>
    /// 用于渲染 <see cref="CopilotChatSubAgentItem"/> 的模板。当前复用 ToolItemTemplate，不做特殊处理。
    /// </summary>
    public DataTemplate? SubAgentItemTemplate { get; set; }

    /// <summary>
    /// 用于渲染 <see cref="CopilotChatImageItem"/> 的模板。
    /// </summary>
    public DataTemplate? ImageItemTemplate { get; set; }

    /// <summary>
    /// 用于渲染 <see cref="CopilotChatAudioItem"/> 的模板。
    /// </summary>
    public DataTemplate? AudioItemTemplate { get; set; }

    /// <inheritdoc />
    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            CopilotChatTextItem => TextItemTemplate,
            CopilotChatReasoningItem => ReasoningItemTemplate,
            CopilotChatApprovalToolItem => throw new NotSupportedException("审批工具暂不支持。"),
            CopilotChatToolItem => ToolItemTemplate,
            CopilotChatSubAgentItem => SubAgentItemTemplate ?? ToolItemTemplate,
            CopilotChatImageItem => ImageItemTemplate,
            CopilotChatAudioItem => AudioItemTemplate,
            _ => null
        };
    }
}
