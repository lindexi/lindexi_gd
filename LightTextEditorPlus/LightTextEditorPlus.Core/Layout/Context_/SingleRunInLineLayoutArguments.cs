using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

public readonly record struct SingleRunInLineLayoutArguments(ReadOnlyListSpan<CharData> RunList, int CurrentIndex,
    double LineRemainingWidth, ParagraphProperty ParagraphProperty)
{
}