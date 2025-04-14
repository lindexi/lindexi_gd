using System.Collections.Generic;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 布局完成的日志信息
/// </summary>
public readonly record struct LayoutCompletedLogInfo
{
    /// <summary>
    /// 布局完成的日志信息
    /// </summary>
    internal LayoutCompletedLogInfo(DocumentLayoutResult result, UpdateLayoutContext updateLayoutContext)
    {
        UpdateLayoutContext = updateLayoutContext;
        _documentLayoutResult = result;
    }

    /// <summary>
    /// 本次布局的上下文
    /// </summary>
    public UpdateLayoutContext UpdateLayoutContext { get; }

    /// <summary>
    /// 获取文档的布局范围
    /// </summary>
    public DocumentLayoutBounds DocumentBounds => _documentLayoutResult.LayoutBounds.ToDocumentLayoutBounds();

    private readonly DocumentLayoutResult _documentLayoutResult;

    /// <summary>
    /// 获取布局的调试信息
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<LayoutDebugMessage> GetLayoutDebugMessageList() => UpdateLayoutContext.GetLayoutDebugMessageList();
}
