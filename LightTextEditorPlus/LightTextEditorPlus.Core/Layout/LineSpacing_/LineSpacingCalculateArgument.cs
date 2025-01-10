using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 行距计算参数
/// </summary>
public readonly record struct LineSpacingCalculateArgument(int ParagraphIndex, int LineIndex, ParagraphProperty ParagraphProperty, IReadOnlyRunProperty MaxFontSizeCharRunProperty);