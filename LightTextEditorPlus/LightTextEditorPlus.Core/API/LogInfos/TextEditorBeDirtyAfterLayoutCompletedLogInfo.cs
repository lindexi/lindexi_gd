using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.LogInfos;

/// <summary>
/// 在 LayoutCompleted 事件里面，再次变更了文本，导致文本重新布局的日志信息
/// </summary>
/// <remarks>
/// 不一定是出现问题了，只是记录一下，文本库允许业务如此使用的。只是如果出现了多次重复布局，则会存在性能问题
/// </remarks>
/// <param name="TextEditor"></param>
public readonly record struct TextEditorBeDirtyAfterLayoutCompletedLogInfo(TextEditorCore TextEditor)
{
    /// <summary>
    /// 布局更新原因。仅当文本进入调试模式时有值
    /// </summary>
    public string? LayoutUpdateReason => TextEditor.GetLayoutUpdateReason();

    /// <inheritdoc />
    public override string ToString() =>
        $"[TextEditorCore][Layout] 在 LayoutCompleted 事件里面，再次变更了文本，将导致文本重新布局。更新理由：{LayoutUpdateReason ?? "<非调试模式，无原因记录日志>"}";
}
