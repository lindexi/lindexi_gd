using System.Collections.Generic;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Diagnostics.LogInfos;

/// <summary>
/// 布局完成的日志信息
/// </summary>
public readonly record struct LayoutCompletedLogInfo
{
    /// <summary>
    /// 布局完成的日志信息
    /// </summary>
    internal LayoutCompletedLogInfo(DocumentLayoutResult result)
    {
        _documentLayoutResult = result;
    }

    /// <summary>
    /// 获取文档的布局范围
    /// </summary>
    public TextRect DocumentBounds => _documentLayoutResult.DocumentBounds;

    private readonly DocumentLayoutResult _documentLayoutResult;

    /// <summary>
    /// 获取布局的调试信息
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<LayoutDebugMessage> GetLayoutDebugMessageList() => _documentLayoutResult.UpdateLayoutContext.GetLayoutDebugMessageList();
}
