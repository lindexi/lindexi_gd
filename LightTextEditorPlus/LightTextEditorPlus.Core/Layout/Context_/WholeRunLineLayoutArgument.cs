using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

public readonly record struct WholeRunLineLayoutArgument(ParagraphProperty ParagraphProperty, in ReadOnlyListSpan<IImmutableRun> RunList, double LineMaxWidth)
{
    
}