using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 命中测试结果
/// </summary>
/// <param name="IsOutOfTextCharacterBounds">是否超过文本字符范围了</param>
/// <param name="IsEndOfTextCharacterBounds">在 <see cref="IsOutOfTextCharacterBounds"/> 的基础上，是否在文档末尾</param>
/// <param name="IsInLineBoundsNotHitChar">是否在一行上，尽管没有直接命中到具体的字符。例如居中的两边或行的最后</param>
/// <param name="HitCaretOffset">命中的光标</param>
/// <param name="HitCharData">命中到的字符，如果没有命中到字符，将返回空</param>
/// <param name="HitParagraphIndex">命中到第几段</param>
public readonly record struct TextHitTestResult(bool IsOutOfTextCharacterBounds, bool IsEndOfTextCharacterBounds, bool IsInLineBoundsNotHitChar, CaretOffset HitCaretOffset, CharData? HitCharData, ParagraphIndex HitParagraphIndex)
{
    /// <summary>
    /// 是否命中到了空白部分，也就是没有直接命中字符
    /// </summary>
    public bool IsHitSpace { init; get; } = false;

    /// <summary>
    /// 命中到哪个段落
    /// </summary>
    internal ParagraphData HitParagraphData { init; get; } = null!;// 框架内部使用

    /// <summary>
    /// 命中的段落的段落属性
    /// </summary>
    public ParagraphProperty ParagraphProperty => HitParagraphData.ParagraphProperty;

    /// <summary>
    /// 命中到哪一行
    /// </summary>
    internal LineLayoutData? LineLayoutData { init; get; }
}
