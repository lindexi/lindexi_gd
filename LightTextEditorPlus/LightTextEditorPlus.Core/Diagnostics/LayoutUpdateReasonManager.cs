using System;
using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Diagnostics;

/// <summary>
/// 需要更新布局的原因管理
/// </summary>
/// 用于调试为什么当前需要布局
internal class LayoutUpdateReasonManager
{
    public LayoutUpdateReasonManager(TextEditorCore textEditor)
    {
        textEditor.LayoutCompleted += TextEditor_LayoutCompleted;
    }

    private void TextEditor_LayoutCompleted(object? sender, EventArgs e)
    {
        ReasonList.Clear();
    }

    /// <summary>
    /// 添加触发布局的原因
    /// </summary>
    /// <param name="reason"></param>
    public void AddLayoutReason(string reason)
    {
        ReasonList.Add(reason);
    }

    private List<string> ReasonList { get; } = new List<string>();

    public string ReasonText => string.Join(';', ReasonList);

    public override string ToString()
    {
        return "LayoutUpdateReason:" + ReasonText;
    }
}
