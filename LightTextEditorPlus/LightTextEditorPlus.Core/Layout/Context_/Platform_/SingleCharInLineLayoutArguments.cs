using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

public readonly record struct SingleCharInLineLayoutArguments(ReadOnlyListSpan<CharData> RunList, int CurrentIndex,
    double LineRemainingWidth, ParagraphProperty ParagraphProperty)
{
    public CharData CurrentCharData => RunList[CurrentIndex];
}