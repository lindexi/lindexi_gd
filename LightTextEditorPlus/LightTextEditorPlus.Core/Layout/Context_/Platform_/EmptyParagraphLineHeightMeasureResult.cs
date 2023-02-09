using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 空段的行高测量结果
/// </summary>
/// <param name="ParagraphBounds"></param>
/// <param name="NextLineStartPoint"></param>
public readonly record struct EmptyParagraphLineHeightMeasureResult(Rect ParagraphBounds, Point NextLineStartPoint)
{
}