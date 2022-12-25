using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

public readonly record struct WholeLineLayoutArgument(ParagraphProperty ParagraphProperty, in ReadOnlyListSpan<CharData> CharDataList, double LineMaxWidth, Point CurrentStartPoint)
{
    
}