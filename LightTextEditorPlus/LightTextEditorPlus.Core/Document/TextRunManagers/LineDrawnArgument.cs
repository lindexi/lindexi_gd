using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

public readonly record struct LineDrawnArgument(bool IsDrawn, bool IsLineStartPointUpdated,
    object? LineAssociatedRenderData, Point StartPoint, Size Size, ReadOnlyListSpan<CharData> CharList)
{
}