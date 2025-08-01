using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 行距计算参数
/// </summary>
public readonly record struct LineSpacingCalculateArgument(ParagraphIndex ParagraphIndex, int LineIndex, ParagraphProperty ParagraphProperty, IReadOnlyRunProperty MaxFontSizeCharRunProperty);
