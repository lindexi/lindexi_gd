using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 文档布局结果
/// </summary>
readonly record struct DocumentLayoutResult(DocumentLayoutBounds LayoutBounds, UpdateLayoutContext UpdateLayoutContext)
{
}
