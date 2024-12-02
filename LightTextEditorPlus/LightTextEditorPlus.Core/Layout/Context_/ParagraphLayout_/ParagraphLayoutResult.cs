using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段落布局的结果
/// </summary>
/// <param name="NextLineStartPoint">下一行的起始坐标</param>
public readonly record struct ParagraphLayoutResult(TextPoint NextLineStartPoint);