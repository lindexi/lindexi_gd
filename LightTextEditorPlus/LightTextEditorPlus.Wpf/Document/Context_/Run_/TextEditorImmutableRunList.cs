using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 不可变的文本列表
/// </summary>
public class TextEditorImmutableRunList : ImmutableRunListBase<ImmutableRun>
{
    /// <summary>
    /// 创建不可变的文本列表
    /// </summary>
    public TextEditorImmutableRunList(IEnumerable<ImmutableRun> runs) : base(runs)
    {
    }
}