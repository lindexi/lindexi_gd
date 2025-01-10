using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 文档布局结果
/// </summary>
/// <param name="DocumentBounds"></param>
readonly record struct DocumentLayoutResult(TextRect DocumentBounds)
{
}