using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 命中段落数据的结果
/// </summary>
/// <param name="InputCaretOffset">参与命中的光标偏移量</param>
/// <param name="ParagraphData">命中到的段落</param>
/// <param name="HitOffset">命中到的段落偏移量</param>
/// <param name="ParagraphManager"></param>
/// 此类型用来减少重复计算
readonly record struct HitParagraphDataResult(CaretOffset InputCaretOffset, ParagraphData ParagraphData, ParagraphCaretOffset HitOffset, ParagraphManager ParagraphManager)
{
    /// <summary>
    /// 获取命中的字符。如果命中到段落首，那将取首个字符，否则取命中到的前一个字符
    /// </summary>
    /// <returns>对于空段落，返回 null 值</returns>
    public CharData? GetHitCharData()
    {
        if (ParagraphData.IsEmptyParagraph)
        {
            // 如果是空段，那就没有命中字符
            return null;
        }

        ParagraphCharOffset paragraphCharOffset;
        if (HitOffset.Offset == 0)
        {
            // 如果命中到段落首，那将取首个字符
            paragraphCharOffset = new ParagraphCharOffset(HitOffset.Offset);
        }
        else
        {
            paragraphCharOffset = new ParagraphCharOffset(HitOffset.Offset - 1);
        }

        var charData = ParagraphData.GetCharData(paragraphCharOffset);
        return charData;
    }
}