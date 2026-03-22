#if !USE_SKIA || USE_AllInOne
using System;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Events;

/// <summary>
/// 准备上下文右键菜单的参数
/// </summary>
public class PrepareContextMenuEventArgs : EventArgs
{
    /// <summary>
    /// 命中到的点
    /// </summary>
    public TextPoint HitPoint { get; init; }

    /// <summary>
    /// 文本框编辑器
    /// </summary>
    public required TextEditor TextEditor { get; init; }

    /// <summary>
    /// 文本框编辑器核心
    /// </summary>
    public TextEditorCore TextEditorCore => TextEditor.TextEditorCore;

    /// <summary>
    /// 尝试对 <see cref="HitPoint"/> 进行命中测试，获取命中结果
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool TryHitTest(out TextHitTestResult result)
    {
        return TextEditorCore.TryHitTest(HitPoint, out result);
    }
}
#endif