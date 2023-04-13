using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 空段的行高测量参数
/// </summary>
/// <param name="ParagraphProperty"></param>
public readonly record struct EmptyParagraphLineHeightMeasureArgument(ParagraphProperty ParagraphProperty,int ParagraphIndex)
{
}