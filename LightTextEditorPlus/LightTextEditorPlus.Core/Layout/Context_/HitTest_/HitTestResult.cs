using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 命中测试结果
/// </summary>
/// <param name="IsOutOfTextCharacterBounds">是否超过文本字符范围了</param>
/// <param name="IsEndOfTextCharacterBounds">在 <see cref="IsOutOfTextCharacterBounds"/> 的基础上，是否在文档末尾</param>
public readonly record struct TextHitTestResult(bool IsOutOfTextCharacterBounds, bool IsEndOfTextCharacterBounds, CaretOffset HitCaretOffset)
{
    /// <summary>
    /// 命中到哪个段落
    /// </summary>
    internal ParagraphData HitParagraphData { init; get; }

    /// <summary>
    /// 命中到第几段
    /// </summary>
    public int HitParagraphIndex => HitParagraphData.Index;
}
