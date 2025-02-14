using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段落布局的结果
/// </summary>
/// <param name="NextParagraphStartPoint">下一段的起始坐标</param>
public readonly record struct ParagraphLayoutResult(TextPoint NextParagraphStartPoint);