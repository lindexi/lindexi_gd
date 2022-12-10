using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 命中段落数据的结果
/// </summary>
/// <param name="Offset">参与命中的光标偏移量</param>
/// <param name="ParagraphData">命中到的段落</param>
/// <param name="HitOffset">命中到的段落偏移量</param>
/// <param name="ParagraphManager"></param>
/// 此类型用来减少重复计算
readonly record struct HitParagraphDataResult(CaretOffset Offset, ParagraphData ParagraphData, ParagraphCaretOffset HitOffset, ParagraphManager ParagraphManager)
{
}