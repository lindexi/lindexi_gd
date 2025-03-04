using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

public readonly record struct DivideWordArgument(TextReadOnlyListSpan<CharData> CurrentRunList, UpdateLayoutContext UpdateLayoutContext);