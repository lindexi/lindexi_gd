using System.Collections.Generic;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本列表
/// </summary>
public class TextEditorImmutableRunList : TextEditorImmutableRunListBase<ImmutableRun>
{
    /// <summary>
    /// 创建不可变的文本列表
    /// </summary>
    public TextEditorImmutableRunList(IEnumerable<ImmutableRun> runs) : base(runs)
    {
    }
}