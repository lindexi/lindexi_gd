using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 按 Copilot 消息片段类型选择对应的数据模板。
/// </summary>
public sealed class ChatMessageItemTemplateSelector : IDataTemplate
{
    /// <summary>
    /// 获取或设置文本片段模板。
    /// </summary>
    public IDataTemplate? TextItemTemplate { get; set; }

    /// <summary>
    /// 获取或设置思考片段模板。
    /// </summary>
    public IDataTemplate? ReasoningItemTemplate { get; set; }

    /// <summary>
    /// 获取或设置图片片段模板。
    /// </summary>
    public IDataTemplate? ImageItemTemplate { get; set; }

    /// <summary>
    /// 获取或设置审批工具片段模板。
    /// </summary>
    public IDataTemplate? ApprovalToolItemTemplate { get; set; }

    /// <summary>
    /// 获取或设置普通工具片段模板。
    /// </summary>
    public IDataTemplate? ToolItemTemplate { get; set; }

    /// <summary>
    /// 获取或设置子代理片段模板。
    /// </summary>
    public IDataTemplate? SubAgentItemTemplate { get; set; }

    /// <summary>
    /// 为指定片段选择模板。
    /// </summary>
    /// <param name="item">消息片段。</param>
    /// <returns>匹配的模板；没有匹配类型时返回 <see langword="null"/>。</returns>
    public IDataTemplate? SelectTemplate(object? item)
    {
        return item switch
        {
            CopilotChatTextItem => TextItemTemplate,
            CopilotChatReasoningItem => ReasoningItemTemplate,
            CopilotChatImageItem => ImageItemTemplate,
            CopilotChatApprovalToolItem => ApprovalToolItemTemplate,
            CopilotChatToolItem => ToolItemTemplate,
            CopilotChatSubAgentItem => SubAgentItemTemplate,
            _ => null,
        };
    }

    /// <inheritdoc />
    public Control? Build(object? param) => SelectTemplate(param)?.Build(param);

    /// <inheritdoc />
    public bool Match(object? data) => data is ICopilotChatMessageItem;
}
